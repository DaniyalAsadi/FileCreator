using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestTemplateModelFactory
{
    public static MediatorRequestTemplateModel Create(
        string ns,
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


        var baseTypeName =
            ResolveBaseType(
                requestType,
                hasResponse,
                resultTypeName);


        var isPagedQuery =
            requestType == RequestType.Query &&
            responseType == ResponseType.PagedList;


        return new MediatorRequestTemplateModel
        {
            Namespace = ns,

            Usings =
            [
                "SharedKernel"
            ],


            UseCaseName = useCaseName,

            RequestType = requestType,


            ClassName = $"{useCaseName}{requestType}",


            BaseTypeName = baseTypeName,


            HasResponse = hasResponse,

            ResponseType = responseType,

            ResultTypeName = resultTypeName,


            IsPagedQuery = isPagedQuery,


            FilterTypeName =
                $"{useCaseName}{requestType}Filter",


            PagedRequestTypeName =
                "PagedRequest",


            Properties = isPagedQuery
                ?
                [
                    new()
                    {
                        TypeName = $"{useCaseName}{requestType}Filter",
                        Name = "Filter",
                        InitializerName = "filter"
                    },
                    new()
                    {
                        TypeName = "PagedRequest",
                        Name = "PagedRequest",
                        InitializerName = "pagedRequest"
                    }
                ]
                :
                []
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

            ResponseType.PagedList =>
                $"PagedList<{useCaseName}{type}Response>",

            ResponseType.KeyValuePair =>
                "IEnumerable<SelectItem>",

            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }


    private static string ResolveBaseType(
        RequestType type,
        bool hasResponse,
        string resultTypeName)
    {
        if (hasResponse)
        {
            return type switch
            {
                RequestType.Command =>
                    $"ICommand<{resultTypeName}>",

                RequestType.Query =>
                    $"IQuery<{resultTypeName}>",

                _ =>
                    throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }


        return type switch
        {
            RequestType.Command =>
                "ICommand",

            RequestType.Query =>
                "IQuery",

            _ =>
                throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
