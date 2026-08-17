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
        var protoRelative = Path.Combine("Grpc", "Protos", $"{serviceName}.proto");
        var serviceRelative = Path.Combine("Grpc", "Services", $"{serviceName}GrpcService.cs");
        var baseWebNameSpace = $"{context.ProjectName}.Web";
        var baseBffNameSpace = "Presentation.Bff";
        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath,
                protoRelative,
                protoGenerator.Generate(endpoints, $"{baseWebNameSpace}.Grpc.Protos")),

            new GeneratedFile(
                context.Paths.WebBasePath, 
                serviceRelative,
                serviceGenerator.Generate(endpoints, $"{baseWebNameSpace}.Grpc.Services")),

            new GeneratedFile(
                context.Paths.BffBasePath, 
                serviceRelative,
                clientGenerator.Generate(endpoints, $"{baseBffNameSpace}.Grpc.Services"))
        ];
    }

    public IReadOnlyList<GeneratedFile> GenerateMapping(
        EndpointModel endpoint,
        string serviceName)
    {
        var relative = Path.Combine("Grpc", "Mappings", $"The{serviceName}", $"{NamingConventions.MappingClassName(endpoint.EndpointClassName)}.cs");

        var baseWebNameSpace = $"{context.ProjectName}.Web";
        var baseBffNameSpace = "Presentation.Bff";
        List<GeneratedFile> files =
        [
            new(
                context.Paths.WebBasePath, 
                relative,
                mappingGenerator.Generate(endpoint, $"{baseWebNameSpace}.Grpc.Mappings.$The{serviceName}")),
            new(
                context.Paths.BffBasePath,
                relative,
                clientMappingGenerator.Generate(endpoint, $"{baseBffNameSpace}.Grpc.Mappings.$The{serviceName}"))
        ];
        
        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateContracts(
        EndpointModel endpoint,
        string serviceName)
    {
        var contractRelatives = Path.Combine("Grpc", "Contracts", $"The{serviceName}");
        var baseBffNameSpace = "Presentation.Bff";
        List<GeneratedFile> files = [];
        if (endpoint.Request is not null)
        {
            files.Add(new(
                context.Paths.BffBasePath, 
                Path.Combine( contractRelatives, $"{endpoint.Request.Name}.g.cs"),
                contractGenerator.GenerateRequest(endpoint, serviceName, $"{baseBffNameSpace}.Grpc.Contracts.$The{serviceName}")));
        }
        if (endpoint.Response is not null)
        {
            files.Add(new(
                context.Paths.BffBasePath,
                Path.Combine(contractRelatives, $"{endpoint.Response.Name}.g.cs"),
                contractGenerator.GenerateResponse(endpoint, serviceName, $"{baseBffNameSpace}.Grpc.Contracts.$The{serviceName}")));
        }
        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateDiRegistration(
        IEnumerable<string> serviceNames)
    {
        var relative = Path.Combine("Grpc", "GrpcServiceRegistration.g.cs");
        var baseWebNameSpace = $"{context.ProjectName}.Web";
        //var baseBffNameSpace = "Presentation.Bff";
        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath, 
                relative,
                diGenerator.Generate(serviceNames, $"{baseWebNameSpace}.Grpc"))
        ];
    }
}
