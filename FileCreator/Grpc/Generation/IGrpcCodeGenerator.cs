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
                clientGenerator.Generate(endpoints, $"{baseBffNameSpace}.Grpc.The{serviceName}.Services"))
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
                clientMappingGenerator.Generate(endpoint, $"{baseBffNameSpace}.Grpc.The{serviceName}"))
        ];

        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateContracts(
        EndpointModel endpoint,
        string serviceName)
    {
        var baseBffNameSpace = "Presentation.Bff";
        List<GeneratedFile> files = [];
        if (endpoint.Request is not null)
        {
            files.Add(new(
                context.Paths.BffBasePath,
                Path.Combine("Grpc", $"The{serviceName}", "Contracts", $"{endpoint.Request.Name}.g.cs"),
                contractGenerator.GenerateRequest(endpoint, $"{baseBffNameSpace}.Grpc.The{serviceName}.Contracts")));
        }
        if (endpoint.Response is not null)
        {
            files.Add(new(
                context.Paths.BffBasePath,
                Path.Combine("Grpc", $"The{serviceName}", "Contracts", $"{endpoint.Response.Name}.g.cs"),
                contractGenerator.GenerateResponse(endpoint, $"{baseBffNameSpace}.Grpc.The{serviceName}.Contracts")));
        }
        return files;
    }

    public IReadOnlyList<GeneratedFile> GenerateDiRegistration(
        IEnumerable<string> serviceNames)
    {
        var baseWebNameSpace = $"{context.ProjectName}.Web";
        //var baseBffNameSpace = "Presentation.Bff";
        return
        [
            new GeneratedFile(
                context.Paths.WebBasePath,
                Path.Combine("Grpc", "GrpcServiceRegistration.g.cs"),
                diGenerator.Generate(serviceNames, $"{baseWebNameSpace}.Grpc"))
        ];
    }
}
