namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "mediator-request-handler.sbn" template needs
/// to render a mediator request handler class.
///
/// All names and decisions are already resolved by
/// <see cref="MediatorRequestHandlerTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class MediatorRequestHandlerTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- handler --------------------------------------------------------------

    /// <summary>
    /// e.g. GetUserQueryHandler
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// e.g. IQueryHandler&lt;GetUserQuery, UserResponse&gt;
    /// </summary>
    public required string BaseTypeName { get; init; }


    // ---- request --------------------------------------------------------------

    public required string RequestTypeName { get; init; }

    public required RequestType RequestType { get; init; }


    // ---- response -------------------------------------------------------------

    public required bool HasResponse { get; init; }

    public ResponseType? ResponseType { get; init; }

    public required string ResultTypeName { get; init; }


    // ---- constructor ----------------------------------------------------------

    public required string DependencyName { get; init; }

    public required string DependencyTypeName { get; init; }


    // ---- method ---------------------------------------------------------------

    public required string ServiceMethodName { get; init; }

    public required string ReturnTypeName { get; init; }

    public required bool IsQuery { get; init; }

    public required bool IsCommand { get; init; }

    public required bool HasExpressionBody { get; init; }

    public required string MethodBody { get; init; }
}