using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestServiceImplementationTemplateModelFactory
{
    public static MediatorRequestServiceImplementationTemplateModel Create(
        string ns,
        string useCaseNamespace,
        string useCaseName,
        RequestType requestType,
        ResponseType responseType)
    {
        return new MediatorRequestServiceImplementationTemplateModel
        {
            Namespace = ns,

            UseCaseNamespace = useCaseNamespace,

            Usings =
            [
                "System.Threading",
                "System.Threading.Tasks",
                "System.Collections.Generic"
            ],


            ClassName =
                $"{useCaseName}Service",


            InterfaceName =
                $"I{useCaseName}Service",


            MethodName =
                ResolveMethodName(responseType),


            ReturnTypeName =
                ResolveReturnType(
                    useCaseName,
                    requestType,
                    responseType),


            Parameters =
                ResolveParameters(
                    useCaseName,
                    requestType,
                    responseType),


            DependencyName = "repository",

            DependencyTypeName =
                "IRepository",


            ExpressionBody =
                ResolveExpression(
                    useCaseName,
                    requestType,
                    responseType)
        };
    }


    private static string ResolveReturnType(
        string useCaseName,
        RequestType type,
        ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Single =>
                $"Task<{useCaseName}{type}Response?>",

            ResponseType.IEnumerable =>
                $"Task<IEnumerable<{useCaseName}{type}Response>>",

            ResponseType.KeyValuePair =>
                "Task<IEnumerable<SelectItem>>",

            ResponseType.PagedList =>
                $"Task<PagedList<{useCaseName}{type}Response>>",

            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }


    private static string ResolveMethodName(ResponseType responseType)
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


    private static IReadOnlyList<MethodParameterTemplateModel> ResolveParameters(
        string useCaseName,
        RequestType type,
        ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Single or
            ResponseType.IEnumerable or
            ResponseType.KeyValuePair =>
            [
                new()
                {
                    TypeName = "CancellationToken",
                    Name = "cancellationToken"
                }
            ],


            ResponseType.PagedList =>
            [
                new()
                {
                    TypeName = $"{useCaseName}{type}Filter",
                    Name = "filter"
                },
                new()
                {
                    TypeName = "PagedRequest",
                    Name = "pagedRequest"
                },
                new()
                {
                    TypeName = "CancellationToken",
                    Name = "cancellationToken"
                }
            ],


            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }


    private static string ResolveExpression(
        string useCaseName,
        RequestType type,
        ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Single =>
                $"repository.SingleOrDefaultAsync(new {useCaseName}{type}Specification(),cancellationToken)",


            ResponseType.IEnumerable =>
                $"repository.TolistAsync(new {useCaseName}{type}Specification(),cancellationToken)",


            ResponseType.KeyValuePair =>
                $"repository.GetSelectionItemAsync(new {useCaseName}{type}Specification(), cancellationToken)",


            ResponseType.PagedList =>
                $"repository.ToPagedListAsync(new {useCaseName}{type}Specification(pagedRequest),cancellationToken)",


            _ =>
                throw new ArgumentOutOfRangeException(nameof(responseType))
        };
    }
}