using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-service-implementation.sbn" template needs
/// to render a service implementation class.
///
/// All naming decisions and generated expressions are resolved by
/// <see cref="MediatorRequestServiceImplementationTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestServiceImplementationTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required string UseCaseNamespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- class ----------------------------------------------------------------

    /// <summary>
    /// e.g. UserService
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// e.g. IUserService
    /// </summary>
    public required string InterfaceName { get; init; }


    // ---- method ----------------------------------------------------------------

    public required string MethodName { get; init; }

    public required string ReturnTypeName { get; init; }


    public required IReadOnlyList<MethodParameterTemplateModel> Parameters { get; init; } = [];


    // ---- constructor ----------------------------------------------------------

    public required string DependencyName { get; init; }

    public required string DependencyTypeName { get; init; }


    // ---- implementation -------------------------------------------------------

    /// <summary>
    /// Expression body after =>
    /// </summary>
    public required string ExpressionBody { get; init; }
}
