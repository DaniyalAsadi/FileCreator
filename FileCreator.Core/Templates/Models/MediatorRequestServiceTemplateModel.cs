using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-service.sbn" template needs
/// to render a use case service interface.
///
/// All names and signatures are resolved by
/// <see cref="MediatorRequestServiceTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestServiceTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- interface ------------------------------------------------------------

    /// <summary>
    /// e.g. IUserService
    /// </summary>
    public required string InterfaceName { get; init; }


    // ---- method ----------------------------------------------------------------

    public required string MethodName { get; init; }

    public required string ReturnTypeName { get; init; }


    public required IReadOnlyList<MethodParameterTemplateModel> Parameters { get; init; } = [];
}
