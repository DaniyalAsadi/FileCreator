using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;
using System.Xml.Linq;

namespace GrpcScaffold.Core.Generation;

/// <summary>
/// Shared building blocks for both <see cref="MappingGenerator"/> (server: gRPC ⇄ mediator)
/// and <see cref="ClientMappingGenerator"/> (client: CLR request ⇄ gRPC message). Both
/// generators only differ in *which contracts* play the role of "proto side" and "clr side" —
/// the actual expression-building logic is identical, so it lives here once.
/// </summary>
internal static class MappingExpressionBuilder
{
    // ---------------------------------------------------------------------
    // Contract -> template-model projection
    // ---------------------------------------------------------------------

    public static Dictionary<string, object?> CreateContract(ContractInfo? contract)
    {
        if (contract is null)
            return new Dictionary<string, object?> { ["fields"] = Array.Empty<object>() };

        return new Dictionary<string, object?>
        {
            ["name"] = contract.Name,
            ["namespace"] = contract.Namespace,
            ["type_name"] = contract.ClrType.ToDisplayString(),

            ["fields"] = contract.Fields.Select(CreateField).ToList(),

            ["preferred_constructor"] = CreateConstructor(contract.PreferredConstructor)
        };
    }

    private static Dictionary<string, object?> CreateField(ProtoFieldInfo field)
    {
        var reference = field.Reference;

        return new Dictionary<string, object?>
        {
            ["name"] = field.Name,
            ["proto_name"] = field.ProtoName,

            ["proto_type"] = reference.ProtoTypeName,
            ["clr_type"] = reference.ClrType.ToDisplayString(),

            ["is_enum"] = reference.IsEnum,
            ["is_message"] = reference.IsMessage,
            ["is_repeated"] = reference.IsRepeated,
            ["is_nullable"] = reference.IsNullable,
            ["is_well_known"] = reference.IsWellKnownType,

            ["is_struct"] = reference.IsStruct,
            ["is_map"] = reference.IsMap,

            ["needs_cast"] = ProtoTypeConversion.NeedsCast(reference)
        };
    }

    private static Dictionary<string, object?> CreateConstructor(ConstructorInfo? ctor)
    {
        if (ctor is null)
            return [];

        return new Dictionary<string, object?>
        {
            ["name"] = ctor.Name,
            ["is_parameterless"] = ctor.IsParameterless,
            ["parameters"] = ctor.Parameters.Select(CreateParameter).ToList()
        };
    }

    private static Dictionary<string, object?> CreateParameter(ConstructorParameterInfo parameter) => new()
    {
        ["name"] = parameter.Name,
        ["source"] = parameter.SourceFieldName ?? parameter.Name,
        ["type_name"] = parameter.TypeName,
        ["is_optional"] = parameter.IsOptional,
        ["has_default"] = parameter.HasDefaultValue,
        ["default_value"] = parameter.DefaultValue,
        ["is_nullable"] = parameter.IsNullable,
        ["is_params"] = parameter.IsParams,
        ["ref_kind"] = parameter.RefKind.ToString()
    };

    // ---------------------------------------------------------------------
    // <proto side>.<field> -> new <ClrTarget>(...)
    //
    // Used by: server BuildRequestMappings (grpc request -> mediator ctor)
    //          client BuildResponseMappings (grpc response -> response ctor)
    // ---------------------------------------------------------------------

