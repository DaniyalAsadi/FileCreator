// src/GrpcScaffold.Core/Generation/ProtoGenerator.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;
using System.Xml.Linq;

namespace GrpcScaffold.Core.Generation;

public sealed class ProtoGenerator(TemplateEngine templates)
{
    public string Generate(
    IReadOnlyList<EndpointModel> endpoints,
    string csharpNamespace)
    {
        var first = endpoints[0];

        var imports = BuildImports(endpoints);

        var contracts = CollectContractGraph(endpoints);
        var messages = BuildMessages(endpoints);
        var wrapperMessages = BuildMapWrappers(contracts);
        var enums = BuildEnums(endpoints, contracts);

        var model = new Dictionary<string, object?>
        {
            ["proto_package"] = NamingConventions.ToProtoPackage(csharpNamespace),
            ["csharp_namespace"] = csharpNamespace,
            ["service_name"] = first.ServiceName,
            ["proto_imports"] = imports,
            ["rpcs"] = endpoints.Select(ToRpc).ToList(),
            ["messages"] = messages.Concat(wrapperMessages).ToList(),
            ["enums"] = enums
        };
        return templates.Render("service-proto.sbn", model);
    }

    private static List<string> BuildImports(
    IEnumerable<EndpointModel> endpoints)
    {
        var imports = new HashSet<string>();

        foreach (var endpoint in endpoints)
        {
            if (endpoint.Request is null)
                imports.Add("google/protobuf/empty.proto");

            
            Scan(endpoint.Request, imports);

            if (endpoint.Response is null)
                imports.Add("google/protobuf/empty.proto");
            Scan(endpoint.Response, imports);
        }

        return imports.Distinct(StringComparer.Ordinal).OrderBy(x => x).ToList();
    }
    private static void Scan(
    ContractInfo? contract,
    ISet<string> imports)
    {
        if (contract is null)
            return;

        foreach (var field in contract.Fields)
        {
            Scan(field.Reference, imports);
        }

        foreach (var dependency in contract.Dependencies)
        {
            Scan(dependency, imports);
        }
    }

    private static void Scan(
        ProtoTypeReference reference,
        ISet<string> imports)
    {
        if (reference.IsWellKnownType &&
            reference.ProtoTypeName == "google.protobuf.Timestamp")
        {
            imports.Add("google/protobuf/timestamp.proto");
        }
        if (reference.IsStruct)
        {
            imports.Add("google/protobuf/struct.proto");
        }
        foreach (var arg in reference.GenericArguments)
        {
            Scan(arg, imports);
        }
    }

    private static List<Dictionary<string, object?>> ToTemplateFields(
    IReadOnlyList<ProtoFieldInfo> fields) =>
    [.. fields.Select(f => new Dictionary<string, object?>
    {
        ["name"] = f.Name,
        ["proto_name"] = f.ProtoName,
        ["proto_type_name"] = f.Reference.ProtoTypeName,
        ["field_number"] = f.FieldNumber,

        ["is_repeated"] = f.Reference.IsRepeated,
        ["is_nullable"] = f.IsNullable,

        ["is_map"] = f.Reference.IsMap,
        ["is_struct"] = f.Reference.IsStruct,

        // The map key/value are now full protobuf references (see ProtoTypeMapper.Map), so we
        // emit their resolved proto type name — never the raw CLR symbol, which would leak
        // C# nullability (e.g. `string?`) into invalid protobuf syntax.
        ["map_key_type"] = f.Reference.MapKeyReference?.ProtoTypeName,

        ["map_value_type"] = f.Reference.MapValueReference?.ProtoTypeName
    })];
    private static Dictionary<string, object?> ToRpc(
    EndpointModel endpoint)
    {
        return new()
        {
            ["rpc_name"] = endpoint.RpcName,
            ["request"] = endpoint.Request,
            ["response"] = endpoint.Response,
        };
    }

