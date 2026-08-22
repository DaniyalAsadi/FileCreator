// src/GrpcScaffold.Core/Generation/ProtoTypeConversion.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;

namespace GrpcScaffold.Core.Generation;


/// <summary>
/// Builds the two directions of scalar conversion that every field mapping needs:
/// proto/gRPC wire value -&gt; CLR value (used by <c>MapToQuery</c>), and
/// CLR value -&gt; proto/gRPC wire value (used by <c>MapToResponse</c>).
///
/// This is the single source of truth for "how do we convert a Guid / DateTime / enum /
/// etc.", so <see cref="MappingGenerator"/> never re-derives conversion rules from raw
/// <see cref="ITypeSymbol"/> switches in two different places (which is how the two
/// directions used to drift apart).
///
/// <see cref="Classify"/> is the ONLY place that decides "what kind of well-known scalar
/// is this CLR type" — every other method in this class (and, transitively, every caller)
/// goes through it instead of re-matching on <see cref="ITypeSymbol.Name"/>.
///
/// IMPORTANT: this must stay in lockstep with <c>ProtoTypeMapper.Map</c>'s scalar table.
/// Today that table is: Guid -&gt; string, DateTime/DateTimeOffset -&gt; Timestamp,
/// DateOnly -&gt; string, decimal -&gt; string. Everything else recognized by
/// <c>ProtoTypeMapper</c> as <c>IsPrimitive</c> (bool/int/long/float/double/string) needs
/// no conversion at all and passes through unchanged.
///
/// Types <c>ProtoTypeMapper</c> does NOT actually map as scalars — byte, sbyte, short,
/// ushort, uint, ulong, char, TimeOnly, byte[], ReadOnlyMemory&lt;byte&gt;,
/// Memory&lt;byte&gt; — are deliberately absent here. They fall through
/// <c>ProtoTypeMapper.Map</c> to <c>IsMessage = true</c> today (a pre-existing gap in that
/// class, not something to paper over here), so they never reach these methods; adding
/// conversion cases for them in this class would be dead code at best and misleading at
/// worst. See the accompanying analysis for details.
/// </summary>
internal static class ProtoTypeConversion
{
    /// <summary>
    /// The well-known CLR scalar kinds that require an explicit conversion rather than a
    /// straight passthrough. This is the single classification every other method defers to.
    /// </summary>
    private enum ScalarKind
    {
        None,
        Guid,
        Decimal,
        DateOnly,
        DateTime,
        DateTimeOffset,

        Dictionary
    }

    /// <summary>
    /// Whether the proto field generated for this reference gets a proto3 <c>optional</c>
    /// presence accessor in C# (<c>Has&lt;Name&gt;</c> / <c>Clear&lt;Name&gt;</c>).
    ///
    /// <c>ProtoGenerator</c> emits the <c>optional</c> label for every nullable-annotated
    /// contract field, but only scalar/enum/string fields actually gain <c>HasX</c> in the
    /// generated C# — message-backed shapes (Timestamp for DateTime/DateTimeOffset,
    /// google.protobuf.Struct, maps, repeated fields, nested messages) keep their natural
    /// null-based presence instead. Callers AND this with the field's nullability
    /// annotation to decide whether a <c>HasX</c> accessor exists.
    /// </summary>
    public static bool HasProtoPresenceAccessor(ProtoTypeReference reference)
    {
        if (reference.IsRepeated || reference.IsMessage || reference.IsStruct || reference.IsMap)
            return false;

        var kind = Classify(UnwrapNullable(reference.ClrType));
        return kind is not (ScalarKind.DateTime or ScalarKind.DateTimeOffset);
    }

