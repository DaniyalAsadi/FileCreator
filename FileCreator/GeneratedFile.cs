using System.IO;

namespace FileCreator;

public sealed record GeneratedFile(string BasePath , string RelativePath, string Content)
{
    public string AbsolutePath => Path.Combine(BasePath, RelativePath);
}