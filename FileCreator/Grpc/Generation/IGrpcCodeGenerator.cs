using FileCreator.Services;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.WebRequestMethods;

namespace FileCreator.Grpc.Generation;

public interface IGrpcCodeGenerator
{
    IReadOnlyList<GeneratedFile> GenerateForServiceGroup(string serviceName, IReadOnlyList<EndpointModel> endpoints);
    IReadOnlyList<GeneratedFile> GenerateMapping(EndpointModel endpoint, string serviceName);

    IReadOnlyList<GeneratedFile> GenerateDiRegistration(IEnumerable<string> serviceNames);
    IReadOnlyList<GeneratedFile> GenerateContracts(EndpointModel endpoint, string serviceName);
}

public sealed class GrpcCodeGenerator(
    ProtoGenerator protoGenerator,
    GrpcServiceGenerator serviceGenerator,
    GrpcClientGenerator clientGenerator,
    ContractGenerator contractGenerator,
    MappingGenerator mappingGenerator,
    DiRegistrationGenerator diGenerator,
    ClientMappingGenerator clientMappingGenerator,
    GenerationContext context) : IGrpcCodeGenerator
{
    public IReadOnlyList<GeneratedFile> GenerateForServiceGroup(
        string serviceName,
        IReadOnlyList<EndpointModel> endpoints)
    {
        var baseWebNameSpace = $"{context.ProjectName}.Web";
        var baseBffNameSpace = "Presentation.Bff";
        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath,
                Path.Combine("Grpc", "Protos", $"{serviceName}.proto"),
                protoGenerator.Generate(endpoints, $"{baseWebNameSpace}.Grpc.Protos.{serviceName}")),

            new GeneratedFile(
                context.Paths.WebBasePath,
                Path.Combine("Grpc","Services",$"{serviceName}GrpcService.cs"),
                Content: serviceGenerator.Generate(
                    endpoints,
                    serviceNameSpace: $"{baseWebNameSpace}.Grpc.Services",
                    protoNameSpace: $"{baseWebNameSpace}.Grpc.Protos.{serviceName}",
                    mapperNameSpace: $"{baseWebNameSpace}.Grpc.Mappings.The{serviceName}")),

            new GeneratedFile(
                context.Paths.BffBasePath,
                Path.Combine("Grpc",$"The{serviceName}","Services",$"{serviceName}GrpcService.cs"),
                clientGenerator.Generate(
                    endpoints,
                    clientNamespace: $"{baseBffNameSpace}.Grpc.The{serviceName}.Services",
                    protoNamespace: $"{baseWebNameSpace}.Grpc.Protos.{serviceName}",
                    mappingNamespace: $"{baseBffNameSpace}.Grpc.The{serviceName}.Mappings",
                    contractNamespace: $"{baseBffNameSpace}.Grpc.The{serviceName}.Contracts"))
        ];
    }

    public IReadOnlyList<GeneratedFile> GenerateMapping(
        EndpointModel endpoint,
        string serviceName)
    {

        var baseWebNameSpace = $"{context.ProjectName}.Web";
        var baseBffNameSpace = "Presentation.Bff";
        List<GeneratedFile> files =
        [
            new(
                context.Paths.WebBasePath,
                Path.Combine("Grpc", "Mappings",$"The{serviceName}", $"{NamingConventions.MappingClassName(endpoint.EndpointClassName)}.cs"),
                mappingGenerator.Generate(endpoint,
                mapperNameSpace: $"{baseWebNameSpace}.Grpc.Mappings.The{serviceName}",
                protoNameSpace: $"{baseWebNameSpace}.Grpc.Protos.{serviceName}")),
            new(
                context.Paths.BffBasePath,
                Path.Combine("Grpc", $"The{serviceName}", "Mappings", $"{NamingConventions.MappingClassName(endpoint.EndpointClassName)}.cs"),
                clientMappingGenerator.Generate(
                    endpoint,
                    mappingNamespace: $"{baseBffNameSpace}.Grpc.The{serviceName}.Mappings",
                    protoNamespace: $"{baseWebNameSpace}.Grpc.Protos.{serviceName}",
                    contractNamespace: $"{baseBffNameSpace}.Grpc.The{serviceName}.Contracts"))
        ];

        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateContracts(
        EndpointModel endpoint,
        string serviceName)
    {
        var baseBffNameSpace = "Presentation.Bff";
        var contractNamespace = $"{baseBffNameSpace}.Grpc.The{serviceName}.Contracts";
        var files = new List<GeneratedFile>();

        foreach (var generated in contractGenerator.GenerateContracts(endpoint.Request, contractNamespace))
        {
            files.Add(new(
                context.Paths.BffBasePath,
                Path.Combine("Grpc", $"The{serviceName}", "Contracts", generated.FileName),
                generated.Content));
        }

        foreach (var generated in contractGenerator.GenerateContracts(endpoint.Response, contractNamespace))
        {
            files.Add(new(
                context.Paths.BffBasePath,
                Path.Combine("Grpc", $"The{serviceName}", "Contracts", generated.FileName),
                generated.Content));
        }

        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateDiRegistration(
        IEnumerable<string> serviceNames)
    {
        var baseWebNameSpace = $"{context.ProjectName}.Web";
        var baseBffNameSpace = "Presentation.Bff";
        var services = serviceNames.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath,
                Path.Combine("Grpc", "GrpcServiceRegistration.g.cs"),
                diGenerator.Generate(services, $"{baseWebNameSpace}.Grpc")),

            new GeneratedFile(
                context.Paths.BffBasePath,
                Path.Combine("Grpc", "GrpcClientRegistration.g.cs"),
                diGenerator.GenerateClient(
                    services.Select(service => new GrpcClientRegistrationDescriptor(
                        ServiceName: service,
                        ProtoNamespace: $"{baseWebNameSpace}.Grpc.Protos.{service}",
                        ClientNamespace: $"{baseBffNameSpace}.Grpc.The{service}.Services",
                        ClientClassName: $"{service}GrpcClient")),
                    $"{baseBffNameSpace}.Grpc"))
        ];
    }
}
