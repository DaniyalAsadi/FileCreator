using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Generators;

public sealed class ScribanFileCreator(
    ICodeGeneratorResolver resolver)
{
    private readonly ICodeGeneratorResolver _resolver = resolver;


    public async Task<string> GenerateAsync<TModel>(
        TModel model,
        CancellationToken ct = default)
        where TModel : notnull, IGeneratorModel
    {
        var generator = _resolver.Resolve(typeof(TModel));


        return await generator.GenerateAsync(model, ct);
    }
}