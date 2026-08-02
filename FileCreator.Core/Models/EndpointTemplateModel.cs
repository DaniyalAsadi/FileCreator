using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Models;

/// Everything the "endpoint.sbn" template needs to render a FastEndpoints class.
/// This is a plain data bag — every field is already fully resolved by
/// <see cref="EndpointTemplateModelFactory"/>. The template never branches on
/// anything that isn't a boolean flag or a pre-computed string; it never
/// concatenates type names or derives naming conventions itself.
/// </summary>
public sealed class EndpointTemplateModel
{
    // ---- identity / placement -------------------------------------------------
    public required string Namespace { get; init; }
    public required string UseCaseNamespace { get; init; }
    public required string ProjectName { get; init; }
    public required string Group { get; init; }
    public required string UseCaseName { get; init; }
    public required IReadOnlyList<string> Usings { get; init; } = [];

    // ---- raw inputs (kept for traceability / debugging) ------------------------
    public required RequestType RequestType { get; init; }
    public required HttpVerb HttpVerb { get; init; }
    public required bool HasRequest { get; init; }
    public required bool HasResponse { get; init; }
    public ResponseType? ResponseType { get; init; }

    // ---- derived values consumed directly by the template ----------------------

    /// <summary>e.g. "CreateUserEndpoint"</summary>
    public required string ClassName { get; init; }

    /// <summary>"Endpoint&lt;CreateUserRequest&gt;" or "EndpointWithoutRequest"</summary>
    public required string BaseTypeName { get; init; }

    /// <summary>"CreateUserRequest" or "EmptyRequest" when there is no request</summary>
    public required string RequestTypeName { get; init; }

    /// <summary>"CreateUserResponse" / "IEnumerable&lt;T&gt;" / "PagedList&lt;T&gt;" / "EmptyResponse"</summary>
    public required string ResponseModelTypeName { get; init; }

    /// <summary>"MapToCommand" / "MapToQuery"</summary>
    public required string MapMethodName { get; init; }

    /// <summary>local variable name for the mediator message: "command" / "query"</summary>
    public required string RequestVariableName { get; init; }
}