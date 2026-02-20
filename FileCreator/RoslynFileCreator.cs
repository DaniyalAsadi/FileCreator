using FileCreator.Core;
using FileCreator.Core.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator;

public class RoslynFileCreator(
    string SolutionName,
    GroupName GroupName,
    string UsecaseName,
    string UseCasePath,
    string WebPath,
    string FunctionalTestPath,
    string UnitTestPath,
    bool HasRequest,
    RequestType RequestType,
    bool HasResponse,
    ResponseType ResponseType,
    HttpVerb HttpVerb)
{
    
    public void Generate()
    {

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


        string usecaseFolderNameSpace = $"{SolutionName}.UseCases.{GroupName.Feature}.{(RequestType == RequestType.Command ? "Commands" : "Queries")}.{UsecaseName}";
        string webFolderNameSpace = $"{SolutionName}.Web.EndPoints.{GroupName.Resource}.{UsecaseName}";
        string functionalTestFolderNameSpace = $"{SolutionName}.FunctionalTests.ApiEndpoints.{GroupName.Resource}";
        string unitTestsFolderNameSpace = $"{SolutionName}.UnitTests.UseCases.{GroupName.Feature}";
        // ------------------------ MediatorRequestGenerator ------------------------ 
        WriteIfNotExists(Path.Combine(
                useCasePath,
                $"{UsecaseName}{RequestType}.cs"),
            MediatorRequestGenerator.Generate(
                usecaseFolderNameSpace,
                UsecaseName,
                RequestType,
                HasResponse,
                ResponseType));
        // ------------------------ MediatorRequestHandlerGenerator ------------------------ 
        WriteIfNotExists(Path.Combine(
                useCasePath,
                $"{UsecaseName}{RequestType}Handler.cs"),
            MediatorRequestHandlerGenerator.Generate(
                usecaseFolderNameSpace,
                UsecaseName,
                RequestType,
                HasResponse,
                ResponseType));

        if (HasResponse)
        {
            if (ResponseType == ResponseType.PagedList)
            {
                // ------------------------ MediatorRequestFiltersGenerator ------------------------ 
                WriteIfNotExists(Path.Combine(
                    useCasePath,
                $"{UsecaseName}{RequestType}Filter.cs"),
                MediatorRequestFiltersGenerator.Generate(
                    usecaseFolderNameSpace,
                    UsecaseName,
                    RequestType));
            }

            // ------------------------ MediatorRequestResponseGenerator ------------------------ 
            WriteIfNotExists(Path.Combine(
                useCasePath,
                $"{UsecaseName}{RequestType}Response.cs"),
                MediatorRequestResponseGenerator.Generate(
                    usecaseFolderNameSpace,
                    UsecaseName,
                    RequestType));
        }
        if (RequestType == RequestType.Query)
        {
            WriteIfNotExists(Path.Combine(useCasePath,
                $"{UsecaseName}{RequestType}Specification"),
                MediatorRequestSpecificationGenerator.Generate(
                usecaseFolderNameSpace,
                    UsecaseName,
                    RequestType));


            WriteIfNotExists(Path.Combine(useCasePath,
                $"I{UsecaseName}Service.cs"),
                MediatorRequestServiceGenerator.Generate(
                    usecaseFolderNameSpace,
                    UsecaseName,
                    RequestType,
                    ResponseType));
        }


            // ------------------------ EndpointGenerator ------------------------ 
            WriteIfNotExists(Path.Combine(endpointPath,
            $"{UsecaseName}.cs"),
                EndpointGenerator.Generate(
                webFolderNameSpace,
                usecaseFolderNameSpace,
                GroupName.Resource,
                UsecaseName,
                RequestType,
                HttpVerb,
                HasRequest,
                HasResponse,
                ResponseType));
        if (HasRequest)
        {
            // ------------------------ EndpointRequestGenerator ------------------------ 
            WriteIfNotExists(
                Path.Combine(endpointPath,
                $"{UsecaseName}Request.cs"),
                EndpointRequestGenerator.Generate(
                    webFolderNameSpace,
                    usecaseFolderNameSpace,
                    UsecaseName,
                    RequestType,
                    HasResponse,
                    ResponseType));
            // ------------------------ EndpointRequestValidatorGenerator ------------------------ 

            WriteIfNotExists(
                Path.Combine(webFolderNameSpace,
                $"{UsecaseName}Validator.cs"),
                EndpointRequestValidatorGenerator.Generate(webFolderNameSpace, UsecaseName));
        }
        // ------------------------ EndpointTestGenerator ------------------------ 
        WriteIfNotExists(
            Path.Combine(functionalPath,
            $"{UsecaseName}Tests.cs"),
            EndpointTestGenerator.Generate(
                functionalTestFolderNameSpace,
                webFolderNameSpace,
                GroupName,
                UsecaseName,
                HasRequest,
                RequestType,
                HasResponse,
                ResponseType,
                HttpVerb));
        // ------------------------ MediatorRequestHandlerTestGenerator ------------------------ 
        WriteIfNotExists(
            Path.Combine(unitTestPath,
            $"{UsecaseName}{RequestType}HandlerTests.cs"),
            MediatorRequestHandlerTestGenerator.Generate(
                unitTestsFolderNameSpace,
                usecaseFolderNameSpace,
                UsecaseName,
                RequestType,
                HasResponse,
                ResponseType));

    }




    private static void WriteIfNotExists(string path, CompilationUnitSyntax content)
    {
        var pathDirectory = Path.GetDirectoryName(path);
        if (!Directory.Exists(pathDirectory))
        {
            Directory.CreateDirectory(pathDirectory!);
        }
        if (!File.Exists(path))
            File.WriteAllText(path, content.NormalizeWhitespace().ToFullString());
    }
}
