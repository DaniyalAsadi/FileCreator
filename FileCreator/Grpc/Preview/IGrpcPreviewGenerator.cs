using FileCreator.Grpc.Generation;
using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FileCreator.Grpc.Preview;

public interface IGrpcPreviewGenerator
{
    IReadOnlyList<GeneratedFile> Generate(ImmutableArray<EndpointModel> endpoints, GrpcGenerationOptions options);
}

public sealed class GrpcPreviewGenerator(IGrpcCodeGenerator codeGenerator) : IGrpcPreviewGenerator
{
    public IReadOnlyList<GeneratedFile> Generate(ImmutableArray<EndpointModel> endpoints, GrpcGenerationOptions options)
    {
        
        var files = new List<GeneratedFile>();

        foreach (var group in endpoints.GroupBy(e => e.ServiceName))
        {
            files.AddRange(codeGenerator.GenerateForServiceGroup(
                group.Key, [.. group]));
            foreach (var endpoint in group.ToList())
            {
                files.AddRange(codeGenerator.GenerateContracts(endpoint, group.Key));
                files.AddRange(codeGenerator.GenerateMapping(endpoint, group.Key));
            }
        }

        files.AddRange(codeGenerator.GenerateDiRegistration(endpoints.Select(e => e.ServiceName).Distinct()));

        return files;
    }
}
