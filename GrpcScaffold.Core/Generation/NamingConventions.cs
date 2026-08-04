// src/GrpcScaffold.Core/Generation/NamingConventions.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;

namespace GrpcScaffold.Core.Generation;

public static class NamingConventions
{
    public static string ToProtoPackage(string csharpNamespace) =>
        csharpNamespace.ToLowerInvariant().Replace('.', '_');

    public static string MappingClassName(string endpointClassName) =>
        (endpointClassName.EndsWith("Endpoint") ? endpointClassName[..^"Endpoint".Length] : endpointClassName)
        + "Mapping";


    public static string GetMessageName(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return type.Name;

        return GetMessageName(named);
    }

    private static string GetMessageName(INamedTypeSymbol type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var arguments = type.TypeArguments
            .Select(GetMessageName)
            .ToArray();

        return type.Name switch
        {
            // PagedList<UserDto> -> UserDtoPagedList
            "PagedList" when arguments.Length == 1 =>
                $"{arguments[0]}PagedList",


            // Result<UserDto> -> UserDtoResult
            "Result" when arguments.Length == 1 =>
                $"{arguments[0]}Result",

            // Response<UserDto> -> UserDtoResponse
            "CollectionResponse" when arguments.Length == 1 =>
                $"CollectionResponse",

            // Dictionary<TKey,TValue>
            "Dictionary" when arguments.Length == 2 =>
                $"{arguments[0]}{arguments[1]}Dictionary",

            // Generic fallback
            _ => $"{type.Name}Of{string.Join("And", arguments)}"
        };
    }

    public static string GetEnumName(ITypeSymbol type)
    {
        return type.Name;
    }

    public static string GetFieldTypeName(ProtoTypeReference reference)
    {
        return reference.ProtoTypeName;
    }

}