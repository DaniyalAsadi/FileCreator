using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestHandlerTestTemplateModelFactory
{
    public static MediatorRequestHandlerTestTemplateModel Create(
        string ns,
        GroupName groupName,
        string useCaseNamespace,
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        var isQuery = type == RequestType.Query;


        var dependencyName =
            isQuery
                ? "_serviceMock"
                : "_repositoryMock";


        var dependencyType =
            isQuery
                ? $"Mock<I{useCaseName}Service>"
                : $"Mock<I{groupName.Feature.TrimStart("The")}Repository>";


        var dependencyInitialization =
            isQuery
                ? $"_serviceMock = new Mock<I{useCaseName}Service>();"
                : $"_repositoryMock = new Mock<I{groupName.Feature.TrimStart("The")}Repository>();";


        var handlerInitialization =
            isQuery
                ? "_handler = new(_serviceMock.Object);"
                : "_handler = new(_repositoryMock.Object);";


        return new MediatorRequestHandlerTestTemplateModel
        {
            Namespace = ns,

            UseCaseNamespace = useCaseNamespace,


            Usings =
            [
                "System.Threading",
                "System.Threading.Tasks",
                "Xunit",
                "Moq"
            ],


            ClassName =
                $"{useCaseName}{type}HandlerTests",


            HandlerTypeName =
                $"{useCaseName}{type}Handler",


            MockFieldTypeName = dependencyType,

            MockFieldName = dependencyName,


            MockInitialization =
                dependencyInitialization,


            HandlerFieldName =
                "_handler",


            HandlerInitialization =
                handlerInitialization,


            TestMethodName =
                BuildMethodName(
                    useCaseName,
                    type,
                    hasResponse,
                    responseType),


            RequestTypeName =
                $"{useCaseName}{type}",


            ResultVariableName =
                "result",


            HasResponse = hasResponse,

            ResponseType = responseType
        };
    }


    private static string BuildMethodName(
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        var responsePart =
            hasResponse
                ? responseType switch
                {
                    ResponseType.Single => "Single",
                    ResponseType.IEnumerable => "List",
                    ResponseType.KeyValuePair => "KeyValuePair",
                    ResponseType.PagedList => "PagedList",
                    _ => throw new NotSupportedException()
                }
                : "NoResponse";


        return
            $"{useCaseName}{type}Handle_Should_Return_Success_{responsePart}";
    }
}