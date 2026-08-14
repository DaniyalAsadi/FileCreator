using FileCreator.Services;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.Generation;

public interface IGrpcCodeGenerator
{
    IReadOnlyList<GeneratedFile> GenerateForServiceGroup(string serviceName, IReadOnlyList<EndpointModel> endpoints, string ns);
    IReadOnlyList<GeneratedFile> GenerateMapping(EndpointModel endpoint, string serviceName, string ns);
    IReadOnlyList<GeneratedFile> GenerateDiRegistration(IEnumerable<string> serviceNames, string ns);
}

public sealed class GrpcCodeGenerator(
    ProtoGenerator protoGenerator,
    GrpcServiceGenerator serviceGenerator,
    GrpcClientGenerator clientGenerator,
    MappingGenerator mappingGenerator,
    DiRegistrationGenerator diGenerator,
    GenerationContext context) : IGrpcCodeGenerator
{
    public IReadOnlyList<GeneratedFile> GenerateForServiceGroup(
        string serviceName,
        IReadOnlyList<EndpointModel> endpoints,
        string ns)
    {
        var protoRelative = Path.Combine("Grpc", "Protos", $"{serviceName}.proto");
        var serviceRelative = Path.Combine("Grpc", "Services", $"{serviceName}GrpcService.cs");

        return
        [
            new GeneratedFile(Path.Combine(context.Paths.WebBasePath, protoRelative),
                protoGenerator.Generate(endpoints, ns)),

            new GeneratedFile(Path.Combine(context.Paths.WebBasePath, serviceRelative),
                serviceGenerator.Generate(endpoints, ns)),

            new GeneratedFile(Path.Combine(context.Paths.BffBasePath,serviceRelative),
                clientGenerator.Generate(endpoints, ns))
        ];
    }

    public IReadOnlyList<GeneratedFile> GenerateMapping(
        EndpointModel endpoint,
        string serviceName,
        string ns)
    {
        var relative = Path.Combine("Grpc", "Mappings", $"The{serviceName}", $"{NamingConventions.MappingClassName(endpoint.EndpointClassName)}.cs");
        return
        [
            new GeneratedFile(
                Path.Combine(context.Paths.WebBasePath, relative),
                mappingGenerator.Generate(endpoint, ns))
        ];
    }

    public IReadOnlyList<GeneratedFile> GenerateDiRegistration(
        IEnumerable<string> serviceNames,
        string ns)
    {
        var relative = Path.Combine("Grpc", "GrpcServiceRegistration.g.cs");
        return
        [
            new GeneratedFile(
                Path.Combine(context.Paths.WebBasePath, relative),
                diGenerator.Generate(serviceNames, ns))
        ];
    }
}
