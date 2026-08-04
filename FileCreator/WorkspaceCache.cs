namespace FileCreator;

public interface IWorkspaceCache
{
    PreviewWorkspace GetWorkspace();
}
public sealed class WorkspaceCacheService : IWorkspaceCache
{
    public PreviewWorkspace GetWorkspace()
    {
        var path = Properties.Settings.Default.SolutionPath;

        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Solution path is not configured.");

        if (!File.Exists(path))
            throw new FileNotFoundException(path);

        return WorkspaceCache.GetWorkspace(path);
    }
}


public static class WorkspaceCache
{
    private static PreviewWorkspace? _workspace;

    public static PreviewWorkspace GetWorkspace(string slnPath)
    {
        _workspace ??= new PreviewWorkspace(slnPath);

        return _workspace;
    }

    public static void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
    }
}
