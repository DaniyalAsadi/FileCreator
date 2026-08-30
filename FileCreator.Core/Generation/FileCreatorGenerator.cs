using FileCreator.Core;
using FileCreator.Core.Generation;
using FileCreator.Core.Generators;
using FileCreator.Core.Templates.Factories;
using System.IO;

namespace FileCreator.Core.Generation;

public sealed class FileCreatorGenerator(
    FileCreatorGenerationRequest request,
    ScribanFileCreator fileCreator)
{
    public string ProjectName => request.ProjectName;
    public GroupName GroupName => request.GroupName;
    public string UsecaseName => request.UseCaseName;
    public string UseCasePath => request.UseCasesPath;
    public string InfrastructurePath => request.InfrastructurePath;
    public string WebPath => request.WebPath;
    public string FunctionalTestPath => request.FunctionalTestsPath;
    public string UnitTestPath => request.UnitTestsPath;
    public bool HasRequest => request.HasRequest;
    public RequestType RequestType => request.RequestType;
    public bool HasResponse => request.HasResponse;
    public ResponseType ResponseType => request.ResponseType;
    public HttpVerb HttpVerb => request.HttpVerb;

    // ------------------------
    // Generate Preview (no File IO)
    // ------------------------
    public async Task<IReadOnlyList<GeneratedFile>> GeneratePreview()
    {
        var diagnostic = request.Validate().FirstOrDefault(item =>
            item.Severity == GenerationDiagnosticSeverity.Error);
        if (diagnostic is not null)
            throw new GenerationException(diagnostic);

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

        return files
            .OrderBy(file => file.AbsolutePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
