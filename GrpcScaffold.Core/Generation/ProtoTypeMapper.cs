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

            // Dictionary<string, object> -> google.protobuf.Struct
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

            return new ProtoTypeReference
            {
                ClrType = type,
                ProtoTypeName = "map",
                IsMap = true,
                MapKeyType = keyType,
                MapValueType = valueType
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
}