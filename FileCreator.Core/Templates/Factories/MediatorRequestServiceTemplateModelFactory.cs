using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestServiceTemplateModelFactory
{
    public static MediatorRequestServiceTemplateModel Create(
        string ns,
        string useCaseName,
        RequestType requestType,
        ResponseType responseType)
    {
        return new MediatorRequestServiceTemplateModel
        {
            Namespace = ns,

            Usings =
            [
                "System.Threading",
                "System.Threading.Tasks",
                "System.Collections.Generic"
            ],


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

            ResponseType.PagedList =>
                $"Task<PagedList<{useCaseName}{type}Response>>",

            ResponseType.KeyValuePair =>
                "Task<IEnumerable<SelectItem>>",

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

            ResponseType.PagedList =>
                "ListAsync",

            ResponseType.KeyValuePair =>
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
            ResponseType.Single or ResponseType.IEnumerable =>
            [
                new()
                {
                    TypeName = $"{useCaseName}{type}",
                    Name = "request"
                },
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


            ResponseType.KeyValuePair =>
            [
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
}