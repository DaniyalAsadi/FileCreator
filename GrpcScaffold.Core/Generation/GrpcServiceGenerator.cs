// src/GrpcScaffold.Core/Generation/GrpcServiceGenerator.cs
using GrpcScaffold.Core.Analysis.Models;

namespace GrpcScaffold.Core.Generation;


public sealed class GrpcServiceGenerator(TemplateEngine templates)
{
    public string Generate(
        IReadOnlyList<EndpointModel> endpoints,
        string grpcNamespace)
    {
        var first = endpoints[0];

        var model = new Dictionary<string, object?>
        {
            ["grpc_namespace"] = grpcNamespace,
            ["service_name"] = first.ServiceName,
            ["usings"] = BuildUsings(endpoints),
            ["rpcs"] = endpoints
                .Select(ToRpc)
                .ToList()
        };

        return templates.Render("grpc-service.sbn", model);
    }

    private static IReadOnlyList<string> BuildUsings(
        IEnumerable<EndpointModel> endpoints)
    {
        return endpoints
            .SelectMany(e => new[]
            {
                e.EndpointNamespace,
                e.MediatorMessage.Namespace,
                e.Request?.Namespace,
                e.Response?.Namespace
            })
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x)
            .ToList()!;
    }
    private static Dictionary<string, object?> ToRpc(
    EndpointModel endpoint)
    {
        return new()
        {
            ["rpc_name"] = endpoint.RpcName,
            ["request"] = endpoint.Request,
            ["response"] = endpoint.Response,
            ["request_type"] = endpoint.Request?.ClrType.Name,
            ["response_type"] = endpoint.Response?.ClrType.Name,
            ["mapping_class_name"] =
                NamingConventions.MappingClassName(endpoint.EndpointClassName)
        };
    }
}