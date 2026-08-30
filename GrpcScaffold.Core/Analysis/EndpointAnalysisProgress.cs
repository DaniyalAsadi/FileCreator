namespace GrpcScaffold.Core.Analysis;

public sealed record EndpointAnalysisProgress(
    int CompletedSyntaxTrees,
    int TotalSyntaxTrees,
    string? FilePath = null);
