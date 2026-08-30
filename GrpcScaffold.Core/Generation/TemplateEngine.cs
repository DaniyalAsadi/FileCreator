// src/GrpcScaffold.Core/Generation/TemplateEngine.cs
using System.Collections.Concurrent;
using System.Reflection;
using Scriban;

namespace GrpcScaffold.Core.Generation;

public sealed class TemplateEngine
{
    private readonly ConcurrentDictionary<string, Template> _cache = new();

    public string Render(string templateResourceName, object model)
    {
        var template = _cache.GetOrAdd(templateResourceName, LoadTemplate);
        return template.Render(model, member => member.Name); // preserve PascalCase-derived member access via ScriptObject below
    }

    private static Template LoadTemplate(string resourceName)
    {
        var assembly = typeof(TemplateEngine).Assembly;
        var resources = assembly.GetManifestResourceNames();
        var fullName = 
            resources.FirstOrDefault(n => Path.GetFileName(n) == $"GrpcScaffold.Core.Templates.{resourceName}");
        if (fullName is null)
            throw new InvalidOperationException(resourceName);

        using var stream = assembly.GetManifestResourceStream(fullName)!;
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var template = Template.Parse(text, resourceName);
        if (template.HasErrors)
            throw new InvalidOperationException(
                $"Template '{resourceName}' failed to parse:\n{string.Join('\n', template.Messages)}");
        return template;
    }
}
