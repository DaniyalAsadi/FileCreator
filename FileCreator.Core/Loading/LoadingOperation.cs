namespace FileCreator.Core.Loading;

/// <summary>
/// Owns the lifecycle of one asynchronous loading operation and publishes its
/// result only after the operation completes successfully.
/// </summary>
public sealed class LoadingOperation<TResult>
{
    private readonly object _gate = new();

    public LoadingOperationState State { get; private set; } = LoadingOperationState.Idle;

    public LoadingProgress Progress { get; private set; } = new(0, "Idle");

    public TResult? Result { get; private set; }

    public Exception? Error { get; private set; }

    public async Task<TResult> RunAsync(
        Func<IProgress<LoadingProgress>, CancellationToken, Task<TResult>> operation,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_gate)
        {
            if (State == LoadingOperationState.Loading)
                throw new InvalidOperationException("A loading operation is already running.");

            State = LoadingOperationState.Loading;
            Progress = new LoadingProgress(0, "Initializing");
            Result = default;
            Error = null;
        }

        var reporter = new MonotonicProgressReporter(UpdateProgress, progress);
        reporter.Report(Progress);

        try
        {
            var result = await operation(reporter, cancellationToken).ConfigureAwait(false);
            reporter.Complete(new LoadingProgress(100, "Completed"));

            lock (_gate)
            {
                Result = result;
                State = LoadingOperationState.Loaded;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            reporter.Stop();
            lock (_gate)
            {
                State = LoadingOperationState.Cancelled;
                Progress = Progress with { Stage = "Cancelled" };
            }

            throw;
        }
        catch (Exception ex)
        {
            reporter.Stop();
            lock (_gate)
            {
                Error = ex;
                State = LoadingOperationState.Failed;
                Progress = Progress with { Stage = "Failed", Detail = ex.Message };
            }

            throw;
        }
    }

    private void UpdateProgress(LoadingProgress value)
    {
        lock (_gate)
        {
            Progress = value;
        }
    }

    private sealed class MonotonicProgressReporter(
        Action<LoadingProgress> update,
        IProgress<LoadingProgress>? forward) : IProgress<LoadingProgress>
    {
        private readonly object _gate = new();
        private int _percentage;
        private bool _stopped;

        public void Report(LoadingProgress value)
        {
            ArgumentNullException.ThrowIfNull(value);

            lock (_gate)
            {
                if (_stopped)
                    return;

                ReportCore(value);
            }
        }

        public void Complete(LoadingProgress value)
        {
            ArgumentNullException.ThrowIfNull(value);

            lock (_gate)
            {
                if (_stopped)
                    return;

                ReportCore(value);
                _stopped = true;
            }
        }

        public void Stop()
        {
            lock (_gate)
                _stopped = true;
        }

        private void ReportCore(LoadingProgress value)
        {
            var percentage = Math.Clamp(value.Percentage, _percentage, 100);
            _percentage = percentage;
            var normalized = value with { Percentage = percentage };
            update(normalized);
            forward?.Report(normalized);
        }
    }
}
