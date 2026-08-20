    using GrpcScaffold.Core.Analysis.Models;
    using GrpcScaffold.Core.Generation;
    using Microsoft.CodeAnalysis;
    using Newtonsoft.Json;
    using static MappingExpressionBuilder;

/// <summary>
/// Client-side mapping: a CLR request object -> gRPC request message (what the client sends),
/// and the gRPC response message -> a CLR response object (what the caller gets back).
///
/// This is the mirror image of <see cref="MappingGenerator"/>:
///   server: grpc request  -> mediator message   |  client: clr request  -> grpc request
///   server: result        -> grpc response       |  client: grpc response -> clr response
///
/// Unlike the server side there's no separate "mediator message" contract to bridge to —
/// per ContractGenerator, Request/Response are themselves CLR records whose property names
/// already match the gRPC field names 1:1, so each contract plays both roles for itself.
/// </summary>
public sealed class ClientMappingGenerator(TemplateEngine templates)
{
    public string Generate(
        EndpointModel endpoint,
        string grpcNamespace)
    {
        var model = new Dictionary<string, object?>
        {
            ["endpoint_class_name"] = endpoint.EndpointClassName,

            ["grpc_namespace"] =
                $"{grpcNamespace}.Mappings",

            ["mapping_class_name"] =
                NamingConventions.MappingClassName(
                    endpoint.EndpointClassName),

            ["service_name"] =
                endpoint.ServiceName,

            ["request"] =
                endpoint.Request is null
                    ? null
                    : CreateContract(endpoint.Request),

            ["response"] =
                endpoint.Response is null
                    ? null
                    : CreateContract(endpoint.Response),

            ["has_request"] =
                endpoint.Request is not null,

            ["has_response"] =
                endpoint.Response is not null,

            // CLR request -> gRPC request
            ["request_mapping"] =
                BuildOutboundRequestMapping(endpoint),

            // gRPC response -> CLR response
            ["response_mapping"] =
                BuildInboundResponseMapping(endpoint),

            ["usings"] =
                BuildUsings(endpoint),

            ["grpc_request_type"] =
                endpoint.Request is null
                    ? "Google.Protobuf.WellKnownTypes.Empty"
                    : $"{grpcNamespace}.Contracts.{endpoint.Request.Name}",

            ["grpc_response_type"] =
                endpoint.Response is null
                    ? "Google.Protobuf.WellKnownTypes.Empty"
                    : $"{grpcNamespace}.Contracts.{endpoint.Response.Name}"
        };

        return templates.Render(
            "client-mapping.sbn",
            model);
    }

    private static IReadOnlyList<string> BuildUsings(
        EndpointModel endpoint)
    {
        var list = new List<string>();

        if (!string.IsNullOrWhiteSpace(endpoint.EndpointNamespace))
        {
            list.Add(endpoint.EndpointNamespace);
        }

        if (endpoint.Request is not null &&
            !string.IsNullOrWhiteSpace(endpoint.Request.Namespace))
        {
            list.Add(endpoint.Request.Namespace);
        }

        if (endpoint.Response is not null &&
            !string.IsNullOrWhiteSpace(endpoint.Response.Namespace))
        {
            list.Add(endpoint.Response.Namespace);
        }

        return list
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<Dictionary<string, object?>>
        BuildOutboundRequestMapping(
            EndpointModel endpoint)
    {
        if (endpoint.Request is null)
        {
            return [];
        }

        var lookup = FlattenByClrType(endpoint.Request);

        return
        [
            .. endpoint.Request.Fields.Select(field =>
            {
                var expression =
                    BuildClrToProtoExpression(
                        field.Reference,
                        $"request.{field.Name}",
                        lookup);

                return new Dictionary<string, object?>
                {
                    ["destination"] = field.Name,
                    ["expression"] = expression,
                    ["is_repeated"] = field.Reference.IsRepeated,
                    ["needs_review"] =
                        expression.Contains(
                            "/* TODO",
                            StringComparison.Ordinal)
                };
            })
        ];
    }

    private static IReadOnlyList<Dictionary<string, object?>>
        BuildInboundResponseMapping(
            EndpointModel endpoint)
    {
        if (endpoint.Response is null)
        {
            return [];
        }

        var ctor =
            endpoint.Response.PreferredConstructor;

        if (ctor is null ||
            ctor.Parameters.Count == 0)
        {
            return [];
        }

        var responseFields =
            endpoint.Response.Fields;

        var lookup =
            FlattenByClrType(endpoint.Response);

        var visiting =
            new HashSet<ITypeSymbol>(
                SymbolEqualityComparer.Default);

        return ctor.Parameters
            .Select(parameter =>
            {
                var (expression, needsReview) =
                    BuildConstructorArgumentExpression(
                        parameter,
                        "response",
                        responseFields,
                        lookup,
                        lookup,
                        visiting);

                return new Dictionary<string, object?>
                {
                    ["destination"] = parameter.Name,
                    ["expression"] = expression,
                    ["is_optional"] = parameter.IsOptional,
                    ["has_default"] = parameter.HasDefaultValue,
                    ["needs_review"] = needsReview
                };
            })
            .ToList();
    }
}
