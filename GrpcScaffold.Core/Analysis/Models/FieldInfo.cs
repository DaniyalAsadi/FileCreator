// src/GrpcScaffold.Core/Analysis/Models/FieldInfo.cs
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GrpcScaffold.Core.Analysis.Models;

public sealed record ProtoFieldInfo(
    string Name,
    string ProtoName,
    ProtoTypeReference Reference,
    bool IsNullable,
    int FieldNumber);


public sealed record ProtoTypeReference
{
    [JsonIgnore]
    public ITypeSymbol ClrType { get; init; } = default!;

    public required string ProtoTypeName { get; init; }

    public bool IsPrimitive { get; init; }

    public bool IsEnum { get; init; }

    public bool IsMessage { get; init; }

    public bool IsRepeated { get; init; }

    public bool IsNullable { get; init; }

    public bool IsWellKnownType { get; init; }
    public bool IsStruct { get; init; }

    public ProtoTypeReference? ElementType { get; init; }

     public bool IsMap { get; init; }

    public ITypeSymbol? MapKeyType { get; init; }
    public ITypeSymbol? MapValueType { get; init; }


    public IReadOnlyList<ProtoTypeReference> GenericArguments { get; init; }
        = [];
}

public sealed record EnumInfo
{
    public required string Name { get; init; }

    public required string Namespace { get; init; }

    public required IReadOnlyList<EnumValueInfo> Values { get; init; }
}
public sealed record EnumValueInfo(
    string Name,
    int Number);