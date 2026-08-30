using System.IO;
using FileCreator.Core.Loading;

namespace FileCreator;

public interface IWorkspaceCache
{
    bool IsLoaded { get; }

    PreviewWorkspace Current { get; }

    Task<PreviewWorkspace> LoadAsync(
        string solutionPath,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class WorkspaceCacheService : IWorkspaceCache, IDisposable
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly object _gate = new();
    private PreviewWorkspace? _workspace;
    private string? _solutionPath;
    private bool _disposed;

    public bool IsLoaded
    {
        get
        {
            lock (_gate)
                return _workspace is not null;
        }
    }

    public PreviewWorkspace Current
    {
        get
        {
            lock (_gate)
                return _workspace
                    ?? throw new InvalidOperationException("The solution workspace has not been loaded.");
        }
    }

    public async Task<PreviewWorkspace> LoadAsync(
        string solutionPath,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        var fullPath = Path.GetFullPath(solutionPath);
        await _loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            lock (_gate)
            {
                if (_workspace is not null &&
                    string.Equals(_solutionPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report(new LoadingProgress(100, "Completed", Path.GetFileName(fullPath)));
                    return _workspace;
                }
            }

            var loaded = await PreviewWorkspace.LoadAsync(
                    fullPath,
                    progress,
                    cancellationToken)
                .ConfigureAwait(false);

            PreviewWorkspace? previous;
            lock (_gate)
            {
                previous = _workspace;
                _workspace = loaded;
                _solutionPath = fullPath;
            }

            previous?.Dispose();
            return loaded;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_gate)
        {
            _workspace?.Dispose();
            _workspace = null;
            _solutionPath = null;
        }

        _loadGate.Dispose();
    }
}