    public static (string Expression, bool NeedsReview) BuildConstructorArgumentExpression(
        ConstructorParameterInfo parameter,
        string source,
        IReadOnlyList<ProtoFieldInfo> sourceFields,
        IReadOnlyDictionary<ITypeSymbol, ContractInfo> protoLookup,
        IReadOnlyDictionary<ITypeSymbol, ContractInfo> constructibleLookup,
        ISet<ITypeSymbol> visiting)
    {
        var sourceName = parameter.SourceFieldName ?? parameter.Name;

        // 1. Direct field mapping always wins over nested construction (requirement #5).
        var field = sourceFields.FirstOrDefault(f =>
            string.Equals(f.Name, sourceName, StringComparison.OrdinalIgnoreCase));

        if (field is not null)
        {
            var materializer = ProtoTypeConversion.CollectionMaterializer(parameter.TypeName);

            // Destination nullability decides whether a null/presence guard may be emitted:
            // the annotation, or a Nullable<T> parameter shape in nullable-disabled contexts.
            var destinationNullable = parameter.IsNullable ||
                parameter.Type is INamedTypeSymbol
                {
                    OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
                };

            // Presence flows the other way: it exists on the proto field, which the
            // ProtoGenerator marked `optional` based on the *request contract* annotation.
            var presenceSource = field.IsNullable &&
                ProtoTypeConversion.HasProtoPresenceAccessor(field.Reference)
                    ? $"{source}.Has{field.Name}"
                    : null;

            // The presence guard for message-typed fields is likewise driven by the
            // destination: a nullable constructor parameter can receive null when the proto
            // field is unset on the wire; a non-nullable parameter keeps fail-on-missing.
            var expr = BuildProtoToClrExpression(
                field.Reference,
                $"{source}.{field.Name}",
                protoLookup,
                materializer,
                clrNullable: parameter.IsNullable,
                presenceSource: presenceSource,
                destinationNullable: destinationNullable);
            return (expr, false);
        }

        // 2. No direct field — is the parameter's own type a known, constructible contract
        //    reachable from the target's dependency graph?
        if (parameter.Type is not null &&
            constructibleLookup.TryGetValue(parameter.Type, out var nestedContract) &&
            nestedContract.PreferredConstructor is { Parameters.Count: > 0 } nestedCtor)
        {
            if (!visiting.Add(parameter.Type))
            {
                // Cyclic constructor dependency (A -> B -> A).
                return (
                    $"default({parameter.TypeName}) /* TODO: recursive constructor mapping for '{parameter.TypeName}' — map manually */",
                    true);
            }

            try
            {
                var anyReview = false;
                var args = nestedCtor.Parameters.Select(p =>
                {
                    var (argExpr, argReview) = BuildConstructorArgumentExpression(
                        p, source, sourceFields, protoLookup, constructibleLookup, visiting);
                    anyReview |= argReview;
                    return argExpr;
                });

                var expression = $"new {nestedContract.ClrType.ToDisplayString()}({string.Join(", ", args)})";
                return (expression, anyReview);
            }
            finally
            {
                visiting.Remove(parameter.Type);
            }
        }

        // 3. Nothing maps.
        return ($"default({parameter.TypeName}) /* TODO: no '{source}' field maps to '{parameter.Name}' */", true);
    }

