namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-handler-test.sbn" template needs
/// to render a mediator request handler unit test.
///
/// All naming and implementation decisions are resolved by
/// <see cref="MediatorRequestHandlerTestTemplateModelFactory"/>.
/// </summary>
public sealed class MediatorRequestHandlerTestTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required string UseCaseNamespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- class ----------------------------------------------------------------

    public required string ClassName { get; init; }


    public required string HandlerTypeName { get; init; }


    // ---- mock -----------------------------------------------------------------

    public required string MockFieldTypeName { get; init; }

    public required string MockFieldName { get; init; }


    public required string MockInitialization { get; init; }


    // ---- handler --------------------------------------------------------------

    public required string HandlerFieldName { get; init; }

    public required string HandlerInitialization { get; init; }


    // ---- test method ----------------------------------------------------------

    public required string TestMethodName { get; init; }

    public required string RequestTypeName { get; init; }


    public required string ResultVariableName { get; init; }


    public required bool HasResponse { get; init; }

    public required ResponseType? ResponseType { get; init; }
}