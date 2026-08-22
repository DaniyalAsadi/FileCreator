using FileCreator.Core;
using FileCreator.Core.Generators;
using FileCreator.Core.Generators.V1;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Xunit;

public class EndpointGeneratorTests
{
    // -----------------------------
    // Happy Path (Request + Response)
    // -----------------------------
    [Fact]
    public void Generate_Should_Create_Endpoint_With_Request_And_Response()
    {
        var unit = EndpointGenerator.Generate(
            ns: "MyApp.Features.Region",
            useCaseNameSpace: "MyApp.Features.Region.Get",
            projectName:"AuthorizationManager",
            group: "Region",
            useCaseName: "GetRegion",
            type: RequestType.Query,
            httpVerb: HttpVerb.GET,
            hasRequest: true,
            hasResponse: true,
            responseType: ResponseType.IEnumerable);

        var classNode = unit.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        classNode.Identifier.Text.Should().Be("GetRegionEndpoint");

        classNode.BaseList!.Types.Single().ToString()
            .Should().Be("Endpoint<GetRegionRequest>");

        classNode.ParameterList!.Parameters.Should().ContainSingle();
        classNode.ParameterList.Parameters[0].Type!.ToString()
            .Should().Be("IMediator");

        var method = classNode.Members.OfType<MethodDeclarationSyntax>()
            .Single(x => x.Identifier.Text == "ExecuteAsync");

        method.ReturnType.ToString().Should().Be("Task<IResult>");
        method.Modifiers.Any(m => m.Text == "async").Should().BeTrue();
    }

    // -----------------------------
    // Without Request Mode
    // -----------------------------
    [Fact]
    public void Generate_Should_Create_EndpointWithoutRequest_When_No_Request()
    {
        var unit = EndpointGenerator.Generate(
            ns: "Test",
            useCaseNameSpace: "Test",
            projectName: "AuthorizationManager",
            group: "Region",
            useCaseName: "Ping",
            type: RequestType.Command,
            httpVerb: HttpVerb.POST,
            hasRequest: false,
            hasResponse: false,
            responseType: ResponseType.Single);

        var classNode = unit.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        classNode.BaseList!.Types.Single().ToString()
            .Should().Be("EndpointWithoutRequest");

        var execute = classNode.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(x => x.Identifier.Text == "ExecuteAsync");

        execute.ParameterList.Parameters.Should().ContainSingle(); // فقط CancellationToken
    }

    // -----------------------------
    // Request Mapping Generation
    // -----------------------------
    [Fact]
    public void Generate_Should_Map_Request_To_Command()
    {
        var unit = EndpointGenerator.Generate(
            "Test",
            "Test",
            "AuthorizationManager",
            "Region",
            "CreateRegion",
            RequestType.Command,
            HttpVerb.POST,
            hasRequest: true,
            hasResponse: false,
            responseType: ResponseType.Single);

        var method = unit.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(x => x.Identifier.Text == "ExecuteAsync");

        var bodyText = method.Body!.ToString();

        bodyText.Should().Contain("CreateRegionRequest.MapToCommand(req)");
    }

    // -----------------------------
    // Mediator Dispatch Validation
    // -----------------------------
    [Fact]
    public void Generate_Should_Call_Mediator_Send()
    {
        var unit = EndpointGenerator.Generate(
            "Test",
            "Test",
            "AuthorizationManager",
            "Region",
            "DeleteRegion",
            RequestType.Command,
            HttpVerb.DELETE,
            hasRequest: false,
            hasResponse: false,
            responseType: ResponseType.Single);

        var text = unit.NormalizeWhitespace().ToFullString();

        text.Should().Contain("await mediator.Send");
    }

    // -----------------------------
    // Configure Metadata Validation
    // -----------------------------
    [Fact]
    public void Generate_Should_Add_Configure_Metadata()
    {
        var unit = EndpointGenerator.Generate(
            ns: "Test",
            useCaseNameSpace: "Test",
            projectName: "AuthorizationManager",
            group: "Region",
            useCaseName: "GetRegion",
            type: RequestType.Query,
            httpVerb: HttpVerb.GET,
            hasRequest: true,
            hasResponse: true,
            responseType: ResponseType.Single);

        var configure = unit.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(x => x.Identifier.Text == "Configure");

        var body = configure.Body!.ToString();

        body.Should().Contain("Specify(ApiRoutes.AuthorizationManager.Region.GetRegion)");
        body.Should().Contain("Summary");
    }

    // -----------------------------
    // Result Mapping Validation
    // -----------------------------
    [Fact]
    public void Generate_Should_Return_MinimalApi_Result()
    {
        var unit = EndpointGenerator.Generate(
            "Test",
            "Test",
            "AuthorizationManager",
            "Region",
            "ListRegion",
            RequestType.Query,
            HttpVerb.GET,
            true,
            true,
            ResponseType.IEnumerable);

        var method = unit.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(x => x.Identifier.Text == "ExecuteAsync");

        method.Body!.ToString().Should().Contain("return result.ToMinimalApiResult();");
    }
}