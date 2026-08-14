using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.Generation;

public interface IGrpcCodeGenerator
{
    IReadOnlyList<GeneratedFile> GenerateForServiceGroup(string serviceName, IReadOnlyList<EndpointModel> endpoints, string ns, string outputRoot);
    GeneratedFile GenerateMapping(EndpointModel endpoint, string serviceName, string ns, string outputRoot);
    GeneratedFile GenerateDiRegistration(IEnumerable<string> serviceNames, string ns, string outputRoot);
}

public sealed class GrpcCodeGenerator(
    ProtoGenerator protoGenerator,
    GrpcServiceGenerator serviceGenerator,
    MappingGenerator mappingGenerator,
    DiRegistrationGenerator diGenerator) : IGrpcCodeGenerator
{
    public IReadOnlyList<GeneratedFile> GenerateForServiceGroup(
        string serviceName, IReadOnlyList<EndpointModel> endpoints, string ns, string outputRoot)
    {
        var protoRelative = Path.Combine("Grpc", "Protos", $"{serviceName}.proto");
        var serviceRelative = Path.Combine("Grpc", "Services", $"{serviceName}GrpcService.cs");

        return
        [
            new GeneratedFile(Path.Combine(outputRoot, protoRelative), protoGenerator.Generate(endpoints, ns)),
            new GeneratedFile(Path.Combine(outputRoot, serviceRelative), serviceGenerator.Generate(endpoints, ns))
        ];
    }

    public GeneratedFile GenerateMapping(EndpointModel endpoint, string serviceName, string ns, string outputRoot)
    {
        var relative = Path.Combine("Grpc", "Mappings",$"The{serviceName}", $"{NamingConventions.MappingClassName(endpoint.EndpointClassName)}.cs");
        return new GeneratedFile(Path.Combine(outputRoot, relative), mappingGenerator.Generate(endpoint, ns));
    }

    public GeneratedFile GenerateDiRegistration(IEnumerable<string> serviceNames, string ns, string outputRoot)
    {
        var relative = Path.Combine("Grpc", "GrpcServiceRegistration.g.cs");
        return new GeneratedFile(Path.Combine(outputRoot, relative), diGenerator.Generate(serviceNames, ns));
    }
}
