namespace FileCreator.Core.Templates.Models.Internals;

public sealed class PropertyTemplateModel
{
    public required string TypeName { get; init; }

    public required string Name { get; init; }

    public required bool IsRequired { get; init; }
}
