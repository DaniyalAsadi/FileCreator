using FluentAssertions;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
/// Regression tests for the nullable-Dictionary fix in the gRPC scaffold.
///
/// Bug: a C# contract containing <c>Dictionary&lt;string, string?&gt;</c> produced invalid
/// protobuf — <c>map&lt;string, string?&gt;</c> — because the map value type was rendered via
/// the raw CLR symbol's <c>ToDisplayString()</c>, leaking C# nullability into the .proto.
///
/// Fix: <see cref="ProtoTypeMapper.Map"/> now routes the map key/value through the same
/// CLR→proto pipeline a field uses, so the value gets a proper proto type name. A nullable
/// map value (whose null/presence semantic proto maps cannot otherwise express) is wrapped in
/// a generated <c>Nullable&lt;T&gt;</c> message whose presence encodes the null, and both
/// mapping directions recurse into the wrapped value reference.
///
/// These tests cover the minimum required cases (<c>Dictionary&lt;string, string&gt;</c> and
/// <c>Dictionary&lt;string, string?&gt;</c>) plus the other nullable scalar/enum/wrapper
/// shapes, and assert the generated .proto is syntactically valid (no stray <c>?</c>) and that
/// both sides of the C# mapping compile-shaped code correctly.
/// </summary>
public sealed class MapNullableValueTests
{
    private const string ProtoNamespace = "Demo.Web.Grpc.Protos.MapService";
    private const string MappingNamespace = "Demo.Web.Grpc.MapService.Mappings";
    private const string ContractNamespace = "Bff.Grpc.Map.Contracts";

    [Fact]
    public void Proto_Never_Contains_Question_Mark()
    {
        var proto = GenerateProto();
        // A `?` anywhere would be leaked C# nullability — invalid protobuf.
        proto.Should().NotContain("?");
    }

    [Fact]
    public void Proto_NonNullable_String_Map_Is_Plain()
    {
        var proto = GenerateProto();
        // Requirement #2: Dictionary<string, string> -> map<string, string> (no wrapper).
        proto.Should().Contain("map<string, string> tags");
    }

    [Fact]
    public void Proto_Nullable_String_Map_Is_Wrapped()
    {
        var proto = GenerateProto();
        // Requirement #3: Dictionary<string, string?> must NOT silently collapse to
        // `map<string, string>`; it is wrapped in a generated message.
        proto.Should().Contain("map<string, NullableString> metadata");
        proto.Should().Contain("message NullableString {");
        proto.Should().Contain(" string value = 1;");
    }

    [Fact]
    public void Proto_Generates_One_Wrapper_Per_Distinct_Value_Type()
    {
        var proto = GenerateProto();
        // Guid? also maps to proto `string`, so it shares the NullableString wrapper.
        proto.Should().Contain("map<string, NullableString> ids");
        // int? -> NullableInt32, enum? -> NullableUserKind.
        proto.Should().Contain("map<string, NullableInt32> scores");
        proto.Should().Contain("map<string, NullableUserKind> kinds");
        proto.Should().Contain("message NullableInt32 {");
        proto.Should().Contain("message NullableUserKind {");
    }

    [Fact]
    public void Proto_Message_Backed_Nullables_Are_Not_Wrapped()
    {
        var proto = GenerateProto();
        // DateTime? maps to google.protobuf.Timestamp (a message) which already preserves
        // presence, so it must NOT be wrapped — the null semantic survives natively.
        proto.Should().Contain("map<string, google.protobuf.Timestamp> times");
        // Non-nullable DateTime likewise stays a Timestamp, with conversion applied.
        proto.Should().Contain("map<string, google.protobuf.Timestamp> non_null_times");
    }

    [Fact]
    public void Proto_Emits_Enum_Used_As_Wrapped_Map_Value()
    {
        var proto = GenerateProto();
        proto.Should().Contain("enum UserKind {");
    }

    [Fact]
    public void Server_MapToResponse_Wraps_Nullable_Values_ClrToProto()
    {
        var mapping = GenerateServerMapping();
        // Non-nullable string map keeps the simple ToMapField().
        mapping.Should().Contain("result.Tags.ToMapField()");
        // Nullable string map -> filtered projection into the wrapper message.
        mapping.Should().Contain("result.Metadata.Where(kvp => kvp.Value is not null).ToMapField(");
        mapping.Should().Contain("NullableString { Value = kvp.Value }");
        // int? -> wrapper with the inner scalar conversion.
        mapping.Should().Contain("NullableInt32 { Value = kvp.Value.Value }");
        // Guid? -> wrapper with .ToString() on the inner value.
        mapping.Should().Contain("NullableString { Value = kvp.Value.Value.ToString() }");
        // enum? -> wrapper with the inner enum cast.
        mapping.Should().Contain("NullableUserKind { Value = kvp.Value is null ? default : (Demo.Contracts.UserKind)kvp.Value.Value }");
        // DateTime (non-nullable) -> Timestamp conversion applied through the map.
        mapping.Should().Contain("Timestamp.FromDateTime(kvp.Value.ToUniversalTime())");
    }

