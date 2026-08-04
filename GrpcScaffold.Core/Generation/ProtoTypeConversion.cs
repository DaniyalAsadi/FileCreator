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
/// </summary>
internal static class ProtoTypeConversion
{
    /// <summary>Unwraps <c>Nullable&lt;T&gt;</c> down to <c>T</c>; returns the type unchanged otherwise.</summary>
    public static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } named
            ? named.TypeArguments[0]
            : type;

    /// <summary>Whether this scalar type requires an explicit conversion (as opposed to a 1:1 passthrough).</summary>
    public static bool NeedsCast(ProtoTypeReference reference)
    {
        var clr = UnwrapNullable(reference.ClrType);
        return clr.Name is "Guid" or "DateTime" or "DateTimeOffset" or "DateOnly" or "Decimal";
    }

    /// <summary>
    /// Converts a single proto/gRPC scalar value (<paramref name="source"/>) into its CLR
    /// equivalent. Does not handle repeated or message types — see
    /// <see cref="MappingGenerator"/> for how those wrap this.
    /// </summary>
    public static string ProtoScalarToClr(ProtoTypeReference reference, string source)
    {
        var clr = UnwrapNullable(reference.ClrType);
        var clrDisplay = clr.ToDisplayString();
        var isNullableStruct = reference.IsNullable && clr.IsValueType;

        if (reference.IsEnum)
        {
            return isNullableStruct
                ? $"{source} is null ? ({clrDisplay}?)null : ({clrDisplay}){source}"
                : $"({clrDisplay}){source}";
        }

        // NOTE: this must mirror ProtoTypeMapper.Map's scalar table exactly. Today that's:
        //   Guid -> string, DateTime/DateTimeOffset -> Timestamp, DateOnly -> string, decimal -> string.
        // (TimeOnly has no case in ProtoTypeMapper and falls through to IsMessage=true; that's
        // handled by the message branch in MappingGenerator, not here.)
        var sourceIsProtoMessage = clr.Name is "DateTime" or "DateTimeOffset";

        string Convert(string s) => clr.Name switch
        {
            "Guid" => $"Guid.Parse({s})",
            "DateTime" => $"{s}.ToDateTime()",
            "DateTimeOffset" => $"{s}.ToDateTimeOffset()",
            "DateOnly" => $"DateOnly.Parse({s}, System.Globalization.CultureInfo.InvariantCulture)",
            "Decimal" => $"decimal.Parse({s}, System.Globalization.CultureInfo.InvariantCulture)",
            _ => s
        };

        if (!isNullableStruct)
            return Convert(source);

        // Only a proto message (Timestamp) can itself be null on the wire; string-backed
        // scalars (Guid/DateOnly/decimal) are never null, so there's nothing to guard there —
        // a missing value simply fails to parse, which is the correct behavior to surface.
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
        var clr = UnwrapNullable(reference.ClrType);
        var isNullableStruct = reference.IsNullable && clr.IsValueType;
        var accessor = isNullableStruct ? $"{source}.Value" : source;

        if (reference.IsEnum)
        {
            var expr = $"({reference.ProtoTypeName}){accessor}";
            return isNullableStruct ? $"{source} is null ? default : {expr}" : expr;
        }

        // Mirrors ProtoTypeMapper.Map: Guid/DateOnly/decimal all become proto `string`.
        string Convert(string s) => clr.Name switch
        {
            "Guid" => $"{s}.ToString()",
            "DateTime" => $"Timestamp.FromDateTime({s}.ToUniversalTime())",
            "DateTimeOffset" => $"Timestamp.FromDateTime({s}.UtcDateTime)",
            "DateOnly" => $"{s}.ToString(\"O\", System.Globalization.CultureInfo.InvariantCulture)",
            "Decimal" => $"{s}.ToString(System.Globalization.CultureInfo.InvariantCulture)",
            _ => s
        };

        if (!isNullableStruct)
            return Convert(source);

        return $"{source} is null ? null : {Convert(accessor)}";
    }

    /// <summary>Picks a materializer for a repeated proto field landing in a constructor parameter of <paramref name="parameterTypeName"/>.</summary>
    public static string CollectionMaterializer(string parameterTypeName) => parameterTypeName switch
    {
        _ when parameterTypeName.EndsWith("[]", StringComparison.Ordinal) => ".ToArray()",
        _ when parameterTypeName.Contains("ImmutableArray<", StringComparison.Ordinal) => ".ToImmutableArray()",
        _ when parameterTypeName.Contains("ImmutableList<", StringComparison.Ordinal) => ".ToImmutableList()",
        _ when parameterTypeName.Contains("ImmutableHashSet<", StringComparison.Ordinal) => ".ToImmutableHashSet()",
        _ when parameterTypeName.Contains("HashSet<", StringComparison.Ordinal) => ".ToHashSet()",
        _ => ".ToList()" // List<T> / IReadOnlyList<T> / IReadOnlyCollection<T> / IEnumerable<T>
    };
}
