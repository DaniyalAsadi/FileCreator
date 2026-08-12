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

        var model = new Dictionary<string, object?>
        {
            ["proto_package"] = NamingConventions.ToProtoPackage(csharpNamespace),
            ["csharp_namespace"] = csharpNamespace,
            ["service_name"] = first.ServiceName,
            ["proto_imports"] = imports,
            ["rpcs"] = endpoints.Select(ToRpc).ToList(),
            ["messages"] = BuildMessages(endpoints),
            ["enums"] = BuildEnums(endpoints)
        };
        return templates.Render("service.proto.sbn", model);
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

            if (endpoint.Request is null)
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

        foreach (var arg in reference.GenericArguments)
        {
            Scan(arg, imports);
        }
    }


    private static List<Dictionary<string, object?>> ToTemplateFields(IReadOnlyList<ProtoFieldInfo> fields) =>
        fields.Select(f => new Dictionary<string, object?>
        {
            ["name"] = f.Name,
            ["proto_name"] = f.ProtoName,
            ["proto_type_name"] = f.Reference.ProtoTypeName,
            ["field_number"] = f.FieldNumber,
            ["is_repeated"] = f.Reference.IsRepeated,
            ["is_nullable"] = f.IsNullable
        }).ToList();
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

        return messages.Values.ToList();
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
    IEnumerable<EndpointModel> endpoints)
    {
        var enums = new Dictionary<string, Dictionary<string, object?>>();

        foreach (var endpoint in endpoints)
        {
            AddEnums(endpoint.Request, enums);
            AddEnums(endpoint.Response, enums);
        }

        return [.. enums.Values];
    }
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