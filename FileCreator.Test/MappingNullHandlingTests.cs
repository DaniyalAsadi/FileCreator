using FluentAssertions;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
/// Regression tests for null-value handling in <see cref="MappingGenerator"/> (server:
/// gRPC ⇄ mediator) and <see cref="ClientMappingGenerator"/> (client: BFF ⇄ gRPC):
///
///   bug #1 — nullable Guid/DateOnly/decimal must not assign `null` to proto `string`
///            properties (their generated setters throw ArgumentNullException via
///            CheckNotNull): inline fallbacks use string.Empty, top-level fields use
///            statement-level presence guards.
///   bug #2 — reference-type nullability (string?, Details?, Dictionary?) must reach the
///            mapper even though ProtoTypeReference.IsNullable only tracks Nullable&lt;T&gt;.
///   bug #3 — nested proto messages / google.protobuf.Struct may be unset on the wire, so
///            proto→CLR mapping must guard presence instead of dereferencing null.
///   bug #4 — the server-side MapToResponse template must guard nullable CLR collections
///            before AddRange, like the client template already did.
///   gap #5 — nullable string-backed scalars (Guid?/decimal?/DateOnly?) on proto→CLR must
///            map an unset wire field to null instead of throwing FormatException.
///   gap #6 — proto3 `optional` fields expose HasX/ClearX; the mapper must read HasX
///            (unset ⇒ null) and must never write through the property when the CLR value
///            is null (which would set the presence bit on the wire).
/// </summary>
public sealed class MappingNullHandlingTests
{
    private const string ProtoNamespace = "Demo.Web.Grpc.Protos.UserService";
    private const string MappingNamespace = "Presentation.Bff.Grpc.TheUserService.Mappings";
    private const string ContractNamespace = "Presentation.Bff.Grpc.TheUserService.Contracts";

    // ------------------------------------------------------------------
    // server: MappingGenerator (GrpcRequest -> mediator, result -> GrpcResponse)
    // ------------------------------------------------------------------

    [Fact]
    public void Server_MapToQuery_Guards_Unset_Nested_Message_For_Nullable_Parameter()
    {
        var mapping = GenerateServerMapping();

        // request.Filter may legitimately be unset on the wire; the ctor parameter is
        // UserDetails? so presence is honoured with null instead of an NRE.
        mapping.Should().Contain(
            "request.Filter is null ? null : new Demo.Contracts.UserDetails { Email = request.Filter.Email");
    }

    [Fact]
    public void Server_MapToQuery_Reads_Optional_Presence_For_Nullable_Parameters()
    {
        var mapping = GenerateServerMapping();

        // gap #6 — proto emitted `optional int32 page`, so unset ⇒ null (not 0)
        mapping.Should().Contain("request.HasPage ? request.Page : (int?)null");

        // optional string ⇒ unset maps to null
        mapping.Should().Contain("request.HasKeyword ? request.Keyword : null");

        // gap #5 — optional string backing Guid?: unset ⇒ null (not FormatException)
        mapping.Should().Contain(
            "request.HasCorrelationId ? Guid.Parse(request.CorrelationId) : (System.Guid?)null");

        // optional enum ⇒ unset maps to null via HasX
        mapping.Should().Contain(
            "request.HasKind ? (Demo.Contracts.UserKind)request.Kind : (Demo.Contracts.UserKind?)null");
    }

    [Fact]
    public void Server_MapToQuery_Propagates_Presence_Into_Nested_Messages()
    {
        var mapping = GenerateServerMapping();

        mapping.Should().Contain(
            "Rating = request.Filter.HasRating ? decimal.Parse(request.Filter.Rating, System.Globalization.CultureInfo.InvariantCulture) : (decimal?)null");
    }

    [Fact]
    public void Server_MapToResponse_Guards_Optional_Fields_At_Statement_Level()
    {
        var mapping = GenerateServerMapping();

        // gap #6 — assigning even `default` would set the presence bit, so null CLR values
        // must skip the assignment entirely.
        mapping.Should().Contain("if (result.Score is not null)");
        mapping.Should().Contain(
            "grpc.Score = result.Score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);");

        mapping.Should().Contain("if (result.CorrelationId is not null)");
        mapping.Should().Contain("grpc.CorrelationId = result.CorrelationId.Value.ToString();");

        mapping.Should().Contain("if (result.Kind is not null)");
        mapping.Should().Contain(
            "grpc.Kind = (global::Demo.Web.Grpc.Protos.UserService.UserKind)result.Kind.Value;");

        // the old inline forms are gone from the top level
        mapping.Should().NotContain("result.Score is null ? null :");
        mapping.Should().NotContain("Score = result.Score is null ? string.Empty");
    }

