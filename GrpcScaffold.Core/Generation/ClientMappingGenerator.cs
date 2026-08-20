using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;
using static GrpcScaffold.Core.Generation.MappingExpressionBuilder;

namespace GrpcScaffold.Core.Generation;

/// <summary>
/// Client-side mapping: BFF CLR contracts -> generated protobuf request messages, and
/// generated protobuf response messages -> BFF CLR contracts.
/// </summary>
public sealed class ClientMappingGenerator(TemplateEngine templates)
{
    public string Generate(EndpointModel endpoint, string grpcNamespace)
    {
        return Generate(
            endpoint,
            mappingNamespace: $"{grpcNamespace}.Mappings",
            protoNamespace: $"{grpcNamespace}.Contracts",
            contractNamespace: $"{grpcNamespace}.Contracts");
    }

    public string Generate(
        EndpointModel endpoint,
        string mappingNamespace,
        string protoNamespace,
        string contractNamespace)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var request = endpoint.Request is null ? null : CreateClientContract(endpoint.Request, contractNamespace);
        var response = endpoint.Response is null ? null : CreateClientContract(endpoint.Response, contractNamespace);

        var model = new Dictionary<string, object?>
        {
            ["endpoint_class_name"] = endpoint.EndpointClassName,
            ["grpc_namespace"] = mappingNamespace,
            ["mapping_class_name"] = NamingConventions.MappingClassName(endpoint.EndpointClassName),
            ["service_name"] = endpoint.ServiceName,
            ["request"] = request,
            ["response"] = response,
            ["has_request"] = endpoint.Request is not null,
            ["has_response"] = endpoint.Response is not null,
            ["request_mapping"] = BuildOutboundRequestMapping(endpoint, protoNamespace),
            ["response_mapping"] = BuildInboundResponseMapping(endpoint, contractNamespace),
            ["usings"] = BuildUsings(endpoint, contractNamespace),
            ["grpc_request_type"] = endpoint.Request is null
                ? "Google.Protobuf.WellKnownTypes.Empty"
                : $"{protoNamespace}.{endpoint.Request.Name}",
            ["grpc_response_type"] = endpoint.Response is null
                ? "Google.Protobuf.WellKnownTypes.Empty"
                : $"{protoNamespace}.{endpoint.Response.Name}"
        };

        return templates.Render("client-mapping.sbn", model);
    }

    private static Dictionary<string, object?> CreateClientContract(ContractInfo contract, string contractNamespace)
    {
        var model = CreateContract(contract);
        model["namespace"] = contractNamespace;
        model["type_name"] = $"{contractNamespace}.{contract.Name}";
        return model;
    }

    private static IReadOnlyList<string> BuildUsings(EndpointModel endpoint, string contractNamespace)
    {
        var list = new List<string> { contractNamespace };

        // Scalar conversions use these namespaces in generated expressions.
        list.Add("System");
        list.Add("System.Collections.Generic");

        if (endpoint.Request is not null && !string.IsNullOrWhiteSpace(endpoint.Request.Namespace))
            list.Add(endpoint.Request.Namespace);

        if (endpoint.Response is not null && !string.IsNullOrWhiteSpace(endpoint.Response.Namespace))
            list.Add(endpoint.Response.Namespace);

        return list
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<Dictionary<string, object?>> BuildOutboundRequestMapping(
        EndpointModel endpoint,
        string protoNamespace)
    {
        if (endpoint.Request is null)
            return [];

        var lookup = FlattenByClrType(endpoint.Request);

        return [.. endpoint.Request.Fields.Select(field =>
        {
            var expression = BuildClrToProtoExpression(
                field.Reference,
                $"request.{field.Name}",
                lookup,
                protoNamespace: protoNamespace);

            ThrowIfUnsupported(endpoint, field, expression, "client request");

            return new Dictionary<string, object?>
            {
                ["destination"] = field.Name,
                ["expression"] = expression,
                ["is_repeated"] = field.Reference.IsRepeated,
                ["needs_review"] = false
            };
        })];
    }

    private static IReadOnlyList<Dictionary<string, object?>> BuildInboundResponseMapping(EndpointModel endpoint, string contractNamespace)
    {
        if (endpoint.Response is null)
            return [];

        var lookup = FlattenByClrType(endpoint.Response);

        return [.. endpoint.Response.Fields.Select(field =>
        {
            var materializer = ProtoTypeConversion.CollectionMaterializer(field.DeclaredClrType ?? field.Reference.ClrType);
            var expression = BuildProtoToClrExpression(
                field.Reference,
                $"response.{field.Name}",
                lookup,
                materializer,
                clrNamespaceOverride: contractNamespace);

            ThrowIfUnsupported(endpoint, field, expression, "client response");

            return new Dictionary<string, object?>
            {
                ["destination"] = field.Name,
                ["expression"] = expression,
                ["is_repeated"] = field.Reference.IsRepeated,
                ["needs_review"] = false
            };
        })];
    }

    private static void ThrowIfUnsupported(
        EndpointModel endpoint,
        ProtoFieldInfo field,
        string expression,
        string direction)
    {
        if (!expression.Contains("/* TODO", StringComparison.Ordinal))
            return;

        throw new InvalidOperationException(
            $"Unable to generate {direction} mapping for endpoint '{endpoint.EndpointClassName}': " +
            $"property '{field.Name}' of CLR type '{field.Reference.ClrType.ToDisplayString()}' " +
            $"cannot be mapped to protobuf field '{field.Reference.ProtoTypeName}'. Generated expression: {expression}");
    }
}
