namespace FileCreator;

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
