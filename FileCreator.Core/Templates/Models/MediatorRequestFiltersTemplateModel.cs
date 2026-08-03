namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-filter.sbn" template needs
/// to render a mediator request filter class.
///
/// All naming decisions are resolved by
/// <see cref="MediatorRequestFiltersTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestFiltersTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- class ----------------------------------------------------------------

    /// <summary>
    /// e.g. GetUsersQueryFilter
    /// </summary>
    public required string ClassName { get; init; }


    public required string UseCaseName { get; init; }


    public required RequestType RequestType { get; init; }
}