    [Fact]
    public void Client_MapToGrpc_Wraps_Nullable_Values_ClrToProto()
    {
        var mapping = GenerateClientMapping();
        mapping.Should().Contain("request.Tags.ToMapField()");
        mapping.Should().Contain("request.Metadata.Where(kvp => kvp.Value is not null).ToMapField(");
        mapping.Should().Contain("NullableString { Value = kvp.Value }");
        mapping.Should().Contain("NullableInt32 { Value = kvp.Value.Value }");
    }

    [Fact]
    public void Client_MapToResponse_Unwraps_Nullable_Values_ProtoToClr()
    {
        var mapping = GenerateClientMapping();
        // Non-nullable string map: trivial passthrough value.
        mapping.Should().Contain("response.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)");
        // Nullable string map: read through the wrapper's `value` field.
        mapping.Should().Contain("response.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value)");
        // Guid? -> Guid.Parse on the unwrapped value.
        mapping.Should().Contain("Guid.Parse(kvp.Value.Value)");
        // enum? -> cast on the unwrapped value.
        mapping.Should().Contain("(Demo.Contracts.UserKind)kvp.Value.Value");
        // DateTime -> ToDateTime on the Timestamp value.
        mapping.Should().Contain("kvp.Value.ToDateTime()");
    }

    // ------------------------------------------------------------------
    // generation helpers
    // ------------------------------------------------------------------

    private static string GenerateProto()
    {
        var endpoint = CreateEndpointModel();
        return new ProtoGenerator(new TemplateEngine()).Generate(
            [endpoint], ProtoNamespace);
    }

    private static string GenerateServerMapping()
    {
        var endpoint = CreateEndpointModel();
        return new MappingGenerator(new TemplateEngine()).Generate(
            endpoint,
            mapperNameSpace: MappingNamespace,
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
            "MapContracts",
            [CSharpSyntaxTree.ParseText("""
                #nullable enable
                namespace Demo.Contracts;

                public enum UserKind { Unknown = 0, Admin = 1 }

                public sealed record MapRequest
                {
                    public System.Collections.Generic.Dictionary<string, string> Tags { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, string?> Metadata { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, int?> Scores { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, System.Guid?> Ids { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, System.DateTime?> Times { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, UserKind?> Kinds { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, System.DateTime> NonNullTimes { get; init; } = new();
                }

                public sealed record MapResponse
                {
                    public System.Collections.Generic.Dictionary<string, string> Tags { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, string?> Metadata { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, int?> Scores { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, System.Guid?> Ids { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, UserKind?> Kinds { get; init; } = new();
                    public System.Collections.Generic.Dictionary<string, System.DateTime> NonNullTimes { get; init; } = new();
                }
                """)],
            BasicReferences(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        ContractInfo Contract(string metadataName)
        {
            var type = compilation.GetTypeByMetadataName(metadataName)!;
            return new ContractInfo
            {
                ClrType = type,
                Name = type.Name,
                Namespace = type.ContainingNamespace.ToDisplayString(),
                Fields = ProtoTypeMapper.ExtractFields(type),
                Dependencies = []
            };
        }

        var request = Contract("Demo.Contracts.MapRequest");
        var response = Contract("Demo.Contracts.MapResponse");

        return new EndpointModel(
            EndpointGroupName: "Map",
            EndpointClassName: "MapEndpoint",
            EndpointNamespace: "Demo.Endpoints",
            ServiceName: "MapService",
            RpcName: "List",
            Request: request,
            Response: response,
            // ProtoGenerator does not consult the mediator message; a placeholder keeps the
            // record well-formed. (Server MapToQuery is exercised via the client generator,
            // which maps proto responses without a mediator constructor.)
            MediatorMessage: request,
            MediatorMessageIsCommand: false,
            Route: new RouteInfo("GET", "/map", "Map", true),
            Visibility: EndpointVisibility.External,
            SourceFilePath: "MapEndpoint.cs");
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
