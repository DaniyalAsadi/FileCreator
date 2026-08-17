using FileCreator.Services;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.WebRequestMethods;

namespace FileCreator.Grpc.Generation;

public interface IGrpcCodeGenerator
{
    IReadOnlyList<GeneratedFile> GenerateForServiceGroup(string serviceName, IReadOnlyList<EndpointModel> endpoints, string ns);
    IReadOnlyList<GeneratedFile> GenerateMapping(EndpointModel endpoint, string serviceName, string ns);

    IReadOnlyList<GeneratedFile> GenerateDiRegistration(IEnumerable<string> serviceNames, string ns);
    IReadOnlyList<GeneratedFile> GenerateContracts(EndpointModel endpoint, string serviceName, string ns);
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
        IReadOnlyList<EndpointModel> endpoints,
        string ns)
    {
        var protoRelative = Path.Combine("Grpc", "Protos", $"{serviceName}.proto");
        var serviceRelative = Path.Combine("Grpc", "Services", $"{serviceName}GrpcService.cs");

        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath,
                protoRelative,
                protoGenerator.Generate(endpoints, ns)),

            new GeneratedFile(
                context.Paths.WebBasePath, 
                serviceRelative,
                serviceGenerator.Generate(endpoints, ns)),

            new GeneratedFile(
                context.Paths.BffBasePath, 
                serviceRelative,
                clientGenerator.Generate(endpoints, ns))
        ];
    }

    public IReadOnlyList<GeneratedFile> GenerateMapping(
        EndpointModel endpoint,
        string serviceName,
        string ns)
    {
        var relative = Path.Combine("Grpc", "Mappings", $"The{serviceName}", $"{NamingConventions.MappingClassName(endpoint.EndpointClassName)}.cs");
        
        List<GeneratedFile> files =
        [
            new(
                context.Paths.WebBasePath, 
                relative,
                mappingGenerator.Generate(endpoint, ns)),
            new(
                context.Paths.BffBasePath,
                relative,
                clientMappingGenerator.Generate(endpoint, ns))
        ];
        
        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateContracts(
        EndpointModel endpoint,
        string serviceName, 
        string ns)
    {
        var contractRelatives = Path.Combine("Grpc", "Contracts", $"The{serviceName}");
        List<GeneratedFile> files = [];
        if (endpoint.Request is not null)
        {
            files.Add(new(
                context.Paths.BffBasePath, 
                Path.Combine( contractRelatives, $"{endpoint.Request.Name}.g.cs"),
                contractGenerator.GenerateRequest(endpoint, serviceName, ns)));
        }
        if (endpoint.Response is not null)
        {
            files.Add(new(
                context.Paths.BffBasePath,
                Path.Combine(contractRelatives, $"{endpoint.Response.Name}.g.cs"),
                contractGenerator.GenerateResponse(endpoint, serviceName, ns)));
        }
        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateDiRegistration(
        IEnumerable<string> serviceNames,
        string ns)
    {
        var relative = Path.Combine("Grpc", "GrpcServiceRegistration.g.cs");
        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath, 
                relative,
                diGenerator.Generate(serviceNames, ns))
        ];
    }
}
