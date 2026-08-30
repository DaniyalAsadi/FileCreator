namespace FileCreator.Core.Generation;

public enum GenerationDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>Structured, actionable feedback produced by the FileCreator pipeline.</summary>
public sealed record GenerationDiagnostic(
    GenerationDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Source = null,
    string? Location = null,
    string? SuggestedFix = null);

public sealed class GenerationException(GenerationDiagnostic diagnostic)
    : InvalidOperationException($"{diagnostic.Code}: {diagnostic.Message}")
{
    public GenerationDiagnostic Diagnostic { get; } = diagnostic;
}
