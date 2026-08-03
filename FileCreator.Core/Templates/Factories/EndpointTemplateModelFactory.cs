using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

/// <summary>
/// Single source of truth for how raw scaffold inputs (use-case name, request/response
/// flags, HTTP verb, response shape) turn into the concrete type names that appear in
/// generated code. Nothing here touches Scriban or Roslyn — it is pure string logic,
/// fully unit-testable in isolation from rendering.
/// </summary>
public static class EndpointTemplateModelFactory
{
    public static EndpointTemplateModel Create(
        string projectName,
        string useCaseNamespace,
        string webNamespace,
        string group,
        string useCaseName,
        RequestType requestType,
        HttpVerb httpVerb,
        bool hasRequest,
        bool hasResponse,
        ResponseType? responseType,
        IReadOnlyList<string>? extraUsings = null)
    {
        var requestTypeName = hasRequest ? $"{useCaseName}Request" : "EmptyRequest";

        var baseTypeName = hasRequest
            ? $"Endpoint<{requestTypeName}>"
            : "EndpointWithoutRequest";

        var responseModelTypeName = !hasResponse
            ? "EmptyResponse"
            : responseType switch
            {
                Core.ResponseType.Single => $"{useCaseName}{requestType}Response",
                Core.ResponseType.IEnumerable => $"IEnumerable<{useCaseName}{requestType}Response>",
                Core.ResponseType.KeyValuePair => "IEnumerable<SelectItem>",
                Core.ResponseType.PagedList => $"PagedList<{useCaseName}{requestType}Response>",
                null => throw new ArgumentException(
                    "responseType is required when hasResponse is true.", nameof(responseType)),
                _ => throw new ArgumentOutOfRangeException(nameof(responseType))
            };

        var usings = new List<string> { "SharedKernel" };
        if (extraUsings is not null)
            usings.AddRange(extraUsings);

        return new EndpointTemplateModel
        {
            Namespace = webNamespace,
            UseCaseNamespace = useCaseNamespace,
            ProjectName = projectName,
            Group = group,
            UseCaseName = useCaseName,
            Usings = usings.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList(),

            RequestType = requestType,
            HttpVerb = httpVerb,
            HasRequest = hasRequest,
            HasResponse = hasResponse,
            ResponseType = responseType,

            ClassName = $"{useCaseName}Endpoint",
            BaseTypeName = baseTypeName,
            RequestTypeName = requestTypeName,
            ResponseModelTypeName = responseModelTypeName,
            MapMethodName = $"MapTo{requestType}",
            RequestVariableName = requestType.ToString().ToLowerInvariant()
        };
    }
}