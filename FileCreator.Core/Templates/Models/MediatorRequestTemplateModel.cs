using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request.sbn" template needs
/// to render a mediator request class.
///
/// All naming decisions and type resolutions are handled by
/// <see cref="MediatorRequestTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- request --------------------------------------------------------------

    public required string UseCaseName { get; init; }

    public required RequestType RequestType { get; init; }


    // ---- class ----------------------------------------------------------------

    /// <summary>
    /// e.g. CreateUserCommand
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// e.g. ICommand&lt;CreateUserCommandResponse&gt;
    /// or ICommand
    /// </summary>
    public required string BaseTypeName { get; init; }


    // ---- response -------------------------------------------------------------

    public required bool HasResponse { get; init; }

    public ResponseType? ResponseType { get; init; }

    public required string ResultTypeName { get; init; }


    // ---- paged query ----------------------------------------------------------

    public required bool IsPagedQuery { get; init; }

    public required string FilterTypeName { get; init; }

    public required string PagedRequestTypeName { get; init; }

    public required IReadOnlyList<MediatorPropertyTemplateModel> Properties { get; init; } = [];
}
