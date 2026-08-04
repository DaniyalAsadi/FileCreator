using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GrpcScaffold.Core.Analysis.Models;

public sealed record ConstructorInfo
{
    public required string Name { get; init; }

    public bool IsPublic { get; init; }

    public bool IsParameterless { get; init; }

    /// <summary>
    /// Constructor chosen by the analyzer for object creation.
    /// </summary>
    public bool IsPreferred { get; init; }

    public IReadOnlyList<ConstructorParameterInfo> Parameters { get; init; } = [];
}

public sealed record ConstructorParameterInfo
{
    public required string Name { get; init; }

    public required string TypeName { get; init; }

    [JsonIgnore]
    public ITypeSymbol Type { get; init; } = default!;

    public string? SourceFieldName { get; init; }

    public bool IsOptional { get; init; }

    public bool HasDefaultValue { get; init; }

    public object? DefaultValue { get; init; }

    public bool IsNullable { get; init; }

    public bool IsParams { get; init; }

    public RefKind RefKind { get; init; }
}