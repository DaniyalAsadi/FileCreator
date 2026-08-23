// src/GrpcScaffold.Core/Generation/ProtoTypeMapper.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;
using System.Text;

namespace GrpcScaffold.Core.Generation;

public static class ProtoTypeMapper
{
    public static IReadOnlyList<ProtoFieldInfo> ExtractFields(ITypeSymbol? type)
    {
        if (type is null)
            return [];

        var properties = GetProperties(type);

        var fields = new List<ProtoFieldInfo>(properties.Count);

        var fieldNumber = 1;

        foreach (var property in properties)
        {
            fields.Add(new ProtoFieldInfo(
                Name: property.Name,
                ProtoName: ToSnakeCase(property.Name),
                Reference: Map(property.Type),
                IsNullable: property.NullableAnnotation is NullableAnnotation.Annotated,
                FieldNumber: fieldNumber++)
            {
                DeclaredClrType = property.Type
            });
        }

        return fields;
    }

    private static List<IPropertySymbol> GetProperties(ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();

        for (var current = type; current is not null; current = current.BaseType)
        {
            properties.AddRange(
                current.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p =>
                        p.DeclaredAccessibility == Accessibility.Public &&
                        !p.IsStatic &&
                        p.GetMethod is not null));
        }

        return [.. properties
                .DistinctBy(x => x.Name)
                .OrderBy(x => x.Name)];
    }

    private static ProtoTypeReference Map(ITypeSymbol type)
    {
        // Nullable<T>
        if (type is INamedTypeSymbol
            {
                IsGenericType: true,
                Name: "Nullable"
            } nullable)
        {
            return Map(nullable.TypeArguments[0]) with
            {
                IsNullable = true
            };
        }
        // Dictionary
        if (type is INamedTypeSymbol dictionary &&
            dictionary.IsGenericType &&
            dictionary.OriginalDefinition is INamedTypeSymbol definition &&
            definition.ContainingNamespace.ToDisplayString() == "System.Collections.Generic" &&
            definition.Name is "Dictionary" or "IDictionary" or "IReadOnlyDictionary")
        {
            var keyType = dictionary.TypeArguments[0];
            var valueType = dictionary.TypeArguments[1];

            // Dictionary<string, object?> -> google.protobuf.Struct
            if (keyType.SpecialType == SpecialType.System_String &&
                valueType.SpecialType == SpecialType.System_Object)
            {
                return new ProtoTypeReference
                {
                    ClrType = type,
                    ProtoTypeName = "google.protobuf.Struct",
                    IsWellKnownType = true,
                    IsStruct = true
                };
            }

            // Run the key and value through the SAME mapping pipeline a field would use, so
            // they get a proper proto type name (string / int32 / Timestamp / ...) and
            // nullability tracking. Previously only the raw CLR symbol was stored and then
            // `.ToDisplayString()`'d, which leaked `string?` into the generated .proto as
            // `map<string, string?>` — invalid protobuf syntax.
            var keyReference = Map(keyType);
            var valueReference = Map(valueType);

            // Map() only tracks Nullable<T>; capture reference-type nullability (string?)
            // so a nullable map value is never silently collapsed to the non-null proto type.
            if (keyType.NullableAnnotation == NullableAnnotation.Annotated && !keyReference.IsNullable)
                keyReference = keyReference with { IsNullable = true };
            if (valueType.NullableAnnotation == NullableAnnotation.Annotated && !valueReference.IsNullable)
                valueReference = valueReference with { IsNullable = true };

            // A proto3 map value cannot carry nullability on its own (no `optional` for map
            // values, and scalar/enum values have no presence bit). A nullable map value
            // would therefore lose its null/presence semantic unless we wrap it in a message
            // whose presence encodes the null. Message-backed values (Timestamp, nested
            // messages, Struct) already preserve presence, so they need no wrapper.
            var valueNeedsWrapper = valueReference.IsNullable
                && !valueReference.IsMessage
                && !valueReference.IsStruct
                && !valueReference.IsWellKnownType
                && !valueReference.IsRepeated;

            if (valueNeedsWrapper)
            {
                var wrapperName = "Nullable" + SanitizeWrapperName(valueReference.ProtoTypeName);

                valueReference = new ProtoTypeReference
                {
                    ClrType = valueReference.ClrType,
                    ProtoTypeName = wrapperName,
                    IsMessage = true,
                    IsWrapper = true,
                    IsNullable = true,
                    WrapperValueReference = valueReference
                };
            }

            return new ProtoTypeReference
            {
                ClrType = type,
                ProtoTypeName = "map",
                IsMap = true,
                MapKeyType = keyType,
                MapValueType = valueType,
                MapKeyReference = keyReference,
                MapValueReference = valueReference,
                MapValueIsWrapped = valueNeedsWrapper
            };
        }
        // Array
        if (type is IArrayTypeSymbol array)
        {
            var element = Map(array.ElementType);

            return element with
            {
                IsRepeated = true
            };
        }

        // Collections
        if (type is INamedTypeSymbol named &&
            named.IsGenericType &&
            named.Name is "List"
                or "IList"
                or "ICollection"
                or "IEnumerable"
                or "IReadOnlyCollection"
                or "IReadOnlyList")
        {
            var element = Map(named.TypeArguments[0]);

            return element with
            {
                IsRepeated = true
            };
        }

        // Enum
        if (type.TypeKind == TypeKind.Enum)
        {
            return new ProtoTypeReference
            {
                ClrType = type,
                ProtoTypeName = type.Name,
                IsEnum = true
            };
        }

        

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return Primitive(type, "string");

            case SpecialType.System_Boolean:
                return Primitive(type, "bool");

            case SpecialType.System_Int32:
                return Primitive(type, "int32");

            case SpecialType.System_Int64:
                return Primitive(type, "int64");

            case SpecialType.System_Single:
                return Primitive(type, "float");

            case SpecialType.System_Double:
                return Primitive(type, "double");

            case SpecialType.System_Decimal:
                return Primitive(type, "string");
        }

        switch (type.Name)
        {
            case "Guid":
                return Primitive(type, "string");

            case "DateTime":
            case "DateTimeOffset":
                return WellKnown(type, "google.protobuf.Timestamp");

            case "DateOnly":
                return Primitive(type, "string");
        }

        return new ProtoTypeReference
        {
            ClrType = type,
            ProtoTypeName = type.Name,
            IsMessage = true,
            GenericArguments = type is INamedTypeSymbol generic
                ? generic.TypeArguments.Select(Map).ToList()
                : []
        };
    }

    private static ProtoTypeReference Primitive(
        ITypeSymbol clrType,
        string protoType)
    {
        return new ProtoTypeReference
        {
            ClrType = clrType,
            ProtoTypeName = protoType,
            IsPrimitive = true
        };
    }

    private static ProtoTypeReference WellKnown(
        ITypeSymbol clrType,
        string protoType)
    {
        return new ProtoTypeReference
        {
            ClrType = clrType,
            ProtoTypeName = protoType,
            IsWellKnownType = true
        };
    }

    private static string ToSnakeCase(string value)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsUpper(c) && i > 0)
                sb.Append('_');

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Turns a proto type name into a PascalCase identifier fragment used to name the
    /// generated nullable map-value wrapper (e.g. <c>string</c> -&gt; <c>String</c>,
    /// <c>int32</c> -&gt; <c>Int32</c>, <c>google.protobuf.Timestamp</c> -&gt; <c>Timestamp</c>),
    /// so the wrapper message becomes e.g. <c>NullableString</c> / <c>NullableInt32</c>.
    /// </summary>
    private static string SanitizeWrapperName(string protoTypeName)
    {
        var local = protoTypeName.Contains('.', StringComparison.Ordinal)
            ? protoTypeName[(protoTypeName.LastIndexOf('.') + 1)..]
            : protoTypeName;

        if (local.Length == 0)
            return "Value";

        return char.ToUpperInvariant(local[0]) + local[1..];
    }
}