    /// <summary>Unwraps <c>Nullable&lt;T&gt;</c> down to <c>T</c>; returns the type unchanged otherwise.</summary>
    public static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } named
            ? named.TypeArguments[0]
            : type;

    /// <summary>
    /// Classifies an (already-unwrapped) CLR type as one of the well-known scalar kinds
    /// this class knows how to convert, or <see cref="ScalarKind.None"/> if it's a plain
    /// proto3 primitive (bool/int/long/float/double/string) that needs no conversion, or a
    /// type this class simply doesn't recognize.
    ///
    /// Identification deliberately avoids bare <see cref="ITypeSymbol.Name"/> comparisons
    /// (a type named "Guid" in an unrelated namespace must not match): <c>decimal</c> is
    /// identified via <see cref="SpecialType.System_Decimal"/> (guaranteed by the runtime,
    /// no namespace ambiguity possible); everything else is identified via the combination
    /// of its immediate containing namespace ("System") and its short name, since none of
    /// Guid/DateTime/DateTimeOffset/DateOnly have a dedicated <see cref="SpecialType"/>.
    /// </summary>
    private static ScalarKind Classify(ITypeSymbol unwrapped)
    {
        if (unwrapped.SpecialType == SpecialType.System_Decimal)
            return ScalarKind.Decimal;

        if (unwrapped is not INamedTypeSymbol named)
            return ScalarKind.None;

        if (named.ContainingNamespace.Name == "System")
        {
            return named.Name switch
            {
                "Guid" => ScalarKind.Guid,
                "DateOnly" => ScalarKind.DateOnly,
                "DateTime" => ScalarKind.DateTime,
                "DateTimeOffset" => ScalarKind.DateTimeOffset,
                _ => ScalarKind.None
            };
        }

        if (IsDictionaryType(named))
            return ScalarKind.Dictionary;

        return ScalarKind.None;
    }

    /// <summary>
    /// Whether this scalar type requires an explicit conversion (as opposed to a 1:1
    /// passthrough). Kept for source compatibility with existing callers; identical
    /// semantics to before (does not include enums — those are handled by
    /// <see cref="ProtoFieldInfo.Reference"/>.<c>IsEnum</c> separately), just re-implemented
    /// on top of <see cref="Classify"/> instead of a standalone <c>.Name</c> switch.
    /// </summary>
    public static bool NeedsCast(ProtoTypeReference reference) =>
        Classify(UnwrapNullable(reference.ClrType)) != ScalarKind.None;

    /// <summary>Whether converting the proto/gRPC wire value for this field into its CLR value requires an explicit conversion.</summary>
    public static bool RequiresProtoToClrConversion(ProtoTypeReference reference) =>
        reference.IsEnum || Classify(UnwrapNullable(reference.ClrType)) != ScalarKind.None;

    /// <summary>Whether converting the CLR value for this field into its proto/gRPC wire value requires an explicit conversion.</summary>
    public static bool RequiresClrToProtoConversion(ProtoTypeReference reference) =>
        reference.IsEnum || Classify(UnwrapNullable(reference.ClrType)) != ScalarKind.None;

    /// <summary>
    /// Converts a single proto/gRPC scalar value (<paramref name="source"/>) into its CLR
    /// equivalent. Does not handle repeated or message types — see
    /// <see cref="MappingGenerator"/> for how those wrap this.
    /// </summary>
    /// <param name="presenceSource">
    /// The generated <c>HasX</c> accessor for the source proto field (e.g.
    /// <c>request.HasPage</c>), when the field was emitted as a proto3 <c>optional</c>
    /// scalar/enum/string — <see cref="HasProtoPresenceAccessor"/>. <c>null</c> when the
    /// field has no presence accessor; "unset" then falls back to the wire default,
    /// matching plain proto3 semantics.
    /// </param>
    /// <param name="destinationNullable">
    /// Whether the assignment destination (mediator ctor parameter / BFF contract property)
    /// can actually receive null — the caller computes it from the destination's nullability
    /// annotation or <c>Nullable&lt;T&gt;</c> shape. Null-guards are only emitted when this
    /// is true, so non-nullable destinations keep their existing fail-on-missing behavior.
    /// </param>
    public static string ProtoScalarToClr(
        ProtoTypeReference reference,
        string source,
        string? clrNamespaceOverride = null,
        string? presenceSource = null,
        bool destinationNullable = false)
    {
        if (reference.IsRepeated || reference.IsMessage)
        {
            // Contract violation: callers must strip IsRepeated / route IsMessage through
            // the message-graph logic in MappingGenerator before reaching this method.
            throw new InvalidOperationException(
                $"{nameof(ProtoScalarToClr)} does not handle repeated or message references " +
                $"(got '{reference.ClrType.ToDisplayString()}', IsRepeated={reference.IsRepeated}, IsMessage={reference.IsMessage}).");
        }

        var clr = UnwrapNullable(reference.ClrType);
        var clrDisplay = QualifyClrType(clr, clrNamespaceOverride);
        var kind = Classify(clr);

        if (reference.IsEnum)
        {
            var cast = $"({clrDisplay}){source}";

            // proto3 `optional enum` tracks "unset" via HasX instead of the zero value (gap
            // #6). Without a presence accessor proto3 enums cannot represent unset — the
            // plain wire cast is all we can do (this also replaces the old dead `is null`
            // guard, which could never fire for a non-nullable value type).
            return destinationNullable && presenceSource is not null
                ? $"{presenceSource} ? {cast} : ({clrDisplay}?)null"
                : cast;
        }

        // Only a proto message (Timestamp) can itself be null on the wire; string-backed
        // scalars (Guid/DateOnly/decimal) are never null on the wire — their "unset" state
        // is the empty string, which only becomes CLR null through a presence guard below.
        var sourceIsProtoMessage = kind is ScalarKind.DateTime or ScalarKind.DateTimeOffset;

        string Convert(string s) => kind switch
        {
            ScalarKind.Guid => $"Guid.Parse({s})",
            ScalarKind.DateTime => $"{s}.ToDateTime()",
            ScalarKind.DateTimeOffset => $"{s}.ToDateTimeOffset()",
            ScalarKind.DateOnly => $"DateOnly.Parse({s}, System.Globalization.CultureInfo.InvariantCulture)",
            ScalarKind.Decimal => $"decimal.Parse({s}, System.Globalization.CultureInfo.InvariantCulture)",
            ScalarKind.Dictionary => $"{{ {s}.Properties.ToMapField() }}",
            ScalarKind.None => s,
            _ => s // plain proto3 primitive (bool/int/long/float/double/string) — no conversion needed.
        };

        // Non-nullable destination: unchanged — a missing/unset value surfaces as the wire
        // default or a parse failure, which is the documented fail-fast behavior.
        if (!destinationNullable)
            return Convert(source);

        // Timestamp-backed nullables: the proto property itself is null when unset.
        if (sourceIsProtoMessage)
            return $"{source} is null ? ({clrDisplay}?)null : {Convert(source)}";

        // proto3 `optional` scalar/enum/string field with a nullable destination (gaps
        // #5/#6): an unset wire field maps to null instead of 0 / "" / a parse exception.
        if (presenceSource is not null)
        {
            // string is the only nullable reference-typed scalar; it needs no conversion
            // and no (T?) cast on the null branch.
            if (kind == ScalarKind.None && !clr.IsValueType)
                return $"{presenceSource} ? {source} : null";

            return $"{presenceSource} ? {Convert(source)} : ({clrDisplay}?)null";
        }

        // Nullable destination but no presence accessor (e.g. contracts analyzed from a
        // nullable-disabled context): keep the previous pass-through/parse behavior.
        return Convert(source);
    }

    /// <summary>
    /// Converts a single CLR scalar value (<paramref name="source"/>) into its proto/gRPC
    /// equivalent. Does not handle repeated or message types.
    /// </summary>
    /// <param name="clrNullable">
    /// Whether the CLR source is annotated as a nullable reference type (e.g. <c>string?</c>).
    /// Reference-type nullability never flows through <see cref="ProtoTypeReference.IsNullable"/>
    /// (that flag only tracks <c>Nullable&lt;T&gt;</c>), so callers pass it explicitly.
    /// </param>
    /// <param name="presenceHandledByCaller">
    /// When true, the target proto field was emitted as proto3 <c>optional</c> and the
    /// caller's template guards the whole assignment with
    /// <c>if (source is not null) target = &lt;expr&gt;;</c> (gap #6: assigning a value — even
    /// <c>default</c> — through the generated property would set the presence bit, turning
    /// CLR null into "zero, but present" on the wire). In this mode the returned expression
    /// is the plain non-null conversion; no inline null handling is emitted. Nested message
    /// fields always pass <c>false</c> and keep the inline fallbacks.
    /// </param>
    public static string ClrScalarToProto(ProtoTypeReference reference, string source, string? protoNamespace = null, bool clrNullable = false, bool presenceHandledByCaller = false)
    {
        if (reference.IsRepeated || reference.IsMessage)
        {
            throw new InvalidOperationException(
                $"{nameof(ClrScalarToProto)} does not handle repeated or message references " +
                $"(got '{reference.ClrType.ToDisplayString()}', IsRepeated={reference.IsRepeated}, IsMessage={reference.IsMessage}).");
        }

        var clr = UnwrapNullable(reference.ClrType);
        var isNullableStruct = reference.IsNullable && clr.IsValueType;
        var accessor = isNullableStruct ? $"{source}.Value" : source;
        var kind = Classify(clr);

        if (reference.IsEnum)
        {
            var expr = $"({QualifyProtoType(reference.ProtoTypeName, protoNamespace)}){accessor}";
            return isNullableStruct && !presenceHandledByCaller ? $"{source} is null ? default : {expr}" : expr;
        }

        string Convert(string s) => kind switch
        {
            ScalarKind.Guid => $"{s}.ToString()",
            ScalarKind.DateTime => $"Timestamp.FromDateTime({s}.ToUniversalTime())",
            ScalarKind.DateTimeOffset => $"Timestamp.FromDateTime({s}.UtcDateTime)",
            ScalarKind.DateOnly => $"{s}.ToString(\"O\", System.Globalization.CultureInfo.InvariantCulture)",
            ScalarKind.Decimal => $"{s}.ToString(System.Globalization.CultureInfo.InvariantCulture)",
            ScalarKind.Dictionary => $"{{ {s}.Properties.ToMapField() }}",
            ScalarKind.None => s,
            _ => s
        };

        // Nullable reference type (string? is the only nullable scalar reference type — every
        // other ScalarKind is a struct). The generated proto `string` setter rejects null via
        // pb::ProtoPreconditions.CheckNotNull, so null collapses to the proto3 wire default "".
        if (clrNullable && !isNullableStruct && clr.SpecialType == SpecialType.System_String)
            return presenceHandledByCaller ? source : $"{source} ?? string.Empty";

        if (!isNullableStruct)
            return Convert(source);

        // The caller emits `if (source is not null) { grpc.X = <expr>; }` — just the value.
        if (presenceHandledByCaller)
            return kind == ScalarKind.None ? accessor : Convert(accessor);

        return kind switch
        {
            // Plain proto3 scalars (int?, long?, bool?, float?, double?) land on non-nullable
            // value-type properties (`int`, not `int?`). Assigning `null` there does not
            // compile (this was a real bug in a previous implementation). proto3 has no way
            // to represent "unset" on a plain scalar anyway — the wire default *is* the
            // absence of a value — so collapsing to `default` mirrors exactly what the enum
            // branch above already does, not a new invented rule.
            ScalarKind.None => $"{source} is null ? default : {accessor}",

            // Guid/DateOnly/decimal map to proto `string`, whose generated setter throws
            // ArgumentNullException on null (CheckNotNull) — `null` is NOT a valid assignment
            // despite `string` being a reference type. proto3's wire default for strings is
            // "", so null collapses to string.Empty instead (same family as the `default`
            // rule above, not a new invented rule).
            ScalarKind.Guid or ScalarKind.DateOnly or ScalarKind.Decimal
                => $"{source} is null ? string.Empty : {Convert(accessor)}",

            // DateTime/DateTimeOffset map to the Timestamp *message*, whose generated property
            // is a plain reference type without null checks — `null` is a valid assignment
            // there and correctly encodes "no value".
            _ => $"{source} is null ? null : {Convert(accessor)}"
        };
    }

    /// <summary>
    /// Picks a materializer for a repeated proto field landing in a constructor parameter of
    /// <paramref name="parameterTypeName"/>. Legacy string-based overload, kept for callers
    /// that don't have an <see cref="ITypeSymbol"/> handy. Prefer
    /// <see cref="CollectionMaterializer(ITypeSymbol)"/> — this overload matches by
    /// substring, which can false-positive on an unrelated type whose display name merely
    /// contains e.g. "ImmutableArray&lt;" (a real, if unlikely, hazard this overload has
    /// always had).
    /// </summary>
    public static string CollectionMaterializer(string parameterTypeName) => parameterTypeName switch
    {
        _ when parameterTypeName.EndsWith("[]", StringComparison.Ordinal) => ".ToArray()",
        _ when parameterTypeName.Contains("ImmutableArray<", StringComparison.Ordinal) => ".ToImmutableArray()",
        _ when parameterTypeName.Contains("ImmutableList<", StringComparison.Ordinal) => ".ToImmutableList()",
        _ when parameterTypeName.Contains("ImmutableHashSet<", StringComparison.Ordinal) => ".ToImmutableHashSet()",
        _ when parameterTypeName.Contains("HashSet<", StringComparison.Ordinal) => ".ToHashSet()",
        _ => ".ToList()" // List<T> / IReadOnlyList<T> / IReadOnlyCollection<T> / IEnumerable<T>
    };

    /// <summary>
    /// Symbol-based materializer selection. Identifies the destination collection shape via
    /// <see cref="IArrayTypeSymbol"/> / <see cref="INamedTypeSymbol.OriginalDefinition"/> +
    /// containing namespace, instead of matching against a display-name string — so it can't
    /// be fooled by an unrelated type whose name happens to contain "ImmutableArray&lt;".
    /// </summary>
    public static string CollectionMaterializer(ITypeSymbol parameterType)
    {
        if (parameterType is IArrayTypeSymbol)
            return ".ToArray()";

        if (parameterType is INamedTypeSymbol { IsGenericType: true } named)
        {
            var definition = named.OriginalDefinition;
            var ns = definition.ContainingNamespace?.ToDisplayString();

            return (ns, definition.Name) switch
            {
                ("System.Collections.Immutable", "ImmutableArray") => ".ToImmutableArray()",
                ("System.Collections.Immutable", "ImmutableList") => ".ToImmutableList()",
                ("System.Collections.Immutable", "ImmutableHashSet") => ".ToImmutableHashSet()",
                ("System.Collections.Generic", "HashSet") => ".ToHashSet()",
                _ => ".ToList()" // List<T> / IList<T> / IReadOnlyList<T> / IReadOnlyCollection<T> / ICollection<T> / IEnumerable<T>
            };
        }

        return ".ToList()";
    }

    private static string QualifyClrType(ITypeSymbol clrType, string? clrNamespaceOverride)
    {
        if (!string.IsNullOrWhiteSpace(clrNamespaceOverride) &&
            clrType.ContainingNamespace?.ToDisplayString() != "System" &&
            (clrType.TypeKind is TypeKind.Enum or TypeKind.Class ||
             clrType.TypeKind == TypeKind.Struct && clrType.SpecialType == SpecialType.None))
        {
            return $"global::{clrNamespaceOverride}.{clrType.Name}";
        }

        return clrType.ToDisplayString();
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

    private static bool IsDictionaryType(INamedTypeSymbol type)
    {
        if (type.TypeArguments.Length != 2)
            return false;

        var originalDefinition = type.OriginalDefinition;

        return originalDefinition.ContainingNamespace.ToDisplayString() switch
        {
            "System.Collections.Generic"
                => originalDefinition.Name switch
                {
                    "Dictionary" => true,
                    "IDictionary" => true,
                    "IReadOnlyDictionary" => true,
                    _ => false
                },

            _ => false
        };
    }
}
