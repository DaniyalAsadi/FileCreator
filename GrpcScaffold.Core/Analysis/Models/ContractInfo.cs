using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GrpcScaffold.Core.Analysis.Models;

public sealed record ContractInfo
{
    [JsonIgnore]
    public ITypeSymbol ClrType { get; init; } = default!;

    public required string Name { get; init; }

    public required string Namespace { get; init; }

    public IReadOnlyList<ProtoFieldInfo> Fields { get; init; } = [];
    public IReadOnlyList<ContractInfo> Dependencies { get; init; } = [];
    public IReadOnlyList<ConstructorInfo> Constructors { get; init; } = [];

    public ConstructorInfo? PreferredConstructor { get; init; } = default!;



}
