using FileCreator.Core;
using FileCreator.Core.Generators;
using FileCreator.Core.Generators.V1;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class EndpointRequestGeneratorTests
{
    // ------------------------------------------------------------
    // Should create Request class with IRequestEndpoints
    // ------------------------------------------------------------
    [Fact]
    public void Generate_Should_Create_Request_Class_With_Interface()
    {
        var unit = EndpointRequestGenerator.Generate(
            ns: "Test",
            useCaseNameSpace: "Test.Region",
            useCaseName: "GetRegion",
            type: RequestType.Query,
            hasResponse: false,
            responseType: ResponseType.Single);

        var classNode = unit.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        classNode.Identifier.Text.Should().Be("GetRegionRequest");

        classNode.BaseList!.Types.Single().ToString()
            .Should().Be("IRequestEndpoints");
    }

    // ------------------------------------------------------------
    // Paging properties must exist ONLY for paged query
    // ------------------------------------------------------------
    [Fact]
    public void Generate_Should_Add_Paging_Properties_For_Paged_Query()
    {
        var unit = EndpointRequestGenerator.Generate(
            "Test",
            "Test.Region",
            "ListRegion",
            RequestType.Query,
            hasResponse: true,
            responseType: ResponseType.PagedList);

        var props = unit.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

        props.Should().Contain(p => p.Identifier.Text == "PageIndex");
        props.Should().Contain(p => p.Identifier.Text == "PageSize");

        props.All(p => p.Modifiers.Any(m => m.Text == "required"))
            .Should().BeTrue();
    }

    // ------------------------------------------------------------
    // Paging properties must NOT exist in other modes
    // ------------------------------------------------------------
    [Fact]
    public void Generate_Should_Not_Add_Paging_Outside_Paged_Query()
    {
        var unit = EndpointRequestGenerator.Generate(
            "Test",
            "Test.Region",
            "GetRegion",
            RequestType.Query,
            hasResponse: true,
            responseType: ResponseType.Single);

        var props = unit.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

        props.Should().BeEmpty();
    }

    // ------------------------------------------------------------
    // Special Mapping Shape (Paged Query)
    // ------------------------------------------------------------
    [Fact]
    public void Generate_Should_Create_Paged_Query_Map_Method()
    {
        var unit = EndpointRequestGenerator.Generate(
            "Test",
            "Test.Region",
            "ListRegion",
            RequestType.Query,
            hasResponse: true,
            responseType: ResponseType.PagedList);

        var method = unit.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        method.Identifier.Text.Should().Be("MapToQuery");

        // must accept ListRequest
        method.ParameterList.Parameters.Single().Type!.ToString()
            .Should().Be("ListRequest");

        var body = method.ExpressionBody!.Expression.ToString();

        body.Should().Contain("new ListRegionFilter()");
        body.Should().Contain("new PagedRequest");
        body.Should().Contain("request.PageIndex");
        body.Should().Contain("request.PageSize");
    }

    // ------------------------------------------------------------
    // Default Mapping Shape
    // ------------------------------------------------------------
    [Fact]
    public void Generate_Should_Create_Default_Map_Method()
    {
        var unit = EndpointRequestGenerator.Generate(
            "Test",
            "Test.Region",
            "CreateRegion",
            RequestType.Command,
            hasResponse: false,
            responseType: ResponseType.Single);

        var method = unit.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        method.Identifier.Text.Should().Be("MapToCommand");

        // must accept Request DTO
        method.ParameterList.Parameters.Single().Type!.ToString()
            .Should().Be("CreateRegionRequest");

        var expr = method.ExpressionBody!.Expression.ToString();

        expr.Should().Be("new CreateRegionCommand()");
    }

    // ------------------------------------------------------------
    // Method must be internal static
    // ------------------------------------------------------------
    [Fact]
    public void Generate_Map_Method_Should_Be_Internal_Static()
    {
        var unit = EndpointRequestGenerator.Generate(
            "Test",
            "Test.Region",
            "DeleteRegion",
            RequestType.Command,
            hasResponse: false,
            responseType: ResponseType.Single);

        var method = unit.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        method.Modifiers.Any(m => m.Text == "internal").Should().BeTrue();
        method.Modifiers.Any(m => m.Text == "static").Should().BeTrue();
    }

    [Fact]
    public void Generate_PagedQuery_Should_Match_Snapshot()
    {
        // Arrange (Happy Path)
        var unit = EndpointRequestGenerator.Generate(
            ns: "MyApp.Features.Region",
            useCaseNameSpace: "MyApp.Features.Region.List",
            useCaseName: "ListRegion",
            type: RequestType.Query,
            hasResponse: true,
            responseType: ResponseType.PagedList);

        // Act
        var generated = unit
            .NormalizeWhitespace()
            .ToFullString();

        // Snapshot (Golden Master)
        var snapshot =
@"namespace MyApp.Features.Region;

using MyApp.Features.Region.List;
using ECommerce.SharedKernel;

public sealed class ListRegionRequest : IRequestEndpoints
{
    public required int PageIndex { get; set; }
    public required int PageSize { get; set; }

    internal static ListRegionQuery MapToQuery(ListRequest request)
        => new ListRegionQuery(new ListRegionFilter()
        {
        }, new PagedRequest(request.PageIndex, request.PageSize));
}";
        var snapShotTree = CSharpSyntaxTree.ParseText(snapshot);
        var root = snapShotTree.GetRoot();
        var snapShotGenerated = root.NormalizeWhitespace().ToFullString();

        // Assert
        generated.Should().Be(snapShotGenerated);
    }
}