// FileCreator.Core/Templating/EmbeddedResourceTemplateSource.cs
using System.Reflection;

namespace FileCreator.Core.Templating;

/// <summary>
/// Loads templates embedded in the assembly (ItemGroup EmbeddedResource Include="Templates\*.sbn"
/// in the .csproj, same as GrpcScaffold.Core does). No file-system dependency at runtime,
/// so the generator ships as a single self-contained assembly / dotnet tool.
/// </summary>
public sealed class EmbeddedResourceTemplateSource(Assembly? assembly = null) : IScribanTemplateSource
{
    private readonly Assembly _assembly = assembly ?? typeof(EmbeddedResourceTemplateSource).Assembly;

    public string GetTemplateText(string templateName)
    {
        var resourceNames = _assembly.GetManifestResourceNames();
        var fullName = resourceNames.SingleOrDefault(
            n => n.EndsWith(templateName, StringComparison.OrdinalIgnoreCase));

        if (fullName is null)
        {
            throw new InvalidOperationException(
                $"Template '{templateName}' was not found as an embedded resource in " +
                $"'{_assembly.GetName().Name}'. Available: {string.Join(", ", resourceNames)}");
        }

        using var stream = _assembly.GetManifestResourceStream(fullName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

/// <summary>
/// Reads templates straight off disk. Useful in dev/design-time (e.g. a "watch templates"
/// mode inside the FileCreator WinForms tool) where you don't want a rebuild per .sbn edit.
/// </summary>
public sealed class FileSystemTemplateSource(string templatesRoot) : IScribanTemplateSource
{
    public string GetTemplateText(string templateName)
    {
        var path = Path.Combine(templatesRoot, templateName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Template '{templateName}' not found under '{templatesRoot}'.", path);

        return File.ReadAllText(path);
    }
}