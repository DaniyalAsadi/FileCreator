// src/GrpcScaffold.Core/Generation/ProtoTypeConversion.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

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
    public static string ProtoScalarToClr(ProtoTypeReference reference, string source)
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
        var clrDisplay = clr.ToDisplayString();
        var isNullableStruct = reference.IsNullable && clr.IsValueType;
        var kind = Classify(clr);

        if (reference.IsEnum)
        {
            return isNullableStruct
                ? $"{source} is null ? ({clrDisplay}?)null : ({clrDisplay}){source}"
                : $"({clrDisplay}){source}";
        }

        // Only a proto message (Timestamp) can itself be null on the wire; string-backed
        // scalars (Guid/DateOnly/decimal) are never null, so there's nothing to guard there —
        // a missing value simply fails to parse, which is the correct behavior to surface.
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

        if (!isNullableStruct)
            return Convert(source);

        return sourceIsProtoMessage
            ? $"{source} is null ? ({clrDisplay}?)null : {Convert(source)}"
            : Convert(source);
    }

    /// <summary>
    /// Converts a single CLR scalar value (<paramref name="source"/>) into its proto/gRPC
    /// equivalent. Does not handle repeated or message types.
    /// </summary>
    public static string ClrScalarToProto(ProtoTypeReference reference, string source)
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
            var expr = $"({reference.ProtoTypeName}){accessor}";
            return isNullableStruct ? $"{source} is null ? default : {expr}" : expr;
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

        if (!isNullableStruct)
            return Convert(source);

        // Guid/DateOnly/Decimal map to proto `string`, and DateTime/DateTimeOffset map to
        // the Timestamp *message* — both are reference types in generated C#, so `null` is
        // a valid value to assign. Every other nullable value type (int?, long?, bool?,
        // float?, double? — ScalarKind.None) maps to a plain, non-optional proto3 scalar,
        // whose generated property is a non-nullable value type (`int`, not `int?`).
        // Assigning `null` there does not compile (this was a real bug in the previous
        // implementation). proto3 has no way to represent "unset" on a plain scalar anyway
        // — the wire default *is* the absence of a value — so collapsing to `default` here
        // mirrors exactly what the enum branch above already does, not a new invented rule.
        return kind == ScalarKind.None
            ? $"{source} is null ? default : {accessor}"
            : $"{source} is null ? null : {Convert(accessor)}";
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
