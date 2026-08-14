
// src/GrpcScaffold.Core/Generation/MappingGenerator.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;

namespace GrpcScaffold.Core.Generation;

/// <summary>
/// Renders the "mapping.sbn" Scriban template for a single discovered endpoint,
/// producing a static class with MapToQuery / MapToResponse methods that bridge the
/// generated gRPC message types and the application's mediator request/response types.
///
/// The generator resolves conversions field-by-field using the <see cref="ProtoTypeReference"/>
/// metadata already computed by <c>ProtoTypeMapper</c> — it never re-inspects raw
/// <see cref="ITypeSymbol"/>s for conversion rules, so request-side and response-side
/// mappings (Guid, DateTime, enums, nested messages, repeated fields, nullability...)
/// are guaranteed to agree with each other.
/// </summary>
public sealed class MappingGenerator(TemplateEngine templates)
{
    public string Generate(EndpointModel endpoint, string grpcNamespace)
    {
        var model = new Dictionary<string, object?>
        {
            ["endpoint_class_name"] = endpoint.EndpointClassName,
            ["grpc_namespace"] = grpcNamespace,
            ["mapping_class_name"] = NamingConventions.MappingClassName(endpoint.EndpointClassName),
            ["service_name"] = endpoint.ServiceName,

            ["request"] = CreateContract(endpoint.Request),
            ["response"] = CreateContract(endpoint.Response),
            ["mediator_message"] = CreateContract(endpoint.MediatorMessage),

            ["has_request"] = endpoint.Request is not null,
            ["has_response"] = endpoint.Response is not null,

            ["query_mapping"] = BuildRequestMappings(endpoint),
            ["response_mapping"] = BuildResponseMappings(endpoint),

            ["usings"] = BuildUsings(endpoint)
        };

        model["grpc_request_type"] = endpoint.Request is null
        ? "Google.Protobuf.WellKnownTypes.Empty"
        : $"{grpcNamespace}.{endpoint.ServiceName}.{endpoint.Request.Name}";

        model["grpc_response_type"] = endpoint.Response is null
                ? "Google.Protobuf.WellKnownTypes.Empty"
                : $"{grpcNamespace}.{endpoint.ServiceName}.{endpoint.Response.Name}";

        return templates.Render("mapping.sbn", model);
    }

    // ---------------------------------------------------------------------
    // Contract -> template-model projection
    // ---------------------------------------------------------------------

    private static Dictionary<string, object?> CreateContract(ContractInfo? contract)
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

    private static IReadOnlyList<string> BuildUsings(EndpointModel endpoint)
    {
        var list = new List<string> { endpoint.EndpointNamespace, endpoint.MediatorMessage.Namespace };

        if (endpoint.Request is not null) list.Add(endpoint.Request.Namespace);
        if (endpoint.Response is not null) list.Add(endpoint.Response.Namespace);

        return list.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }


    // ---------------------------------------------------------------------
    // request.<GrpcField> -> new MediatorMessage(...)
    // ---------------------------------------------------------------------

    private static IReadOnlyList<Dictionary<string, object?>> BuildRequestMappings(EndpointModel endpoint)
    {
        if (endpoint.Request is null)
            return [];

        var ctor = endpoint.MediatorMessage.PreferredConstructor;
        if (ctor is null || ctor.Parameters.Count == 0)
            return [];

        var requestFields = endpoint.Request?.Fields ?? [];

        // Two distinct lookups — they answer two distinct questions:
        //  - requestLookup: "if a Request *field* is itself a nested proto message, what CLR
        //    contract does it correspond to?" (feeds BuildProtoToClrExpression, unchanged).
        //  - mediatorLookup: "if a MediatorMessage constructor *parameter*'s CLR type is itself
        //    a known, constructible contract (e.g. PermissionListQueryFilter, PagedRequest),
        //    what is its constructor?"
        // PermissionListQueryFilter/PagedRequest live in the MediatorMessage's own dependency
        // graph — flattening only endpoint.Request would never surface them, which was the bug.
        var requestLookup = FlattenByClrType(endpoint.Request);
        var mediatorLookup = FlattenByClrType(endpoint.MediatorMessage);

        var visiting = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        return ctor.Parameters
            .Select(parameter =>
            {
                var (expression, needsReview) = BuildConstructorArgumentExpression(
                    parameter, "request", requestFields, requestLookup, mediatorLookup, visiting);

                return new Dictionary<string, object?>
                {
                    ["destination"] = parameter.Name,
                    ["expression"] = expression,
                    ["is_optional"] = parameter.IsOptional,
                    ["has_default"] = parameter.HasDefaultValue,
                    ["needs_review"] = needsReview
                };
            })
            .ToList();
    }

