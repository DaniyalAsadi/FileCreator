using FileCreator.Core;
using FileCreator.Core.Templates.Models;
using Humanizer;

namespace FileCreator.Core.Templates.Factories;

public static class EndpointTestTemplateModelFactory
{
    public static EndpointTestTemplateModel Create(
        string ns,
        string projectName,
        string useCaseNamespace,
        string webNamespace,
        GroupName groupName,
        string useCaseName,
        bool hasRequest,
        RequestType requestType,
        bool hasResponse,
        ResponseType responseType,
        HttpVerb httpVerb)
    {
        var clientMethodName = ResolveClientMethod(
            httpVerb,
            hasResponse,
            responseType);


        var responseTypeName = hasResponse
            ? $"{useCaseName}{requestType}Response"
            : "HttpResponseMessage";


        var genericArguments = new List<string>();

        if (requestType == RequestType.Command &&
            hasRequest &&
            hasResponse)
        {
            genericArguments.Add(
                $"{useCaseName}Request");
        }

        if (hasResponse)
        {
            genericArguments.Add(responseTypeName);
        }


        return new EndpointTestTemplateModel
        {
            Namespace = ns,

            Usings =
            [
                useCaseNamespace,
                webNamespace
            ],

            ProjectName = projectName,

            UseCaseNamespace = useCaseNamespace,

            WebNamespace = webNamespace,


            ClassName = $"{useCaseName}Tests",

            BaseTypeName = "ApiTestBase",

            ConstructorParameterTypeName =
                "CustomWebApplicationFactory<Program>",


            GroupName = groupName.Resource.ToString(),

            UseCaseName = useCaseName,


            RouteExpression =
                $"ApiRoutePaths.{projectName}.{groupName.Resource}.{useCaseName}",


            HasRequest = hasRequest,

            RequestTypeName =
                $"{useCaseName}Request",


            HasResponse = hasResponse,

            ResponseType = responseType,

            ResponseTypeName = responseTypeName,


            HttpVerb = httpVerb,

            ClientMethodName = clientMethodName,


            TestMethodName =
                BuildTestMethodName(
                    groupName,
                    useCaseName,
                    httpVerb,
                    hasResponse,
                    responseType),


            GenericArguments = genericArguments,


            AssignResponseVariable = hasResponse,

            ValidateResponse = hasResponse
        };
    }


    private static string ResolveClientMethod(
        HttpVerb verb,
        bool hasResponse,
        ResponseType responseType)
    {
        return verb switch
        {
            HttpVerb.GET when !hasResponse =>
                "GetStatusAsync",

            HttpVerb.GET =>
                responseType switch
                {
                    ResponseType.Single =>
                        "GetSingleAsync",

                    ResponseType.IEnumerable =>
                        "GetEnumerableAsync",

                    ResponseType.PagedList =>
                        "GetPagedListAsync",

                    ResponseType.KeyValuePair =>
                        "GetEnumerableAsync",

                    _ =>
                        throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
                },

            HttpVerb.DELETE =>
                "DeleteAsync",

            HttpVerb.POST =>
                "PostBodyAsync",

            HttpVerb.PUT =>
                "PutBodyAsync",

            HttpVerb.PATCH =>
                "PatchBodyAsync",

            _ =>
                "SendAsync"
        };
    }


    private static string BuildTestMethodName(
        GroupName group,
        string useCase,
        HttpVerb verb,
        bool hasResponse,
        ResponseType responseType)
    {
        var result = hasResponse
            ? responseType.ToString()
            : "NoContent";

        return
            $"{verb.ToString().Pascalize()}_{group.Resource}_{useCase}_Should_Return_{result}";
    }
}