    [Fact]
    public void Server_MapToResponse_Timestamp_Backed_Nullable_Stays_Inline()
    {
        var mapping = GenerateServerMapping();

        // Timestamp is a proto message — null IS its presence encoding, so the `? null :`
        // form remains correct here (no HasX accessor exists for it).
        mapping.Should().Contain(
            "CompletedAt = result.CompletedAt is null ? null : Timestamp.FromDateTime(result.CompletedAt.Value.UtcDateTime)");
    }

    [Fact]
    public void Server_MapToResponse_Nullable_Nested_Message_Maps_To_Null_With_Inline_Scalar_Fallback()
    {
        var mapping = GenerateServerMapping();

        // bug #2 — proto message properties accept null ("not set")
        mapping.Should().Contain(
            "Backup = result.Backup is null ? null : new global::Demo.Web.Grpc.Protos.UserService.UserDetails { Email = result.Backup.Email");

        // bug #1 — nested scalar keeps the inline string.Empty collapse (statement-level
        // presence is only available for top-level fields)
        mapping.Should().Contain(
            "Rating = result.Backup.Rating is null ? string.Empty : result.Backup.Rating.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)");
    }

    [Fact]
    public void Server_MapToResponse_Nullable_Struct_Maps_To_Null()
    {
        var mapping = GenerateServerMapping();

        mapping.Should().Contain("Meta = result.Meta is null ? null : result.Meta.ToStruct()");
    }

    [Fact]
    public void Server_MapToResponse_Guards_Nullable_Collection_Before_AddRange()
    {
        var mapping = GenerateServerMapping();

        // bug #4 — nullable list must not be dereferenced
        mapping.Should().Contain("if (result.Labels is not null)");
        mapping.Should().Contain("grpc.Labels.AddRange(");

        // non-nullable list stays unguarded, and neither uses the old `= { expr }` shape
        mapping.Should().Contain("grpc.Aliases.AddRange(");
        mapping.Should().NotContain("if (result.Aliases is not null)");
        mapping.Should().NotContain("Labels = {");
        mapping.Should().NotContain("Aliases = {");
    }

    [Fact]
    public void Server_MapToResponse_Locks_NonNullable_Behaviour()
    {
        var mapping = GenerateServerMapping();

        // Timestamp targets still accept null via plain conversion (no guard needed)
        mapping.Should().Contain("CreatedAt = Timestamp.FromDateTime(result.CreatedAt.UtcDateTime)");

        // non-nullable string stays a passthrough
        mapping.Should().Contain("Name = result.Name");
    }

    // ------------------------------------------------------------------
    // client: ClientMappingGenerator (BFF -> GrpcRequest, GrpcResponse -> BFF)
    // ------------------------------------------------------------------

    [Fact]
    public void Client_MapToGrpc_Guards_Optional_Fields_At_Statement_Level()
    {
        var mapping = GenerateClientMapping();

        mapping.Should().Contain("if (request.MinPrice is not null)");
        mapping.Should().Contain(
            "result.MinPrice = request.MinPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);");

        mapping.Should().Contain("if (request.CorrelationId is not null)");
        mapping.Should().Contain("result.CorrelationId = request.CorrelationId.Value.ToString();");

        mapping.Should().Contain("if (request.Page is not null)");
        mapping.Should().Contain("result.Page = request.Page.Value;");

        mapping.Should().Contain("if (request.Keyword is not null)");
        mapping.Should().Contain("result.Keyword = request.Keyword;");

        mapping.Should().Contain("if (request.Kind is not null)");
        mapping.Should().Contain(
            "result.Kind = (global::Demo.Web.Grpc.Protos.UserService.UserKind)request.Kind.Value;");

        mapping.Should().NotContain("request.Page is null ? default");
        mapping.Should().NotContain("request.MinPrice is null ? string.Empty");
    }

