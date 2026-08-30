namespace FileCreator.Core.Generation;

/// <summary>Normalized input and output configuration for one FileCreator run.</summary>
public sealed record FileCreatorGenerationRequest(
    string ProjectName,
    GroupName GroupName,
    string UseCaseName,
    string UseCasesPath,
    string WebPath,
    string FunctionalTestsPath,
    string UnitTestsPath,
    string InfrastructurePath,
    bool HasRequest,
    RequestType RequestType,
    bool HasResponse,
    ResponseType ResponseType,
    HttpVerb HttpVerb)
{
    public IReadOnlyList<GenerationDiagnostic> Validate()
    {
        var diagnostics = new List<GenerationDiagnostic>();

        Required(ProjectName, nameof(ProjectName));
        Required(UseCaseName, nameof(UseCaseName));
        Required(UseCasesPath, nameof(UseCasesPath));
        Required(WebPath, nameof(WebPath));
        Required(FunctionalTestsPath, nameof(FunctionalTestsPath));
        Required(UnitTestsPath, nameof(UnitTestsPath));
        Required(InfrastructurePath, nameof(InfrastructurePath));

        if (RequestType == RequestType.Command &&
            ResponseType is ResponseType.IEnumerable or ResponseType.PagedList)
        {
            diagnostics.Add(Error(
                "FC1101",
                "A command cannot return a collection response.",
                nameof(ResponseType)));
        }

        if (RequestType == RequestType.Query && !HasResponse)
        {
            diagnostics.Add(Error(
                "FC1102",
                "A query must have a response.",
                nameof(HasResponse)));
        }

        return diagnostics;

        void Required(string value, string source)
        {
            if (string.IsNullOrWhiteSpace(value))
                diagnostics.Add(Error("FC1100", $"{source} is required.", source));
        }

        static GenerationDiagnostic Error(string code, string message, string source) =>
            new(GenerationDiagnosticSeverity.Error, code, message, source);
    }
}
