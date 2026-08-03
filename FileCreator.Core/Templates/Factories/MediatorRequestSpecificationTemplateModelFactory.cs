using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestSpecificationTemplateModelFactory
{
    public static MediatorRequestSpecificationTemplateModel Create(
        string ns,
        string useCaseName,
        RequestType requestType,
        ResponseType responseType)
    {
        var isPaged =
            responseType == ResponseType.PagedList;


        return new MediatorRequestSpecificationTemplateModel
        {
            Namespace = ns,


            Usings =
            [
                "Ardalis.Specification"
            ],


            ClassName =
                $"{useCaseName}{requestType}Specification",


            BaseTypeName =
                ResolveBaseType(
                    useCaseName,
                    requestType,
                    responseType),


            UseCaseName = useCaseName,

            RequestType = requestType,

            ResponseType = responseType,


            HasPagedRequestParameter = isPaged,


            ConstructorParameterTypeName =
                "PagedRequest",


            ConstructorParameterName =
                "pagedRequest",


            BaseConstructorArgument =
                "pagedRequest",


            ConstructorStatement =
                requestType == RequestType.Query
                    ? "Query.AsNoTracking();"
                    : "Query;"
        };
    }


    private static string ResolveBaseType(
        string useCaseName,
        RequestType type,
        ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Single =>
                $"SingleResultSpecification<T,{useCaseName}{type}Response>",


            ResponseType.IEnumerable =>
                $"Specification<T,{useCaseName}{type}Response>",


            ResponseType.PagedList =>
                $"PagedListResultSpecification<T,{useCaseName}{type}Response>",


            ResponseType.KeyValuePair =>
                "KeyValuePairResultSpecification<T>",


            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }
}