using FileCreator.Core.Loading;

namespace FileCreator.Test;

public sealed class LoadingOperationTests
{
    [Fact]
    public async Task Successful_loading_publishes_result_and_reaches_100_percent()
    {
        var operation = new LoadingOperation<object>();
        var expected = new object();
        var observed = new List<LoadingProgress>();

        var result = await operation.RunAsync(
            (progress, _) =>
            {
                progress.Report(new LoadingProgress(10, "Locating project"));
                progress.Report(new LoadingProgress(55, "Reading source files"));
                progress.Report(new LoadingProgress(95, "Building scaffold model"));
                return Task.FromResult(expected);
            },
            new ImmediateProgress<LoadingProgress>(observed.Add));

        Assert.Same(expected, result);
        Assert.Same(expected, operation.Result);
        Assert.Equal(LoadingOperationState.Loaded, operation.State);
        Assert.Equal(0, observed[0].Percentage);
        Assert.Equal(100, observed[^1].Percentage);
        AssertProgressIsValid(observed);
    }

    [Fact]
    public async Task Failure_terminates_loading_without_publishing_a_result()
    {
        var operation = new LoadingOperation<string>();
        var expected = new InvalidOperationException("Invalid project");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            operation.RunAsync((_, _) => Task.FromException<string>(expected)));

        Assert.Same(expected, actual);
        Assert.Null(operation.Result);
        Assert.Same(expected, operation.Error);
        Assert.Equal(LoadingOperationState.Failed, operation.State);
        Assert.Equal("Failed", operation.Progress.Stage);
    }

    [Fact]
    public async Task Cancellation_terminates_loading_without_publishing_a_result()
    {
        var operation = new LoadingOperation<string>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operation.RunAsync(
                (_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult("incomplete");
                },
                cancellationToken: cancellation.Token));

        Assert.Null(operation.Result);
        Assert.Null(operation.Error);
        Assert.Equal(LoadingOperationState.Cancelled, operation.State);
        Assert.Equal("Cancelled", operation.Progress.Stage);
    }

    [Fact]
    public async Task Progress_is_clamped_and_never_moves_backwards()
    {
        var operation = new LoadingOperation<string>();
        var observed = new List<LoadingProgress>();

        await operation.RunAsync(
            (progress, _) =>
            {
                progress.Report(new LoadingProgress(-25, "Before start"));
                progress.Report(new LoadingProgress(70, "Analyzing"));
                progress.Report(new LoadingProgress(20, "Late diagnostic"));
                progress.Report(new LoadingProgress(150, "Finished"));
                return Task.FromResult("complete");
            },
            new ImmediateProgress<LoadingProgress>(observed.Add));

        AssertProgressIsValid(observed);
        Assert.Contains(observed, value => value.Stage == "Late diagnostic" && value.Percentage == 70);
        Assert.Equal(100, observed[^1].Percentage);
    }

    private static void AssertProgressIsValid(IReadOnlyList<LoadingProgress> observed)
    {
        Assert.NotEmpty(observed);

        for (var index = 0; index < observed.Count; index++)
        {
            Assert.InRange(observed[index].Percentage, 0, 100);
            if (index > 0)
                Assert.True(observed[index].Percentage >= observed[index - 1].Percentage);
        }
    }

    private sealed class ImmediateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