    private static List<Dictionary<string, object?>> BuildMessages(IEnumerable<EndpointModel> endpoints)
    {
        var messages = new Dictionary<string, Dictionary<string, object?>>();

        foreach (var endpoint in endpoints)
        {
            AddMessage(endpoint.Request, messages);

            AddMessage(endpoint.Response, messages);
        }

        return [.. messages.Values];
    }
    private static void AddMessage(
    ContractInfo? contract,
    IDictionary<string, Dictionary<string, object?>> messages)
    {
        if (contract is null)
            return;

        var key = $"{contract.Namespace}.{contract.Name}";

        if (!messages.TryAdd(key, CreateMessage(contract)))
            return;

        foreach (var dependency in contract.Dependencies)
        {
            AddMessage(dependency, messages);
        }
    }
    private static void Visit(
    ProtoTypeReference reference,
    IDictionary<string, Dictionary<string, object?>> messages)
    {
        if (reference.IsPrimitive ||
            reference.IsEnum ||
            reference.IsWellKnownType)
        {
            return;
        }

        if (reference.ElementType is not null)
        {
            Visit(reference.ElementType, messages);
        }

        foreach (var arg in reference.GenericArguments)
        {
            Visit(arg, messages);
        }

        if (!reference.IsMessage)
            return;

        var contract = new ContractInfo
        {
            ClrType = reference.ClrType,
            Name = reference.ProtoTypeName,
            Namespace = reference.ClrType.ContainingNamespace.ToDisplayString(),
            Fields = ProtoTypeMapper.ExtractFields(reference.ClrType)
        };

        AddMessage(contract, messages);
    }
    private static Dictionary<string, object?> CreateMessage(ContractInfo contract)
    {
        return new Dictionary<string, object?>
        {
            ["key"] = $"{contract.Namespace}.{contract.Name}",
            ["name"] = contract.Name,
            ["fields"] = ToTemplateFields(contract.Fields)
        };
    }
    private static List<Dictionary<string, object?>> BuildEnums(
        IEnumerable<EndpointModel> endpoints,
        IReadOnlyList<ContractInfo> contracts)
    {
        var enums = new Dictionary<string, Dictionary<string, object?>>();

        foreach (var endpoint in endpoints)
        {
            AddEnums(endpoint.Request, enums);
            AddEnums(endpoint.Response, enums);
        }

        // Enums used as the (nullable) value of a wrapped map also need a proto enum def.
        WalkReferences(contracts, reference =>
        {
            if (reference.IsMap && reference.MapValueIsWrapped &&
                reference.MapValueReference is { WrapperValueReference: { IsEnum: true } inner })
            {
                CollectEnum(inner, enums);
            }
        });

        return [.. enums.Values];
    }

    /// <summary>
    /// Collects every reachable <see cref="ContractInfo"/> (top-level request/response, their
    /// declared dependencies, and nested message types discovered by walking fields) so the
    /// wrapper/enum discovery below can operate on the full type graph.
    /// </summary>
    private static List<ContractInfo> CollectContractGraph(IEnumerable<EndpointModel> endpoints)
    {
        var result = new List<ContractInfo>();
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        void Visit(ContractInfo? contract)
        {
            if (contract is null || !seen.Add(contract.ClrType))
                return;

            result.Add(contract);

            foreach (var dependency in contract.Dependencies)
                Visit(dependency);

            foreach (var field in contract.Fields)
            {
                var f = field.Reference;
                if (f.IsMessage && f.ClrType is { } t)
                {
                    Visit(new ContractInfo
                    {
                        ClrType = t,
                        Name = f.ProtoTypeName,
                        Namespace = t.ContainingNamespace.ToDisplayString(),
                        Fields = ProtoTypeMapper.ExtractFields(t),
                        Dependencies = []
                    });
                }
            }
        }

        foreach (var endpoint in endpoints)
        {
            Visit(endpoint.Request);
            Visit(endpoint.Response);
        }

        return result;
    }

