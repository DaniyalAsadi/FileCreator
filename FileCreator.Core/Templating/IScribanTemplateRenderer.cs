// FileCreator.Core/Templating/IScribanTemplateRenderer.cs
namespace FileCreator.Core.Templating;

/// <summary>
/// Renders a strongly-typed model into text via a named Scriban template.
/// This is the only thing a generator needs — it never touches Scriban's
/// Template/TemplateContext types directly.
/// </summary>
public interface IScribanTemplateRenderer
{
    Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken ct = default)
        where TModel : notnull;
}