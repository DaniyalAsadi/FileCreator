using FileCreator.Core.Templates.Factories;
using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Models;

/// <summary>
/// Everything the "endpoint-request.sbn" template needs to render
/// a FastEndpoints request DTO.
///
/// All names and decisions are already resolved by
/// <see cref="EndpointRequestTemplateModelFactory"/>.
/// The template only renders.
/// </summary>
public sealed class EndpointRequestTemplateModel : IGeneratorModel
{
    // ---- identity -------------------------------------------------------------

    public required string Namespace { get; init; }

    public required string UseCaseNamespace { get; init; }

    public required IReadOnlyList<string> Usings { get; init; } = [];

    public required string UseCaseName { get; init; }


    // ---- request information --------------------------------------------------

    public required RequestType RequestType { get; init; }

    public required bool HasResponse { get; init; }

    public ResponseType? ResponseType { get; init; }


    // ---- resolved names -------------------------------------------------------

    /// <summary>
    /// e.g. CreateUserRequest
    /// </summary>
    public required string ClassName { get; init; }


    /// <summary>
    /// e.g. CreateUserCommand / CreateUserQuery
    /// </summary>
    public required string TargetRequestTypeName { get; init; }


    /// <summary>
    /// e.g. MapToCommand / MapToQuery
    /// </summary>
    public required string MapMethodName { get; init; }


    /// <summary>
    /// Determines whether paging properties should be generated.
    /// </summary>
    public required bool HasPaging { get; init; }


    /// <summary>
    /// e.g. CreateUserQueryFilter
    /// </summary>
    public required string FilterTypeName { get; init; }


    /// <summary>
    /// e.g. PageIndex / PageSize properties
    /// </summary>
    public required IReadOnlyList<PropertyTemplateModel> Properties { get; init; } = [];


    // ---- mapping --------------------------------------------------------------

    public required bool IsPagedQuery { get; init; }

    public required string MappingParameterName { get; init; }

    public required string MappingParameterTypeName { get; init; }


    public required string RequestParameterName { get; init; }

    public required string MapExpression { get; init; }
}
