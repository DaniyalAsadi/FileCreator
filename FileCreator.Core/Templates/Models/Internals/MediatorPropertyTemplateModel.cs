namespace FileCreator.Core.Templates.Models.Internals;

public sealed class MediatorPropertyTemplateModel
{
    public required string TypeName { get; init; }

    public required string Name { get; init; }

    public required string InitializerName { get; init; }
}