namespace FileCreator.Core.Generation;

/// <summary>
/// A generated artifact before it is persisted. Generators own content and a relative
/// destination; the output layer owns all file-system side effects.
/// </summary>
public sealed record GeneratedFile
{
    public GeneratedFile(string basePath, string relativePath, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(relativePath))
            throw new ArgumentException("Generated file paths must be relative.", nameof(relativePath));

        BasePath = Path.GetFullPath(basePath);
        RelativePath = relativePath;
        Content = content;

        var absolutePath = Path.GetFullPath(Path.Combine(BasePath, RelativePath));
        var relativeToRoot = Path.GetRelativePath(BasePath, absolutePath);
        if (relativeToRoot == ".." ||
            relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativeToRoot))
        {
            throw new ArgumentException("Generated file paths cannot escape the output root.", nameof(relativePath));
        }

        AbsolutePath = absolutePath;
    }

    public string BasePath { get; }

    public string RelativePath { get; }

    public string Content { get; }

    public string AbsolutePath { get; }
}
