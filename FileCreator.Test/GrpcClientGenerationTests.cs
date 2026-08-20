using FluentAssertions;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public sealed class GrpcClientGenerationTests
{
    [Fact]
    public void Basic_Client_Generation_Produces_Client_Mapping_And_Di()
    {
        var endpoint = CreateEndpointModel();
        var templates = new TemplateEngine();

        var client = new GrpcClientGenerator(templates).Generate(
            [endpoint],
            clientNamespace: "Presentation.Bff.Grpc.TheUserService.Services",
            protoNamespace: "Demo.Web.Grpc.Protos.UserService",
            mappingNamespace: "Presentation.Bff.Grpc.TheUserService.Mappings",
            contractNamespace: "Presentation.Bff.Grpc.TheUserService.Contracts");

        var mapping = new ClientMappingGenerator(templates).Generate(
            endpoint,
            mappingNamespace: "Presentation.Bff.Grpc.TheUserService.Mappings",
            protoNamespace: "Demo.Web.Grpc.Protos.UserService",
            contractNamespace: "Presentation.Bff.Grpc.TheUserService.Contracts");

        var di = new DiRegistrationGenerator(templates).GenerateClient(
            [new GrpcClientRegistrationDescriptor(
                "UserService",
                "Demo.Web.Grpc.Protos.UserService",
                "Presentation.Bff.Grpc.TheUserService.Services",
                "UserServiceGrpcClient")],
            "Presentation.Bff.Grpc");

        client.Should().Contain("Demo.Web.Grpc.Protos.UserService.UserService.UserServiceClient");
        client.Should().Contain("CancellationToken cancellationToken = default");
        client.Should().Contain("Presentation.Bff.Grpc.TheUserService.Mappings.UserListMapping.MapToGrpc(request)");

        mapping.Should().Contain("using GrpcRequest = Demo.Web.Grpc.Protos.UserService.UserListRequest;");
        mapping.Should().Contain("using GrpcResponse = Demo.Web.Grpc.Protos.UserService.UserListResponse;");
        mapping.Should().Contain("Name = response.Name");
        mapping.Should().Contain("result.Tags.AddRange(");

        di.Should().Contain("AddGrpcClient<Demo.Web.Grpc.Protos.UserService.UserService.UserServiceClient>");
        di.Should().Contain("Grpc:Services:UserService:Address");
        di.Should().NotContain("localhost");
    }

    [Fact]
    public void Generation_Is_Deterministic()
    {
        var endpoint = CreateEndpointModel();
        var generator = new ClientMappingGenerator(new TemplateEngine());

        var first = generator.Generate(endpoint, "Bff.Grpc.User.Mappings", "Web.Grpc.Protos.User", "Bff.Grpc.User.Contracts");
        var second = generator.Generate(endpoint, "Bff.Grpc.User.Mappings", "Web.Grpc.Protos.User", "Bff.Grpc.User.Contracts");

        second.Should().Be(first);
    }

    [Fact]
    public void Contract_Generation_Includes_Nested_Dependencies()
    {
        var endpoint = CreateEndpointModel();
        var generator = new ContractGenerator();

        var contracts = generator.GenerateContracts(endpoint.Response, "Bff.Contracts");

        contracts.Select(c => c.FileName).Should().Contain(["UserListResponse.g.cs", "UserDetails.g.cs"]);
    }

    private static EndpointModel CreateEndpointModel()
    {
        var compilation = CSharpCompilation.Create(
            "TestContracts",
            [CSharpSyntaxTree.ParseText("""
                namespace Demo.Contracts;
                public enum UserKind { Unknown = 0, Admin = 1 }
                public sealed record UserDetails
                {
                    public string Email { get; init; } = string.Empty;
                }
                public sealed record UserListRequest
                {
                    public int? Page { get; init; }
                    public System.Collections.Generic.List<string> Tags { get; init; } = [];
                    public UserKind Kind { get; init; }
                }
                public sealed record UserListResponse
                {
                    public string Name { get; init; } = string.Empty;
                    public System.DateTimeOffset CreatedAt { get; init; }
                    public UserDetails Details { get; init; } = new();
                }
                public sealed record UserListQuery(int? Page, System.Collections.Generic.List<string> Tags, UserKind Kind);
                """)],
            BasicReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ContractInfo Contract(string metadataName)
        {
            var type = compilation.GetTypeByMetadataName(metadataName)!;
            return new ContractInfo
            {
                ClrType = type,
                Name = type.Name,
                Namespace = type.ContainingNamespace.ToDisplayString(),
                Fields = ProtoTypeMapper.ExtractFields(type),
                Dependencies = type.Name == "UserListResponse"
                    ? [new ContractInfo
                    {
                        ClrType = compilation.GetTypeByMetadataName("Demo.Contracts.UserDetails")!,
                        Name = "UserDetails",
                        Namespace = "Demo.Contracts",
                        Fields = ProtoTypeMapper.ExtractFields(compilation.GetTypeByMetadataName("Demo.Contracts.UserDetails")!)
                    }]
                    : []
            };
        }

        return new EndpointModel(
            EndpointGroupName: "User",
            EndpointClassName: "UserListEndpoint",
            EndpointNamespace: "Demo.Endpoints",
            ServiceName: "UserService",
            RpcName: "List",
            Request: Contract("Demo.Contracts.UserListRequest"),
            Response: Contract("Demo.Contracts.UserListResponse"),
            MediatorMessage: Contract("Demo.Contracts.UserListQuery"),
            MediatorMessageIsCommand: false,
            Route: new RouteInfo("GET", "/users", "User", true),
            Visibility: EndpointVisibility.External,
            SourceFilePath: "UserListEndpoint.cs");
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