    private static (string Expression, bool NeedsReview) BuildConstructorArgumentExpression(
        ConstructorParameterInfo parameter,
        string source,
        IReadOnlyList<ProtoFieldInfo> requestFields,
        IReadOnlyDictionary<ITypeSymbol, ContractInfo> requestLookup,
        IReadOnlyDictionary<ITypeSymbol, ContractInfo> mediatorLookup,
        ISet<ITypeSymbol> visiting)
    {
        var sourceName = parameter.SourceFieldName ?? parameter.Name;

        // 1. Direct field mapping always wins over nested construction (requirement #5).
        var field = requestFields.FirstOrDefault(f =>
            string.Equals(f.Name, sourceName, StringComparison.OrdinalIgnoreCase));

        if (field is not null)
        {
            var materializer = ProtoTypeConversion.CollectionMaterializer(parameter.TypeName);
            var expr = BuildProtoToClrExpression(field.Reference, $"{source}.{field.Name}", requestLookup, materializer);
            return (expr, false);
        }

        // 2. No direct field — is the parameter's own type a known, constructible contract
        //    reachable from the MediatorMessage's dependency graph?
        if (parameter.Type is not null &&
            mediatorLookup.TryGetValue(parameter.Type, out var nestedContract) &&
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
                        p, source, requestFields, requestLookup, mediatorLookup, visiting);
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
        return ($"default({parameter.TypeName}) /* TODO: no request field maps to '{parameter.Name}' */", true);
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
    private static string BuildProtoToClrExpression(
    ProtoTypeReference reference,
    string source,
    IReadOnlyDictionary<ITypeSymbol, ContractInfo> lookup,
    string collectionMaterializer = ".ToList()",
    ISet<ITypeSymbol>? visiting = null)
    {
        visiting ??= new HashSet<ITypeSymbol>(
            SymbolEqualityComparer.Default);

        if (reference.IsRepeated)
        {
            var element = reference with
            {
                IsRepeated = false
            };

            const string x = "x";

            var elementExpr = BuildProtoToClrExpression(
                element,
                x,
                lookup,
                visiting: visiting);

            var projected =
                elementExpr == x
                    ? source
                    : $"{source}.Select({x} => {elementExpr})";

            return $"{projected}{collectionMaterializer}";
        }

        // google.protobuf.Struct -> Dictionary<string, object?>
        if (reference.IsStruct)
        {
            return $"{source}.Fields.ToDictionary(" +
                   $"x => x.Key, " +
                   $"x => x.Value.ToObject<object?>())";
        }

        // protobuf map
        if (reference.IsMap)
        {
            return $"{source}.ToDictionary(x => x.Key, x => x.Value)";
        }

        if (reference.IsMessage)
        {
            if (!visiting.Add(reference.ClrType))
            {
                return $"{source} /* TODO: recursive message type '{reference.ClrType.Name}' — map manually */";
            }

            try
            {
                if (lookup.TryGetValue(
                        reference.ClrType,
                        out var nested) &&
                    nested.PreferredConstructor is not null)
                {
                    var args =
                        nested.PreferredConstructor.Parameters.Select(p =>
                        {
                            var nestedSourceName =
                                p.SourceFieldName ?? p.Name;

                            var nestedField =
                                nested.Fields.FirstOrDefault(f =>
                                    string.Equals(
                                        f.Name,
                                        nestedSourceName,
                                        StringComparison.OrdinalIgnoreCase));

                            return nestedField is null
                                ? $"default({p.TypeName}) /* TODO: could not resolve '{p.Name}' on {nested.Name} */"
                                : BuildProtoToClrExpression(
                                    nestedField.Reference,
                                    $"{source}.{nestedField.Name}",
                                    lookup,
                                    visiting: visiting);
                        });

                    return $"new {nested.ClrType.ToDisplayString()}({string.Join(", ", args)})";
                }

                return $"{source} /* TODO: map nested message '{reference.ClrType.Name}' manually */";
            }
            finally
            {
                visiting.Remove(reference.ClrType);
            }
        }

        return ProtoTypeConversion.ProtoScalarToClr(
            reference,
            source);
    }   

    // ---------------------------------------------------------------------
    // result.<ClrField> -> new GrpcResponse { ... }
    // ---------------------------------------------------------------------

    private static IReadOnlyList<Dictionary<string, object?>> BuildResponseMappings(EndpointModel endpoint)
    {
        if (endpoint.Response is null)
            return [];

        var lookup = FlattenByClrType(endpoint.Response);

        return [.. endpoint.Response.Fields
            .Select(field =>
            {
                var expression = BuildClrToProtoExpression(field.Reference, $"result.{field.Name}", lookup);
                return new Dictionary<string, object?>
                {
                    ["destination"] = field.Name,
                    ["expression"] = expression,
                    ["is_repeated"] = field.Reference.IsRepeated,
                    ["needs_review"] = expression.Contains("/* TODO", StringComparison.Ordinal)
                };
            })];
    }

    /// <summary>
    /// Converts a CLR field reference into a proto/gRPC expression, recursing through
    /// repeated collections and nested messages as needed. Repeated results are left as an
    /// <c>IEnumerable&lt;T&gt;</c> projection — <c>RepeatedField&lt;T&gt;.Add(IEnumerable&lt;T&gt;)</c>
    /// lets the template assign them via collection-initializer syntax (<c>Field = { expr }</c>)
    /// without an intermediate <c>.ToList()</c>.
    /// </summary>
    private static string BuildClrToProtoExpression(
    ProtoTypeReference reference,
    string source,
    IReadOnlyDictionary<ITypeSymbol, ContractInfo> lookup,
    ISet<ITypeSymbol>? visiting = null)
    {
        visiting ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        if (reference.IsRepeated)
        {
            var element = reference with { IsRepeated = false };

            const string x = "x";

            var elementExpr = BuildClrToProtoExpression(
                element,
                x,
                lookup,
                visiting);

            return elementExpr == x
                ? source
                : $"{source}.Select({x} => {elementExpr})";
        }

        // Dictionary<string, object?> -> google.protobuf.Struct
        if (reference.IsStruct)
        {
            return $"Google.Protobuf.WellKnownTypes.Struct.FromDictionary({source})";
        }

        // Real protobuf map
        if (reference.IsMap)
        {
            // TODO: map conversion
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
                        $"{f.Name} = {BuildClrToProtoExpression(
                            f.Reference,
                            $"{source}.{f.Name}",
                            lookup,
                            visiting)}");

                    return $"new {reference.ProtoTypeName} {{ {string.Join(", ", assignments)} }}";
                }

                return $"{source} /* TODO: map nested message '{reference.ClrType.Name}' manually */";
            }
            finally
            {
                visiting.Remove(reference.ClrType);
            }
        }

        return ProtoTypeConversion.ClrScalarToProto(reference, source);
    }
    // ---------------------------------------------------------------------
    // shared
    // ---------------------------------------------------------------------

    /// <summary>
    /// Flattens a contract's dependency tree into a single ClrType -&gt; ContractInfo lookup,
    /// so nested message fields (at any depth) can be resolved without re-walking the tree
    /// on every recursive call.
    /// </summary>
    private static Dictionary<ITypeSymbol, ContractInfo> FlattenByClrType(ContractInfo? root)
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
