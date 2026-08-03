using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestHandlerTemplateModelFactory
{
    public static MediatorRequestHandlerTemplateModel Create(
        string ns,
        GroupName groupName,
        string useCaseName,
        RequestType requestType,
        bool hasResponse,
        ResponseType responseType)
    {
        var resultTypeName =
            ResolveResultType(
                useCaseName,
                requestType,
                hasResponse,
                responseType);


        var isQuery = requestType == RequestType.Query;


        var serviceMethodName =
            isQuery
                ? ResolveQueryMethod(responseType)
                : string.Empty;


        var dependencyName =
            isQuery
                ? "service"
                : "repository";


        var dependencyTypeName =
            isQuery
                ? $"I{useCaseName}Service"
                : $"I{groupName.Feature.TrimStart("The")}Repository";


        return new MediatorRequestHandlerTemplateModel
        {
            Namespace = ns,

            Usings =
            [
                "System",
                "SharedKernel"
            ],


            ClassName =
                $"{useCaseName}{requestType}Handler",


            BaseTypeName =
                ResolveBaseType(
                    useCaseName,
                    requestType,
                    hasResponse,
                    resultTypeName),


            RequestTypeName =
                $"{useCaseName}{requestType}",


            RequestType = requestType,


            HasResponse = hasResponse,

            ResponseType = responseType,

            ResultTypeName = resultTypeName,


            DependencyName = dependencyName,

            DependencyTypeName = dependencyTypeName,


            ServiceMethodName = serviceMethodName,


            ReturnTypeName =
                hasResponse
                    ? $"ValueTask<Result<{resultTypeName}>>"
                    : "ValueTask<Result>",


            IsQuery = isQuery,

            IsCommand = !isQuery,


            HasExpressionBody = isQuery,


            MethodBody =
                isQuery
                    ? string.Empty
                    : "throw new NotImplementedException();"
        };
    }


    private static string ResolveResultType(
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        if (!hasResponse)
            return string.Empty;


        return responseType switch
        {
            ResponseType.Single =>
                $"{useCaseName}{type}Response",

            ResponseType.IEnumerable =>
                $"IEnumerable<{useCaseName}{type}Response>",

            ResponseType.KeyValuePair =>
                "IEnumerable<SelectItem>",

            ResponseType.PagedList =>
                $"PagedList<{useCaseName}{type}Response>",

            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }


    private static string ResolveBaseType(
        string useCaseName,
        RequestType type,
        bool hasResponse,
        string resultType)
    {
        if (hasResponse)
        {
            return
                $"I{type}Handler<{useCaseName}{type}, {resultType}>";
        }

        return
            $"I{type}Handler<{useCaseName}{type}>";
    }


    private static string ResolveQueryMethod(
        ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Single =>
                "GetAsync",

            ResponseType.IEnumerable =>
                "ListAsync",

            ResponseType.KeyValuePair =>
                "ListAsync",

            ResponseType.PagedList =>
                "ListAsync",

            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }
}