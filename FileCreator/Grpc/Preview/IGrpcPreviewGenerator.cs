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
        var ns = options.Namespace ?? $"{endpoints[0].EndpointNamespace.Split('.')[0]}.Grpc";
        var files = new List<GeneratedFile>();

        foreach (var group in endpoints.GroupBy(e => e.ServiceName))
        {
            files.AddRange(codeGenerator.GenerateForServiceGroup(
                group.Key, group.ToList(), ns, options.OutputFolder));
        }

        foreach (var group in endpoints.GroupBy(e => e.ServiceName))
        {
            foreach (var endpoint in group.ToList())
                files.Add(codeGenerator.GenerateMapping(endpoint, group.Key, ns, options.OutputFolder));


        }
        files.Add(codeGenerator.GenerateDiRegistration(
            endpoints.Select(e => e.ServiceName).Distinct(), ns, options.OutputFolder));

        return files;
    }
}
