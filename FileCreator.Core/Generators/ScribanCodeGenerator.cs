// FileCreator.Core/Generators/ScribanCodeGenerator.cs
using FileCreator.Core.Templating;

namespace FileCreator.Core.Generators;

/// <summary>
/// Base for every Scriban-backed generator. Concrete generators only declare
/// <see cref="TemplateName"/> (and optionally override <see cref="FormatOutput"/>
/// for non-C# templates such as .proto files, where Roslyn formatting doesn't apply).
/// </summary>
public abstract class ScribanCodeGenerator<TModel>(IScribanTemplateRenderer renderer) : ICodeGenerator<TModel>
    where TModel : notnull
{
    protected abstract string TemplateName { get; }

    /// <summary>Override and return false for non-C# templates (e.g. .proto, .json).</summary>
    protected virtual bool FormatOutput => true;

    public virtual async Task<string> GenerateAsync(TModel model, CancellationToken ct = default)
    {
        var rendered = await renderer.RenderAsync(TemplateName, model, ct);
        return FormatOutput ? RoslynCodeFormatter.Format(rendered) : rendered;
    }
}