    [Fact]
    public void Client_MapToGrpc_Locks_Repeated_Guard()
    {
        var mapping = GenerateClientMapping();

        mapping.Should().Contain("if (request.Tags is not null)");
        mapping.Should().Contain("result.Tags.AddRange(");
    }

    [Fact]
    public void Client_MapToResponse_Reads_Optional_Presence_For_Nullable_Scalars()
    {
        var mapping = GenerateClientMapping();

        // gaps #5/#6 — unset on the wire maps to null, not 0 / FormatException
        mapping.Should().Contain(
            "Score = response.HasScore ? decimal.Parse(response.Score, System.Globalization.CultureInfo.InvariantCulture) : (decimal?)null");
        mapping.Should().Contain(
            "CorrelationId = response.HasCorrelationId ? Guid.Parse(response.CorrelationId) : (System.Guid?)null");
        mapping.Should().Contain(
            "Kind = response.HasKind ? (global::Presentation.Bff.Grpc.TheUserService.Contracts.UserKind)response.Kind : (global::Presentation.Bff.Grpc.TheUserService.Contracts.UserKind?)null");
    }

    [Fact]
    public void Client_MapToResponse_Locks_Timestamp_Nullable_Guard()
    {
        var mapping = GenerateClientMapping();

        mapping.Should().Contain(
            "CompletedAt = response.CompletedAt is null ? (System.DateTimeOffset?)null : response.CompletedAt.ToDateTimeOffset()");
    }

    [Fact]
    public void Client_MapToResponse_Guards_Unset_Nested_Messages()
    {
        var mapping = GenerateClientMapping();

        // bug #3, nullable target: null propagates
        mapping.Should().Contain(
            "Backup = response.Backup is null ? null : new global::Presentation.Bff.Grpc.TheUserService.Contracts.UserDetails { Email = response.Backup.Email");

        // bug #3, non-nullable target: falls back to an empty instance instead of an NRE
        mapping.Should().Contain(
            "Details = response.Details is null ? new global::Presentation.Bff.Grpc.TheUserService.Contracts.UserDetails() : new global::Presentation.Bff.Grpc.TheUserService.Contracts.UserDetails { Email = response.Details.Email");

        // nested optional scalar: presence accessor addressed through the nested message
        mapping.Should().Contain(
            "Rating = response.Backup.HasRating ? decimal.Parse(response.Backup.Rating, System.Globalization.CultureInfo.InvariantCulture) : (decimal?)null");
    }

    [Fact]
    public void Client_MapToResponse_Guards_Unset_Struct()
    {
        var mapping = GenerateClientMapping();

        mapping.Should().Contain(
            "Meta = response.Meta is null ? null : response.Meta.Fields.ToDictionary(x => x.Key, x => x.Value.ToObject<object?>())");
    }

    [Fact]
    public void Client_MapToResponse_Locks_Repeated_And_Scalar_Behaviour()
    {
        var mapping = GenerateClientMapping();

        // proto RepeatedField is never null on the wire — no guard needed
        mapping.Should().Contain("Labels = response.Labels.ToList()");
        mapping.Should().Contain("Name = response.Name");
        mapping.Should().Contain("CreatedAt = response.CreatedAt.ToDateTimeOffset()");
    }

    // ------------------------------------------------------------------
    // generation helpers
    // ------------------------------------------------------------------

    private static string GenerateServerMapping()
    {
        var endpoint = CreateEndpointModel();

        return new MappingGenerator(new TemplateEngine()).Generate(
            endpoint,
            mapperNameSpace: "Demo.Web.Grpc.UserService.Mappings",
            protoNameSpace: ProtoNamespace);
    }

    private static string GenerateClientMapping()
    {
        var endpoint = CreateEndpointModel();

        return new ClientMappingGenerator(new TemplateEngine()).Generate(
            endpoint,
            mappingNamespace: MappingNamespace,
            protoNamespace: ProtoNamespace,
            contractNamespace: ContractNamespace);
    }

