namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-specification.sbn" template needs
/// to render a specification class.
///
/// All naming and conditional decisions are resolved by
/// <see cref="MediatorRequestSpecificationTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestSpecificationTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- specification --------------------------------------------------------

    /// <summary>
    /// e.g. GetUserQuerySpecification
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// e.g. Specification&lt;T, UserResponse&gt;
    /// </summary>
    public required string BaseTypeName { get; init; }


    public required string UseCaseName { get; init; }

    public required RequestType RequestType { get; init; }

    public required ResponseType ResponseType { get; init; }


    // ---- constructor ----------------------------------------------------------

    public required bool HasPagedRequestParameter { get; init; }

    public required string ConstructorParameterTypeName { get; init; }

    public required string ConstructorParameterName { get; init; }

    public required string BaseConstructorArgument { get; init; }


    // ---- body -----------------------------------------------------------------

    /// <summary>
    /// Constructor body statement.
    /// </summary>
    public required string ConstructorStatement { get; init; }
}