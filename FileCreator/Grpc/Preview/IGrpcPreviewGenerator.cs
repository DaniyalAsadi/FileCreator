using FileCreator.Grpc.Generation;
using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using FileCreator.Core.Generation;

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

        foreach (var group in endpoints
            .GroupBy(e => e.ServiceName)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var orderedEndpoints = group
                .OrderBy(endpoint => endpoint.EndpointClassName, StringComparer.Ordinal)
                .ToList();

            files.AddRange(codeGenerator.GenerateForServiceGroup(
                group.Key, orderedEndpoints));
            foreach (var endpoint in orderedEndpoints)
            {
                files.AddRange(codeGenerator.GenerateContracts(endpoint, group.Key));
                files.AddRange(codeGenerator.GenerateMapping(endpoint, group.Key));
            }
        }

        files.AddRange(codeGenerator.GenerateDiRegistration(endpoints.Select(e => e.ServiceName).Distinct()));

        return files
            .OrderBy(file => file.AbsolutePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