    /// <summary>
    /// Walks every protobuf reference reachable from <paramref name="contracts"/> (fields,
    /// element/repeated item types, map values, and nested message fields) invoking
    /// <paramref name="visit"/> on each — the single traversal used to discover both the
    /// generated nullable map-value wrappers and the enums they reference.
    /// </summary>
    private static void WalkReferences(
        IReadOnlyList<ContractInfo> contracts,
        Action<ProtoTypeReference> visit)
    {
        var visiting = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var contract in contracts)
        {
            foreach (var field in contract.Fields)
                WalkReference(field.Reference, visit, visiting);
        }
    }

    private static void WalkReference(
        ProtoTypeReference reference,
        Action<ProtoTypeReference> visit,
        ISet<ITypeSymbol> visiting)
    {
        visit(reference);

        if (reference.ElementType is { } element)
            WalkReference(element, visit, visiting);

        foreach (var arg in reference.GenericArguments)
            WalkReference(arg, visit, visiting);

        if (reference.MapValueReference is { } value)
            WalkReference(value, visit, visiting);

        if (reference.IsMessage && !reference.IsWrapper && reference.ClrType is { } clrType && visiting.Add(clrType))
        {
            foreach (var f in ProtoTypeMapper.ExtractFields(clrType))
                WalkReference(f.Reference, visit, visiting);
            visiting.Remove(clrType);
        }
    }

    /// <summary>
    /// Builds the generated message definitions for nullable map-value wrappers
    /// (<c>Nullable&lt;T&gt;</c>), one per distinct (proto) value type. Each wraps a single
    /// <c>value</c> field of the underlying proto type, so a map entry's presence encodes the
    /// CLR value's nullability — the semantic protobuf maps cannot otherwise express.
    /// </summary>
    private static List<Dictionary<string, object?>> BuildMapWrappers(
        IReadOnlyList<ContractInfo> contracts)
    {
        var wrappers = new Dictionary<string, Dictionary<string, object?>>();

        WalkReferences(contracts, reference =>
        {
            if (reference.IsMap && reference.MapValueIsWrapped &&
                reference.MapValueReference is { WrapperValueReference: { } inner } wrapper)
            {
                wrappers.TryAdd(wrapper.ProtoTypeName, WrapMessage(wrapper.ProtoTypeName, inner));
            }
        });

        return [.. wrappers.Values];
    }

    private static Dictionary<string, object?> WrapMessage(string name, ProtoTypeReference inner) => new()
    {
        ["key"] = name,
        ["name"] = name,
        ["fields"] = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["name"] = "value",
                ["proto_name"] = "value",
                ["proto_type_name"] = inner.ProtoTypeName,
                ["field_number"] = 1,
                ["is_repeated"] = false,
                ["is_nullable"] = false,
                ["is_map"] = false,
                ["is_struct"] = false,
                ["map_key_type"] = null,
                ["map_value_type"] = null,
                ["is_enum"] = inner.IsEnum,
                ["is_message"] = inner.IsMessage,
                ["is_well_known"] = inner.IsWellKnownType,
                ["needs_cast"] = false
            }
        }
    };
    private static void AddEnums(
    ContractInfo? contract,
    IDictionary<string, Dictionary<string, object?>> enums)
    {
        if (contract is null)
            return;

        foreach (var field in contract.Fields)
        {
            CollectEnum(field.Reference, enums);
        }

        foreach (var dependency in contract.Dependencies)
        {
            AddEnums(dependency, enums);
        }
    }
    private static void CollectEnum(
    ProtoTypeReference reference,
    IDictionary<string, Dictionary<string, object?>> enums)
    {
        if (reference.ElementType is not null)
        {
            CollectEnum(reference.ElementType, enums);
        }

        foreach (var arg in reference.GenericArguments)
        {
            CollectEnum(arg, enums);
        }

        if (!reference.IsEnum)
            return;

        var key = reference.ClrType.ToDisplayString();

        enums.TryAdd(key, CreateEnum(reference.ClrType));
    }
    private static Dictionary<string, object?> CreateEnum(ITypeSymbol type)
    {
        var enumType = (INamedTypeSymbol)type;

        return new Dictionary<string, object?>
        {
            ["name"] = enumType.Name,
            ["values"] = enumType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .Select(f => new Dictionary<string, object?>
                {
                    ["name"] = f.Name,
                    ["number"] = Convert.ToInt32(f.ConstantValue)
                })
                .ToList()
        };
    }
}

/*
message CollectionResponse {
  repeated  SelectItem items = 1;
}

message SelectItem {
   bool disabled = 1;
   SelectItemGroup group = 2;
   bool selected = 3;
   string text = 4;
   string value = 5;
}


  <ItemGroup>
    <Protobuf Include="Grpc\Protos\**\*.proto" />
  </ItemGroup>

*/