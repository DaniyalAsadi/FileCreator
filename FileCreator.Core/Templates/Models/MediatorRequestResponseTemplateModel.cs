namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-response.sbn" template needs
/// to render a mediator request response class.
///
/// All naming decisions are resolved by
/// <see cref="MediatorRequestResponseTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestResponseTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- response -------------------------------------------------------------

    /// <summary>
    /// e.g. CreateUserCommandResponse
    /// </summary>
    public required string ClassName { get; init; }


    public required string UseCaseName { get; init; }


    public required RequestType RequestType { get; init; }
}