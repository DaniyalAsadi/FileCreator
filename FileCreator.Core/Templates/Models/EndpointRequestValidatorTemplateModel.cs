using FileCreator.Core.Templates.Factories;

namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "endpoint-request-validator.sbn" template needs
/// to render a FluentValidation validator class.
///
/// All names are already resolved by
/// <see cref="EndpointRequestValidatorTemplateModelFactory"/>.
/// The template only renders source code.
/// </summary>
public sealed class EndpointRequestValidatorTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];


    // ---- validator information -----------------------------------------------

    /// <summary>
    /// e.g. CreateUserValidator
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// e.g. Validator&lt;CreateUserRequest&gt;
    /// </summary>
    public required string BaseTypeName { get; init; }


    /// <summary>
    /// e.g. CreateUserRequest
    /// </summary>
    public required string RequestTypeName { get; init; }


    /// <summary>
    /// Placeholder rules generated inside constructor.
    /// </summary>
    public required IReadOnlyList<string> Rules { get; init; } = [];
}