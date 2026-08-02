// FileCreator.Core/Generators/ICodeGenerator.cs
namespace FileCreator.Core.Generators;

/// <summary>
/// A generator turns one strongly-typed model into one piece of generated text.
/// It knows *which* template to use and *whether* to post-format the result —
/// nothing about how Scriban itself works.
/// </summary>
public interface ICodeGenerator<in TModel> where TModel : notnull
{
    Task<string> GenerateAsync(TModel model, CancellationToken ct = default);
}