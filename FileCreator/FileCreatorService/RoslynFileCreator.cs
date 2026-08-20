using FileCreator.Core;
using FileCreator.Core.DependencyInjection;
using FileCreator.Core.Generators;
using FileCreator.Core.Templates.Factories;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;

namespace FileCreator.FileCreatorService;

public sealed class RoslynFileCreator(
    string projectName,
    GroupName groupName,
    string usecaseName,
    string useCasePath,
    string webPath,
    string functionalTestPath,
    string unitTestPath,
    string infrastructurePath,
    bool hasRequest,
    RequestType requestType,
    bool hasResponse,
    ResponseType responseType,
    HttpVerb httpVerb)
{
    public string ProjectName { get; } = projectName;
    public GroupName GroupName { get; } = groupName;
    public string UsecaseName { get; } = usecaseName;
    public string UseCasePath { get; } = useCasePath;
    public string InfrastructurePath { get; } = infrastructurePath;
    public string WebPath { get; } = webPath;
    public string FunctionalTestPath { get; } = functionalTestPath;
    public string UnitTestPath { get; } = unitTestPath;
    public bool HasRequest { get; } = hasRequest;
    public RequestType RequestType { get; } = requestType;
    public bool HasResponse { get; } = hasResponse;
    public ResponseType ResponseType { get; } = responseType;
    public HttpVerb HttpVerb { get; } = httpVerb;

    // ------------------------
    // Generate Preview (no File IO)
    // ------------------------
    public async Task<IReadOnlyList<GeneratedFile>> GeneratePreview()
    {
        var files = new List<GeneratedFile>();

        string useCasePath = Path.Combine(
            UseCasePath,
            GroupName.Feature,
            RequestType == RequestType.Command ? "Commands" : "Queries",
            UsecaseName);

        string endpointPath = Path.Combine(
            WebPath,
            "EndPoints",
            GroupName.Resource,
            UsecaseName);

        string unitTestPath = Path.Combine(
            UnitTestPath,
            "UseCases",
            GroupName.Feature);

        string functionalPath = Path.Combine(
            FunctionalTestPath,
            "ApiEndpoints",
            GroupName.Resource);

        string infrastructureService = Path.Combine(
            InfrastructurePath,
            "Data",
            "Queries",
            GroupName.Feature);

        string usecaseNamespace = $"{ProjectName}.UseCases.{GroupName.Feature}.{(RequestType == RequestType.Command ? "Commands" : "Queries")}.{UsecaseName}";
        string webNamespace = $"{ProjectName}.Web.EndPoints.{GroupName.Resource}.{UsecaseName}";
        string functionalNamespace = $"{ProjectName}.FunctionalTests.ApiEndpoints.{GroupName.Resource}";
        string unitTestNamespace = $"{ProjectName}.UnitTests.UseCases.{GroupName.Feature}";
        string infrastructureNamespace = $"{ProjectName}.Infrastructure.Data.Queries.{GroupName.Feature}";

        var services = new ServiceCollection().AddScribanCodeGeneration().BuildServiceProvider();
        var fileCreator = services.GetRequiredService<ScribanFileCreator>();

        //// ------------------------ MediatorRequest ------------------------
        #region Request
        files.Add(new GeneratedFile(
            useCasePath,
            $"{UsecaseName}{RequestType}.cs",
            await fileCreator.GenerateAsync(MediatorRequestTemplateModelFactory.Create(
            ns: usecaseNamespace,
            useCaseName: UsecaseName,
            requestType: RequestType,
            hasResponse: HasResponse,
            responseType: ResponseType))));
        #endregion


        #region Request Handler
        files.Add(new GeneratedFile(
            useCasePath,
            $"{UsecaseName}{RequestType}Handler.cs",
            await fileCreator.GenerateAsync(MediatorRequestHandlerTemplateModelFactory.Create(
                ns: usecaseNamespace,
                groupName: GroupName,
                useCaseName: UsecaseName,
                requestType: RequestType,
                hasResponse: HasResponse,
                responseType: ResponseType))));
        #endregion

        if (HasResponse)
        {
            #region Request Filter
            if (ResponseType == ResponseType.PagedList)
            {
                files.Add(new GeneratedFile(
                    useCasePath,
                    $"{UsecaseName}{RequestType}Filter.cs",
                    await fileCreator.GenerateAsync(MediatorRequestFiltersTemplateModelFactory.Create(
                        ns: usecaseNamespace,
                        useCaseName: UsecaseName,
                        requestType: RequestType))));
            }
            #endregion

            if (ResponseType is not ResponseType.KeyValuePair)
            {
                #region Request Response
                files.Add(new GeneratedFile(
                    useCasePath,
                    $"{UsecaseName}{RequestType}Response.cs",
                   await fileCreator.GenerateAsync(MediatorRequestResponseTemplateModelFactory.Create(
                       ns: usecaseNamespace,
                       useCaseName: UsecaseName,
                       requestType: RequestType
                       ))));
                #endregion
            }
        }

        if (RequestType == RequestType.Query)
        {
            #region Request Specification
            files.Add(new GeneratedFile(
                useCasePath,
                $"{UsecaseName}{RequestType}Specification.cs",
                await fileCreator.GenerateAsync(MediatorRequestSpecificationTemplateModelFactory.Create(
                    ns: usecaseNamespace,
                    useCaseName: UsecaseName,
                    requestType: RequestType,
                    responseType: ResponseType))));
            #endregion
            #region Request Service
            files.Add(new GeneratedFile(
                useCasePath,
                $"I{UsecaseName}Service.cs",
                await fileCreator.GenerateAsync(MediatorRequestServiceTemplateModelFactory.Create(
                    ns: usecaseNamespace,
                    useCaseName: UsecaseName,
                    requestType: RequestType,
                    responseType: ResponseType))));
            #endregion
            #region Request Service Implementation
            files.Add(new GeneratedFile(
               infrastructureService,
               $"{UsecaseName}Service.cs",
                await fileCreator.GenerateAsync(MediatorRequestServiceImplementationTemplateModelFactory.Create(
                    ns: infrastructureNamespace,
                    useCaseNamespace: usecaseNamespace,
                    useCaseName: UsecaseName,
                    requestType: RequestType,
                    responseType: ResponseType))));
            #endregion
        }

        //// ------------------------ Endpoint ------------------------
        #region EndPoint
        files.Add(
            new GeneratedFile(
            endpointPath,
            $"{UsecaseName}.cs",
            await fileCreator.GenerateAsync(EndpointTemplateModelFactory.Create(
            projectName: ProjectName,
            useCaseNamespace: usecaseNamespace,
            webNamespace: webNamespace,
            group: GroupName.Feature,
            useCaseName: UsecaseName,
            requestType: RequestType,
            httpVerb: HttpVerb,
            hasRequest: HasRequest,
            hasResponse: HasResponse,
            responseType: ResponseType))));
        #endregion

        if (HasRequest)
        {
            #region Endpoint Request
            files.Add(new GeneratedFile(
                endpointPath,
                $"{UsecaseName}Request.cs",
                await fileCreator.GenerateAsync(EndpointRequestTemplateModelFactory.Create(
                    ns: webNamespace,
                    useCaseNameSpace: usecaseNamespace,
                    useCaseName: UsecaseName,
                    requestType: RequestType,
                    hasResponse: HasResponse,
                    responseType: ResponseType))
            ));
            #endregion
            #region Endpoint Request Validator
            files.Add(new GeneratedFile(
                endpointPath,
                $"{UsecaseName}Validator.cs",
                await fileCreator.GenerateAsync(EndpointRequestValidatorTemplateModelFactory.Create(
                    webNamespace,
                    UsecaseName))
            ));
            #endregion
        }

        // ------------------------ Tests ------------------------
        files.Add(new GeneratedFile(
            functionalPath,
            $"{UsecaseName}Tests.cs",
            await fileCreator.GenerateAsync(EndpointTestTemplateModelFactory.Create(
                ns: functionalNamespace,
                projectName: ProjectName,
                useCaseNamespace: usecaseNamespace,
                webNamespace: webNamespace,
                groupName: GroupName,
                useCaseName: UsecaseName,
                hasRequest: HasRequest,
                requestType: RequestType,
                hasResponse: HasResponse,
                responseType: ResponseType,
                httpVerb: HttpVerb))
        ));

        files.Add(new GeneratedFile(
            unitTestPath,
            $"{UsecaseName}{RequestType}HandlerTests.cs",
            await fileCreator.GenerateAsync(MediatorRequestHandlerTestTemplateModelFactory.Create(
                ns: unitTestNamespace,
                groupName: GroupName,
                useCaseNamespace: usecaseNamespace,
                useCaseName: UsecaseName,
                type: RequestType,
                hasResponse: HasResponse,
                responseType: ResponseType))
        ));

        return files;
    }

    // ------------------------
    // Optional: Write to disk
    // ------------------------
    public static void WriteFiles(IEnumerable<GeneratedFile> files)
    {
        foreach (var file in files)
        {
            var dir = Path.GetDirectoryName(file.AbsolutePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(file.AbsolutePath))
                File.WriteAllText(file.AbsolutePath, file.Content, Encoding.UTF8);
        }
    }
}
