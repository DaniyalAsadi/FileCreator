namespace FileCreator.Core.Loading;

public sealed record LoadingProgress(
    int Percentage,
    string Stage,
    string? Detail = null);

public enum LoadingOperationState
{
    Idle,
    Loading,
    Loaded,
    Failed,
    Cancelled
}
