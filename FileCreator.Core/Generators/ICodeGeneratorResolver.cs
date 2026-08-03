using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Generators;

public interface ICodeGeneratorResolver
{
    ICodeGenerator Resolve(Type modelType);
}
public sealed class CodeGeneratorResolver(
    IEnumerable<ICodeGenerator> generators)
    : ICodeGeneratorResolver
{
    private readonly Dictionary<Type, ICodeGenerator> _map =
        generators.ToDictionary(
            x => x.ModelType);


    public ICodeGenerator Resolve(Type modelType)
    {
        if (!_map.TryGetValue(modelType, out var generator))
        {
            throw new InvalidOperationException(
                $"Generator not found for {modelType.Name}");
        }

        return generator;
    }
}