    private static EndpointModel CreateEndpointModel()
    {
        var compilation = CSharpCompilation.Create(
            "NullContracts",
            [CSharpSyntaxTree.ParseText("""
                #nullable enable
                namespace Demo.Contracts;

                public enum UserKind { Unknown = 0, Admin = 1 }

                public sealed record UserDetails
                {
                    public string Email { get; init; } = string.Empty;
                    public decimal? Rating { get; init; }
                }

                public sealed record UserListRequest
                {
                    public int? Page { get; init; }
                    public System.Collections.Generic.List<string> Tags { get; init; } = [];
                    public decimal? MinPrice { get; init; }
                    public System.Guid? CorrelationId { get; init; }
                    public string? Keyword { get; init; }
                    public UserKind? Kind { get; init; }
                    public UserDetails? Filter { get; init; }
                }

                public sealed record UserListResponse
                {
                    public string Name { get; init; } = string.Empty;
                    public System.DateTimeOffset CreatedAt { get; init; }
                    public System.DateTimeOffset? CompletedAt { get; init; }
                    public decimal? Score { get; init; }
                    public System.Guid? CorrelationId { get; init; }
                    public UserKind? Kind { get; init; }
                    public UserDetails Details { get; init; } = new();
                    public UserDetails? Backup { get; init; }
                    public System.Collections.Generic.List<string> Aliases { get; init; } = [];
                    public System.Collections.Generic.List<string>? Labels { get; init; }
                    public System.Collections.Generic.Dictionary<string, object?>? Meta { get; init; }
                }

                public sealed record UserListQuery(int? Page, string? Keyword, UserDetails? Filter, System.Guid? CorrelationId, UserKind? Kind);
                """)],
            BasicReferences(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        ContractInfo Contract(string metadataName, bool withDetailsDependency = false)
        {
            var type = compilation.GetTypeByMetadataName(metadataName)!;
            return new ContractInfo
            {
                ClrType = type,
                Name = type.Name,
                Namespace = type.ContainingNamespace.ToDisplayString(),
                Fields = ProtoTypeMapper.ExtractFields(type),
                Dependencies = withDetailsDependency ? [DetailsContract(compilation)] : [],
                PreferredConstructor = metadataName.EndsWith("UserListQuery", StringComparison.Ordinal)
                    ? CreatePreferredConstructor((INamedTypeSymbol)type)
                    : null
            };
        }

        return new EndpointModel(
            EndpointGroupName: "User",
            EndpointClassName: "UserListEndpoint",
            EndpointNamespace: "Demo.Endpoints",
            ServiceName: "UserService",
            RpcName: "List",
            Request: Contract("Demo.Contracts.UserListRequest", withDetailsDependency: true),
            Response: Contract("Demo.Contracts.UserListResponse", withDetailsDependency: true),
            MediatorMessage: Contract("Demo.Contracts.UserListQuery", withDetailsDependency: true),
            MediatorMessageIsCommand: false,
            Route: new RouteInfo("GET", "/users", "User", true),
            Visibility: EndpointVisibility.External,
            SourceFilePath: "UserListEndpoint.cs");
    }

    private static ContractInfo DetailsContract(Compilation compilation)
    {
        var type = compilation.GetTypeByMetadataName("Demo.Contracts.UserDetails")!;
        return new ContractInfo
        {
            ClrType = type,
            Name = type.Name,
            Namespace = type.ContainingNamespace.ToDisplayString(),
            Fields = ProtoTypeMapper.ExtractFields(type),
            Dependencies = []
        };
    }

    /// <summary>Mirrors <c>EndpointAnalyzer</c>'s constructor extraction for the positional record.</summary>
    private static ConstructorInfo CreatePreferredConstructor(INamedTypeSymbol queryType)
    {
        var ctor = queryType.InstanceConstructors.First(c => c.Parameters.Length == 5);

        return new ConstructorInfo
        {
            Name = ctor.Name,
            IsPublic = true,
            IsParameterless = false,
            IsPreferred = true,
            Parameters = [.. ctor.Parameters.Select(p => new ConstructorParameterInfo
            {
                Name = p.Name,
                TypeName = p.Type.ToDisplayString(),
                Type = p.Type,
                SourceFieldName = p.Name,
                IsOptional = p.IsOptional,
                HasDefaultValue = p.HasExplicitDefaultValue,
                DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null,
                IsNullable = p.NullableAnnotation == NullableAnnotation.Annotated,
                IsParams = p.IsParams,
                RefKind = p.RefKind
            })]
        };
    }

    private static IEnumerable<MetadataReference> BasicReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            typeof(System.Runtime.GCSettings).Assembly
        };

        return assemblies
            .Distinct()
            .Select(a => MetadataReference.CreateFromFile(a.Location));
    }
}
