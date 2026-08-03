using FileCreator.Core;

namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "endpoint-test.sbn" template needs to render
/// a FastEndpoints integration test class.
///
/// All naming rules and decisions are already resolved by
/// <see cref="EndpointTestTemplateModelFactory"/>.
/// The template only renders source code.
/// </summary>
public sealed class EndpointTestTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];

    public required string ProjectName { get; init; }

    public required string UseCaseNamespace { get; init; }

    public required string WebNamespace { get; init; }


    // ---- class ----------------------------------------------------------------

    /// <summary>
    /// e.g. CreateUserTests
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// ApiTestBase
    /// </summary>
    public required string BaseTypeName { get; init; }


    /// <summary>
    /// CustomWebApplicationFactory&lt;Program&gt;
    /// </summary>
    public required string ConstructorParameterTypeName { get; init; }


    // ---- endpoint information -------------------------------------------------

    public required string GroupName { get; init; }

    public required string UseCaseName { get; init; }

    public required string RouteExpression { get; init; }


    // ---- request --------------------------------------------------------------

    public required bool HasRequest { get; init; }

    public required string RequestTypeName { get; init; }


    // ---- response -------------------------------------------------------------

    public required bool HasResponse { get; init; }

    public ResponseType? ResponseType { get; init; }

    public required string ResponseTypeName { get; init; }


    // ---- http -----------------------------------------------------------------

    public required HttpVerb HttpVerb { get; init; }

    public required string ClientMethodName { get; init; }

    public required string TestMethodName { get; init; }


    // ---- client call ----------------------------------------------------------

    public required IReadOnlyList<string> GenericArguments { get; init; } = [];

    public required bool AssignResponseVariable { get; init; }

    public required bool ValidateResponse { get; init; }
}