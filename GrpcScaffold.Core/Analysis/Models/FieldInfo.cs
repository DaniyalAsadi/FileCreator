// src/GrpcScaffold.Core/Analysis/Models/FieldInfo.cs
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GrpcScaffold.Core.Analysis.Models;

public sealed record ProtoFieldInfo(
    string Name,
    string ProtoName,
    ProtoTypeReference Reference,
    bool IsNullable,
    int FieldNumber)
{
    [JsonIgnore]
    public ITypeSymbol? DeclaredClrType { get; init; }
}



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

    /// <summary>
    /// The mapped protobuf reference for this map's KEY type. Built by
    /// <see cref="ProtoTypeMapper.Map"/> so the key gets a proper proto type name
    /// (e.g. <c>string</c> / <c>int32</c>) instead of a raw CLR <see cref="ITypeSymbol"/>.
    /// </summary>
    public ProtoTypeReference? MapKeyReference { get; init; }

    /// <summary>
    /// The mapped protobuf reference for this map's VALUE type. This is the single source of
    /// truth for the value's proto name AND nullability, including the nullable-wrapper
    /// indirection described by <see cref="MapValueIsWrapped"/>.
    /// </summary>
    public ProtoTypeReference? MapValueReference { get; init; }

    /// <summary>
    /// True when the map's value is wrapped in a generated message (<c>Nullable&lt;T&gt;</c>)
    /// so its null/presence semantic survives protobuf's lack of nullable map values. When
    /// true, <see cref="MapValueReference"/> is that wrapper message (see
    /// <see cref="IsWrapper"/> / <see cref="WrapperValueReference"/>).
    /// </summary>
    public bool MapValueIsWrapped { get; init; }

    /// <summary>
    /// True for a generated map-value wrapper message. Wrapper messages are produced by
    /// <see cref="ProtoTypeMapper.Map"/> (never from a real CLR type) and carry a single
    /// <c>value</c> field described by <see cref="WrapperValueReference"/>.
    /// </summary>
    public bool IsWrapper { get; init; }

    /// <summary>
    /// For a wrapper message, the underlying (nullable) value type being preserved.
    /// </summary>
    public ProtoTypeReference? WrapperValueReference { get; init; }

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