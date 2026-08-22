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
            ["usings"] = BuildUsings(contractNamespace),
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

    private static IReadOnlyList<string> BuildUsings(string contractNamespace)
    {
        var list = new List<string> { contractNamespace };

        // Scalar conversions use these namespaces in generated expressions.
        list.Add("System");
        list.Add("System.Collections.Generic");
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
            // proto3 `optional` scalar/string field with a nullable CLR source (gap #6):
            // the template guards the assignment at statement level so null never sets
            // the presence bit, and the expression carries the plain non-null value.
            var hasPresence = field.IsNullable &&
                ProtoTypeConversion.HasProtoPresenceAccessor(field.Reference);

            var expression = BuildClrToProtoExpression(
                field.Reference,
                $"request.{field.Name}",
                lookup,
                protoNamespace: protoNamespace,
                // Reference-type nullability (string?, Details?) only exists as a field
                // annotation — the type reference itself never carries it.
                clrNullable: field.IsNullable,
                presenceHandledByCaller: hasPresence);

            ThrowIfUnsupported(endpoint, field, expression, "client request");

            return new Dictionary<string, object?>
            {
                ["destination"] = field.Name,
                ["source"] = $"request.{field.Name}",
                ["expression"] = expression,
                ["is_repeated"] = field.Reference.IsRepeated,
                ["has_presence"] = hasPresence,
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

            // proto3 `optional` scalar/string fields expose a HasX accessor generated from the
            // same nullability annotation the proto template used — reading it maps "unset on
            // the wire" to null instead of 0 / "" / a parse exception (gaps #5/#6).
            var hasPresence = field.IsNullable &&
                ProtoTypeConversion.HasProtoPresenceAccessor(field.Reference);

            var expression = BuildProtoToClrExpression(
                field.Reference,
                $"response.{field.Name}",
                lookup,
                materializer,
                clrNamespaceOverride: contractNamespace,
                // Reference-type nullability (Details?, List<T>?) only exists as a field
                // annotation — the type reference itself never carries it.
                clrNullable: field.IsNullable,
                presenceSource: hasPresence ? $"response.Has{field.Name}" : null,
                // The generated BFF contract preserves the annotation, and Nullable<T> covers
                // nullable-disabled analysis contexts.
                destinationNullable: field.IsNullable || field.Reference.IsNullable);

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
