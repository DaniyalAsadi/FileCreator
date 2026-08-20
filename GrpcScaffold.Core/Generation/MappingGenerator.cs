using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;
using static GrpcScaffold.Core.Generation.MappingExpressionBuilder;

namespace GrpcScaffold.Core.Generation;

/// <summary>
/// Server-side mapping: gRPC request -> mediator message (handler input), and the handler's
/// result -> gRPC response.
/// </summary>
public sealed class MappingGenerator(TemplateEngine templates)
{
    public string Generate(EndpointModel endpoint, string mapperNameSpace, string protoNameSpace)
    {
        var model = new Dictionary<string, object?>
        {
            ["endpoint_class_name"] = endpoint.EndpointClassName,
            ["mapper_namespace"] = mapperNameSpace,
            ["proto_namespace"] = protoNameSpace,
            ["mapping_class_name"] = NamingConventions.MappingClassName(endpoint.EndpointClassName),
            ["service_name"] = endpoint.ServiceName,

            ["request"] = CreateContract(endpoint.Request),
            ["response"] = CreateContract(endpoint.Response),
            ["mediator_message"] = CreateContract(endpoint.MediatorMessage),

            ["has_request"] = endpoint.Request is not null,
            ["has_response"] = endpoint.Response is not null,

            // grpc request -> mediator message constructor
            ["query_mapping"] = BuildRequestMappings(endpoint),
            // result -> grpc response
            ["response_mapping"] = BuildResponseMappings(endpoint, protoNameSpace),

            ["usings"] = BuildUsings(endpoint)
        };

        model["grpc_request_type"] = endpoint.Request is null
            ? "Google.Protobuf.WellKnownTypes.Empty"
            : $"{protoNameSpace}.{endpoint.Request.Name}";

        model["grpc_response_type"] = endpoint.Response is null
            ? "Google.Protobuf.WellKnownTypes.Empty"
            : $"{protoNameSpace}.{endpoint.Response.Name}";

        return templates.Render("mapping.sbn", model);
    }

    private static IReadOnlyList<string> BuildUsings(EndpointModel endpoint)
    {
        var list = new List<string> { endpoint.EndpointNamespace, endpoint.MediatorMessage.Namespace };

        if (endpoint.Request is not null) list.Add(endpoint.Request.Namespace);
        if (endpoint.Response is not null) list.Add(endpoint.Response.Namespace);

        return list.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    // ---------------------------------------------------------------------
    // request.<GrpcField> -> new MediatorMessage(...)
    // ---------------------------------------------------------------------

    private static IReadOnlyList<Dictionary<string, object?>> BuildRequestMappings(EndpointModel endpoint)
    {
        if (endpoint.Request is null)
            return [];

        var ctor = endpoint.MediatorMessage.PreferredConstructor;
        if (ctor is null || ctor.Parameters.Count == 0)
            return [];

        var requestFields = endpoint.Request?.Fields ?? [];

        // Two distinct lookups — they answer two distinct questions:
        //  - requestLookup: "if a Request *field* is itself a nested proto message, what CLR
        //    contract does it correspond to?" (feeds BuildProtoToClrExpression, unchanged).
        //  - mediatorLookup: "if a MediatorMessage constructor *parameter*'s CLR type is itself
        //    a known, constructible contract (e.g. PermissionListQueryFilter, PagedRequest),
        //    what is its constructor?"
        // PermissionListQueryFilter/PagedRequest live in the MediatorMessage's own dependency
        // graph — flattening only endpoint.Request would never surface them, which was the bug.
        var requestLookup = FlattenByClrType(endpoint.Request);
        var mediatorLookup = FlattenByClrType(endpoint.MediatorMessage);

        var visiting = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        return ctor.Parameters
            .Select(parameter =>
            {
                var (expression, needsReview) = BuildConstructorArgumentExpression(
                    parameter, "request", requestFields, requestLookup, mediatorLookup, visiting);

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

    // ---------------------------------------------------------------------
    // result.<ClrField> -> new GrpcResponse { ... }
    // ---------------------------------------------------------------------

    private static IReadOnlyList<Dictionary<string, object?>> BuildResponseMappings(EndpointModel endpoint, string protoNamespace)
    {
        if (endpoint.Response is null)
            return [];

        var lookup = FlattenByClrType(endpoint.Response);

        return [.. endpoint.Response.Fields
            .Select(field =>
            {
                var expression = BuildClrToProtoExpression(field.Reference, $"result.{field.Name}", lookup, protoNamespace: protoNamespace);
                return new Dictionary<string, object?>
                {
                    ["destination"] = field.Name,
                    ["expression"] = expression,
                    ["is_repeated"] = field.Reference.IsRepeated,
                    ["needs_review"] = expression.Contains("/* TODO", StringComparison.Ordinal)
                };
            })];
    }
}