    /// <summary>
    /// Converts a proto/gRPC field reference into a CLR expression, recursing through
    /// repeated collections and nested messages as needed.
    ///
    /// IMPORTANT: per <c>ProtoTypeMapper.Map</c>, a repeated field does NOT carry a separate
    /// element reference — the item's own shape (ClrType/IsMessage/IsEnum/...) lives on the
    /// *same* <see cref="ProtoTypeReference"/>, just with <c>IsRepeated = true</c> layered on
    /// top (<c>ElementType</c> is always null in practice). So the element view is obtained by
    /// stripping the repeated flag, not by dereferencing a non-existent nested reference.
    /// </summary>
    public static string BuildProtoToClrExpression(
        ProtoTypeReference reference,
        string source,
        IReadOnlyDictionary<ITypeSymbol, ContractInfo> lookup,
        string collectionMaterializer = ".ToList()",
        ISet<ITypeSymbol>? visiting = null,
        string? clrNamespaceOverride = null,
        bool clrNullable = false,
        string? presenceSource = null,
        bool destinationNullable = false)
    {
        visiting ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        if (reference.IsRepeated)
        {
            var element = reference with { IsRepeated = false };

            const string x = "x";

            // The list-level annotation (clrNullable/presenceSource) is deliberately NOT
            // forwarded: proto repeated fields are never null on the wire, and element
            // nullability is a property of the element view (e.g. List<int?> flows through
            // reference.IsNullable), not of the container.
            var elementExpr = BuildProtoToClrExpression(element, x, lookup, visiting: visiting, clrNamespaceOverride: clrNamespaceOverride);

            var projected = elementExpr == x
                ? source
                : $"{source}.Select({x} => {elementExpr})";

            return $"{projected}{collectionMaterializer}";
        }

        // google.protobuf.Struct -> Dictionary<string, object?>
        if (reference.IsStruct)
        {
            var read = $"{source}.Fields.ToDictionary(" +
                       $"x => x.Key, " +
                       $"x => x.Value.ToObject<object?>())";

            // Struct is a proto message, so the wire may not carry it at all. Honour
            // presence instead of dereferencing null: nullable targets receive null,
            // non-nullable targets receive an empty dictionary ("no map" reads as "empty").
            return clrNullable
                ? $"{source} is null ? null : {read}"
                : $"{source} is null ? new System.Collections.Generic.Dictionary<string, object?>() : {read}";
        }

        // protobuf map — project each value through its (now fully-mapped) value
        // reference so scalar/enum/wrapper conversions are applied exactly like a field.
        if (reference.IsMap)
        {
            var valueRef = reference.MapValueReference
                ?? new ProtoTypeReference { ClrType = reference.ClrType, ProtoTypeName = "string", IsPrimitive = true };

            const string kvp = "kvp";
            var valueExpr = BuildProtoToClrExpression(
                valueRef, $"{kvp}.Value", lookup, visiting: visiting, clrNamespaceOverride: clrNamespaceOverride);

            return $"{source}.ToDictionary({kvp} => {kvp}.Key, {kvp} => {valueExpr})";
        }

        if (reference.IsWrapper)
        {
            // A wrapper message's presence encodes the CLR value's nullability. On the wire a
            // map entry is always present, so the wrapper is never null here; read straight
            // through to its `value` field (applying any inner scalar/enum conversion).
            var inner = reference.WrapperValueReference!;
            return BuildProtoToClrExpression(
                inner, $"{source}.Value", lookup, visiting: visiting, clrNamespaceOverride: clrNamespaceOverride,
                clrNullable: inner.IsNullable, destinationNullable: inner.IsNullable);
        }

        if (reference.IsMessage)
        {
            if (!visiting.Add(reference.ClrType))
            {
                return $"{source} /* TODO: recursive message type '{reference.ClrType.Name}' — map manually */";
            }

            try
            {
                if (lookup.TryGetValue(reference.ClrType, out var nested))
                {
                    if (nested.PreferredConstructor is { Parameters.Count: > 0 } ctor)
                    {
                        var args = ctor.Parameters.Select(p =>
                        {
                            var nestedSourceName = p.SourceFieldName ?? p.Name;

                            var nestedField = nested.Fields.FirstOrDefault(f =>
                                string.Equals(f.Name, nestedSourceName, StringComparison.OrdinalIgnoreCase));

                            return nestedField is null
                                ? $"default({p.TypeName}) /* TODO: could not resolve '{p.Name}' on {nested.Name} */"
                                : BuildProtoToClrExpression(
                                    nestedField.Reference,
                                    $"{source}.{nestedField.Name}",
                                    lookup,
                                    visiting: visiting,
                                    clrNamespaceOverride: clrNamespaceOverride,
                                    clrNullable: nestedField.IsNullable,
                                    presenceSource: NestedPresenceSource(nestedField, source),
                                    destinationNullable: IsNullableDestination(nestedField));
                        });

                        var construction = $"new {QualifyClrType(nested, clrNamespaceOverride)}({string.Join(", ", args)})";

                        // A proto message field may be unset on the wire (generated C# getter
                        // returns null). Nullable targets honour that presence with a null
                        // guard instead of dereferencing null. Constructor-based non-nullable
                        // targets cannot be "emptied out" generically, so they keep the
                        // previous fail-on-missing behaviour (a required value that is absent
                        // surfaces as an exception, same family as DateTime/Guid parsing).
                        return clrNullable
                            ? $"{source} is null ? null : {construction}"
                            : construction;
                    }

                    var assignments = nested.Fields.Select(f =>
                        $"{f.Name} = {BuildProtoToClrExpression(f.Reference, $"{source}.{f.Name}", lookup, visiting: visiting, clrNamespaceOverride: clrNamespaceOverride, clrNullable: f.IsNullable, presenceSource: NestedPresenceSource(f, source), destinationNullable: IsNullableDestination(f))}");

                    var initializer = $"new {QualifyClrType(nested, clrNamespaceOverride)} {{ {string.Join(", ", assignments)} }}";

                    // Same presence guard; the empty-instance fallback is safe here because
                    // this branch only runs when a parameterless construction shape exists.
                    return clrNullable
                        ? $"{source} is null ? null : {initializer}"
                        : $"{source} is null ? new {QualifyClrType(nested, clrNamespaceOverride)}() : {initializer}";
                }

                return $"{source} /* TODO: map nested message '{reference.ClrType.Name}' manually */";
            }
            finally
            {
                visiting.Remove(reference.ClrType);
            }
        }

        return ProtoTypeConversion.ProtoScalarToClr(
            reference, source, clrNamespaceOverride, presenceSource, destinationNullable);
    }

