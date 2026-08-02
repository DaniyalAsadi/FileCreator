// FileCreator.Core/Templating/ScribanTemplateRenderer.cs
using System.Collections.Concurrent;
using Scriban;
using Scriban.Runtime;

namespace FileCreator.Core.Templating;

/// <summary>
/// Production renderer: parses each template once (cached for the process lifetime),
/// exposes every public property of the model to the template under its
/// snake_case name (so <c>UseCaseName</c> in C# becomes <c>use_case_name</c> in .sbn —
/// exactly Scriban's own convention), and fails loudly on template errors instead
/// of silently emitting broken code.
/// </summary>
public sealed class ScribanTemplateRenderer(IScribanTemplateSource templateSource) : IScribanTemplateRenderer
{
    private readonly ConcurrentDictionary<string, Template> _cache = new();

    public Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken ct = default)
        where TModel : notnull
    {
        ct.ThrowIfCancellationRequested();

        var template = _cache.GetOrAdd(templateName, LoadTemplate);

        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: StandardMemberRenamer.Rename);

        var context = new TemplateContext
        {
            MemberRenamer = StandardMemberRenamer.Rename,
            LoopLimit = 100_000,
            StrictVariables = false
        };
        context.PushGlobal(scriptObject);

        var rendered = template.Render(context);
        return Task.FromResult(rendered);
    }

    private Template LoadTemplate(string templateName)
    {
        var text = templateSource.GetTemplateText(templateName);
        var template = Template.Parse(text, templateName);

        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Template '{templateName}' failed to parse:{Environment.NewLine}" +
                string.Join(Environment.NewLine, template.Messages));
        }

        return template;
    }
}