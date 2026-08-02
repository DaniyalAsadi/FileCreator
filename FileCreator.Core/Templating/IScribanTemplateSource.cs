// FileCreator.Core/Templating/IScribanTemplateSource.cs
namespace FileCreator.Core.Templating;

/// <summary>
/// Resolves the raw text of a named template ("endpoint.sbn", "request.sbn", ...).
/// Kept separate from the renderer so the source (embedded resource today, a
/// folder on disk tomorrow for hot-reload during template authoring) can change
/// without touching rendering/caching logic.
/// </summary>
public interface IScribanTemplateSource
{
    /// <param name="templateName">Logical name, e.g. "endpoint.sbn".</param>
    string GetTemplateText(string templateName);
}