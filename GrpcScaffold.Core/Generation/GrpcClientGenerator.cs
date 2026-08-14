using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Text;

// src/GrpcScaffold.Core/Generation/GrpcClientGenerator.cs
namespace GrpcScaffold.Core.Generation;


public sealed class GrpcClientGenerator(TemplateEngine templates)
{
    public string Generate(
        IReadOnlyList<EndpointModel> endpoints,
        string grpcNamespace)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (endpoints.Count == 0)
            throw new ArgumentException(
                "At least one endpoint is required.",
                nameof(endpoints));

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

        return templates.Render("grpc-client.sbn", model);
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
            .OrderBy(x => x, StringComparer.Ordinal)
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

            ["request_type"] =
                endpoint.Request?.ClrType.Name,

            ["response_type"] =
                endpoint.Response?.ClrType.Name,

            ["mapping_class_name"] =
                NamingConventions.MappingClassName(
                    endpoint.EndpointClassName)
        };
    }
}