    /// <summary>
    /// Converts a CLR field reference into a proto/gRPC expression, recursing through
    /// repeated collections and nested messages as needed. Repeated results are left as an
    /// <c>IEnumerable&lt;T&gt;</c> projection — the templates feed them to
    /// <c>RepeatedField&lt;T&gt;.AddRange(IEnumerable&lt;T&gt;)</c> (behind a null guard when
    /// the source collection is annotated nullable) without an intermediate <c>.ToList()</c>.
    ///
    /// Used by: server BuildResponseMappings (result -> grpc response)
    ///          client BuildRequestMappings (clr request -> grpc request)
    /// </summary>
    public static string BuildClrToProtoExpression(
        ProtoTypeReference reference,
        string source,
        IReadOnlyDictionary<ITypeSymbol, ContractInfo> lookup,
        ISet<ITypeSymbol>? visiting = null,
        string? protoNamespace = null,
        bool clrNullable = false,
        bool presenceHandledByCaller = false)
    {
        visiting ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        if (reference.IsRepeated)
        {
            var element = reference with { IsRepeated = false };

            const string x = "x";

            // List-level nullability is handled by the caller's template (guarded AddRange);
            // it must not leak onto the element view.
            var elementExpr = BuildClrToProtoExpression(element, x, lookup, visiting, protoNamespace);

            return elementExpr == x
                ? source
                : $"{source}.Select({x} => {elementExpr})";
        }

        // Dictionary<string, object?> -> google.protobuf.Struct
        if (reference.IsStruct)
        {
            // Proto Struct properties accept null; guard a nullable dictionary source
            // instead of letting the extension throw on it.
            return clrNullable
                ? $"{source} is null ? null : {source}.ToStruct()"
                : $"{source}.ToStruct()";
        }
        if (reference.IsMap)
        {
            var valueRef = reference.MapValueReference
                ?? new ProtoTypeReference { ClrType = reference.ClrType, ProtoTypeName = "string", IsPrimitive = true };

            const string kvp = "kvp";
            var valueExpr = BuildClrToProtoExpression(
                valueRef, $"{kvp}.Value", lookup, visiting, protoNamespace, clrNullable: valueRef.IsNullable);

            // Nullable map values are wrapped in a generated message whose presence encodes
            // null; a proto MapField cannot hold a null entry, so null CLR values are dropped.
            return valueRef.IsWrapper
                ? $"{source}.Where({kvp} => {kvp}.Value is not null).ToMapField({kvp} => {kvp}.Key, {kvp} => {valueExpr})"
                : $"{source}.ToMapField({kvp} => {kvp}.Key, {kvp} => {valueExpr})";
        }

        if (reference.IsWrapper)
        {
            var inner = reference.WrapperValueReference!;
            var innerExpr = BuildClrToProtoExpression(
                inner, source, lookup, visiting, protoNamespace, clrNullable: inner.IsNullable);

            return inner.IsNullable
                ? $"{source} is null ? null : new {QualifyProtoType(reference.ProtoTypeName, protoNamespace)} {{ Value = {innerExpr} }}"
                : $"new {QualifyProtoType(reference.ProtoTypeName, protoNamespace)} {{ Value = {innerExpr} }}";
        }

        if (reference.IsMessage)
        {
            if (!visiting.Add(reference.ClrType))
            {
                return $"{source} /* TODO: recursive message type '{reference.ClrType.Name}' — map manually */";
            }

            try
            {
                if (lookup.TryGetValue(reference.ClrType, out var nested))
                {
                    var assignments = nested.Fields.Select(f =>
                        $"{f.Name} = {BuildClrToProtoExpression(f.Reference, $"{source}.{f.Name}", lookup, visiting, protoNamespace, clrNullable: f.IsNullable)}");

                    var construction = $"new {QualifyProtoType(reference.ProtoTypeName, protoNamespace)} {{ {string.Join(", ", assignments)} }}";

                    // A nullable CLR message source maps to null on the proto side (message
                    // properties accept null — that is how proto3 encodes "not set") instead
                    // of dereferencing it. Non-nullable sources stay unguarded: annotating a
                    // contract as required and then passing null is an author error.
                    return clrNullable
                        ? $"{source} is null ? null : {construction}"
                        : construction;
                }

                return $"{source} /* TODO: map nested message '{reference.ClrType.Name}' manually */";
            }
            finally
            {
                visiting.Remove(reference.ClrType);
            }
        }

        return ProtoTypeConversion.ClrScalarToProto(reference, source, protoNamespace, clrNullable, presenceHandledByCaller);
    }

