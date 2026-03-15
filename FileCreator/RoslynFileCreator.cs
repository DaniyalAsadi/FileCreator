using FileCreator.Core;
using FileCreator.Core.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator;

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
    public IReadOnlyList<GeneratedFile> GeneratePreview()
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

        // ------------------------ MediatorRequest ------------------------
        files.Add(new GeneratedFile(
            Path.Combine(useCasePath, $"{UsecaseName}{RequestType}.cs"),
            MediatorRequestGenerator.Generate(usecaseNamespace, UsecaseName, RequestType, HasResponse, ResponseType).NormalizeWhitespace().ToFullString()
        ));

        files.Add(new GeneratedFile(
            Path.Combine(useCasePath, $"{UsecaseName}{RequestType}Handler.cs"),
            MediatorRequestHandlerGenerator.Generate(usecaseNamespace, UsecaseName, RequestType, HasResponse, ResponseType).NormalizeWhitespace().ToFullString()
        ));

        if (HasResponse)
        {
            if (ResponseType == ResponseType.PagedList)
            {
                files.Add(new GeneratedFile(
                    Path.Combine(useCasePath, $"{UsecaseName}{RequestType}Filter.cs"),
                    MediatorRequestFiltersGenerator.Generate(usecaseNamespace, UsecaseName, RequestType).NormalizeWhitespace().ToFullString()
                ));
            }
            if (ResponseType is not ResponseType.KeyValuePair)
            {

                files.Add(new GeneratedFile(
                    Path.Combine(useCasePath, $"{UsecaseName}{RequestType}Response.cs"),
                    MediatorRequestResponseGenerator.Generate(usecaseNamespace, UsecaseName, RequestType).NormalizeWhitespace().ToFullString()
                ));
            }
        }

        if (RequestType == RequestType.Query)
        {
            files.Add(new GeneratedFile(
                Path.Combine(useCasePath, $"{UsecaseName}{RequestType}Specification.cs"),
                MediatorRequestSpecificationGenerator.Generate(usecaseNamespace, UsecaseName, RequestType, ResponseType).NormalizeWhitespace().ToFullString()
            ));

            files.Add(new GeneratedFile(
                Path.Combine(useCasePath, $"I{UsecaseName}Service.cs"),
                MediatorRequestServiceGenerator.Generate(usecaseNamespace, UsecaseName, RequestType, ResponseType).NormalizeWhitespace().ToFullString()
            ));

            files.Add(new GeneratedFile(
                Path.Combine(infrastructureService, $"{UsecaseName}Service.cs"),
                MediatorRequestServiceImplementationGenerator.Generate(infrastructureNamespace, usecaseNamespace, UsecaseName, RequestType, ResponseType).NormalizeWhitespace().ToFullString()
            ));
        }

        // ------------------------ Endpoint ------------------------
        files.Add(new GeneratedFile(
            Path.Combine(endpointPath, $"{UsecaseName}.cs"),
            EndpointGenerator.Generate(webNamespace, usecaseNamespace, GroupName.Resource, UsecaseName, RequestType, HttpVerb, HasRequest, HasResponse, ResponseType).NormalizeWhitespace().ToFullString()
        ));

        if (HasRequest)
        {
            files.Add(new GeneratedFile(
                Path.Combine(endpointPath, $"{UsecaseName}Request.cs"),
                EndpointRequestGenerator.Generate(webNamespace, usecaseNamespace, UsecaseName, RequestType, HasResponse, ResponseType).NormalizeWhitespace().ToFullString()
            ));

            files.Add(new GeneratedFile(
                Path.Combine(endpointPath, $"{UsecaseName}Validator.cs"),
                EndpointRequestValidatorGenerator.Generate(webNamespace, UsecaseName).NormalizeWhitespace().ToFullString()
            ));
        }

        // ------------------------ Tests ------------------------
        files.Add(new GeneratedFile(
            Path.Combine(functionalPath, $"{UsecaseName}Tests.cs"),
            EndpointTestGenerator.Generate(functionalNamespace, webNamespace, GroupName, UsecaseName, HasRequest, RequestType, HasResponse, ResponseType, HttpVerb).NormalizeWhitespace().ToFullString()
        ));

        files.Add(new GeneratedFile(
            Path.Combine(unitTestPath, $"{UsecaseName}{RequestType}HandlerTests.cs"),
            MediatorRequestHandlerTestGenerator.Generate(unitTestNamespace, usecaseNamespace, UsecaseName, RequestType, HasResponse, ResponseType).NormalizeWhitespace().ToFullString()
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
            var dir = Path.GetDirectoryName(file.Path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(file.Path))
                File.WriteAllText(file.Path, file.Content, Encoding.UTF8);
        }
    }
}
