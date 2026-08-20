// src/GrpcScaffold.Core/Generation/GrpcClientGenerator.cs
using GrpcScaffold.Core.Analysis.Models;

namespace GrpcScaffold.Core.Generation;

public sealed class GrpcClientGenerator(TemplateEngine templates)
{
    public string Generate(IReadOnlyList<EndpointModel> endpoints, string grpcNamespace)
    {
        return Generate(
            endpoints,
            clientNamespace: grpcNamespace,
            protoNamespace: grpcNamespace,
            mappingNamespace: $"{grpcNamespace}.Mappings",
            contractNamespace: grpcNamespace);
    }

    public string Generate(
        IReadOnlyList<EndpointModel> endpoints,
        string clientNamespace,
        string protoNamespace,
        string mappingNamespace,
        string contractNamespace)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (endpoints.Count == 0)
            throw new ArgumentException("At least one endpoint is required.", nameof(endpoints));

        var first = endpoints[0];

        if (endpoints.Any(e => !string.Equals(e.ServiceName, first.ServiceName, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "A generated gRPC client can only contain endpoints from one service group.",
                nameof(endpoints));
        }

        var model = new Dictionary<string, object?>
        {
            ["client_namespace"] = clientNamespace,
            ["proto_namespace"] = protoNamespace,
            ["mapping_namespace"] = mappingNamespace,
            ["contract_namespace"] = contractNamespace,
            ["service_name"] = first.ServiceName,
            ["client_class_name"] = $"{first.ServiceName}GrpcClient",
            ["usings"] = BuildUsings(contractNamespace),
            ["rpcs"] = endpoints
                .OrderBy(e => e.RpcName, StringComparer.Ordinal)
                .Select(e => ToRpc(e, contractNamespace))
                .ToList()
        };

        return templates.Render("grpc-client.sbn", model);
    }

    private static IReadOnlyList<string> BuildUsings(string contractNamespace)
    {
        return new[]
            {
                "System",
                "System.Threading",
                "System.Threading.Tasks",
                "Grpc.Core",
                "Google.Protobuf.WellKnownTypes",
                contractNamespace
            }
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static Dictionary<string, object?> ToRpc(EndpointModel endpoint, string contractNamespace)
    {
        return new()
        {
            ["rpc_name"] = endpoint.RpcName,
            ["request"] = endpoint.Request is not null,
            ["response"] = endpoint.Response is not null,
            ["request_type"] = endpoint.Request is null ? null : $"{contractNamespace}.{endpoint.Request.Name}",
            ["response_type"] = endpoint.Response is null ? null : $"{contractNamespace}.{endpoint.Response.Name}",
            ["mapping_class_name"] = NamingConventions.MappingClassName(endpoint.EndpointClassName)
        };
    }
}