    /// <summary>
    /// The generated <c>HasX</c> accessor for a proto3 <c>optional</c> field read through
    /// <paramref name="sourceOwner"/> (e.g. <c>request.HasPage</c> /
    /// <c>request.Filter.HasMinLevel</c>), or <c>null</c> when the proto field carries no
    /// presence accessor (non-nullable contract annotation, or message-backed shapes).
    /// Uses the same predicate <c>ProtoGenerator</c>/<c>service-proto.sbn</c> applies when
    /// emitting the <c>optional</c> label, so the two can never drift apart.
    /// </summary>
    private static string? NestedPresenceSource(ProtoFieldInfo field, string sourceOwner) =>
        field.IsNullable && ProtoTypeConversion.HasProtoPresenceAccessor(field.Reference)
            ? $"{sourceOwner}.Has{field.Name}"
            : null;

    /// <summary>
    /// Whether the destination property/parameter for this field can receive null: the
    /// nullability annotation (reference types, and <c>Nullable&lt;T&gt;</c> in annotated
    /// contexts) or a <c>Nullable&lt;T&gt;</c> shape visible on the type itself (covers
    /// contracts analyzed from nullable-disabled contexts).
    /// </summary>
    private static bool IsNullableDestination(ProtoFieldInfo field) =>
        field.IsNullable || field.Reference.IsNullable;

    private static string QualifyClrType(ContractInfo contract, string? clrNamespaceOverride)
    {
        return string.IsNullOrWhiteSpace(clrNamespaceOverride)
            ? contract.ClrType.ToDisplayString()
            : $"global::{clrNamespaceOverride}.{contract.Name}";
    }

    private static string QualifyProtoType(string protoTypeName, string? protoNamespace)
    {
        if (string.IsNullOrWhiteSpace(protoNamespace) ||
            protoTypeName.Contains('.', StringComparison.Ordinal))
        {
            return protoTypeName;
        }

        return $"global::{protoNamespace}.{protoTypeName}";
    }

    // ---------------------------------------------------------------------
    // shared
    // ---------------------------------------------------------------------

    /// <summary>
    /// Flattens a contract's dependency tree into a single ClrType -&gt; ContractInfo lookup,
    /// so nested message fields (at any depth) can be resolved without re-walking the tree
    /// on every recursive call.
    /// </summary>
    public static Dictionary<ITypeSymbol, ContractInfo> FlattenByClrType(ContractInfo? root)
    {
        var map = new Dictionary<ITypeSymbol, ContractInfo>(SymbolEqualityComparer.Default);
        if (root is null)
            return map;

        var stack = new Stack<ContractInfo>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!map.TryAdd(current.ClrType, current))
                continue; // already visited (handles cyclic/shared dependencies)

            foreach (var dependency in current.Dependencies ?? [])
                stack.Push(dependency);
        }

        return map;
    }
}
