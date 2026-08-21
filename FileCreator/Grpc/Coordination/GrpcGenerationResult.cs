using FileCreator.Grpc.Discovery;
using FileCreator.Grpc.Preview;
using FileCreator.Grpc.ViewModels;
using FileCreator.Grpc.Writers;
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.IO;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace FileCreator.Grpc.Coordination;

public sealed record GrpcGenerationResult(
    ImmutableArray<EndpointModel> Endpoints,
    IReadOnlyList<GeneratedFile> Files,
    GrpcGenerationOptions Options);

public sealed class GrpcGenerationCoordinator(
    IEndpointDiscoveryService discovery,
    IGrpcPreviewGenerator previewGenerator,
    IGrpcFileWriter writer,
    GenerationContext context)
{
    public async Task<GrpcGenerationResult> PrepareAsync(GrpcGenerationOptions options)
    {
        var validationErrors = options.Validate();
        if (validationErrors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));


        ImmutableArray<EndpointModel> endpoints = await discovery.DiscoverAsync(options);

        if (endpoints.IsEmpty)
            throw new InvalidOperationException("هیچ Endpoint منطبقی پیدا نشد.");

        if (options.SelectedEndpoints.Count > 0)
        {
            endpoints = [.. endpoints
                .Where(x =>
                    options.SelectedEndpoints
                    .Contains(x.EndpointClassName))];
        }

        IReadOnlyList<GeneratedFile> files = previewGenerator.Generate(endpoints, options);

        return new GrpcGenerationResult(endpoints, files, options);
    }

    public IReadOnlyList<WriteResult> Commit(GrpcGenerationResult result)
    {
        var csprojUpdater = new CsprojUpdater();
        var csprojPath = Path.Combine(
            context.Paths.WebBasePath, 
            $"{result.Options.ProjectName}.Web.csproj");
        var writeResult = writer.Write(result.Files, result.Options);
        csprojUpdater.EnsureProtoInclude(
            csprojPath,
            Path.Combine("Grpc", "Protos"));

        var bffCsprojPath = Directory.Exists(context.Paths.BffBasePath)
            ? Directory.GetFiles(context.Paths.BffBasePath, "*.csproj").FirstOrDefault()
            : null;

        if (!string.IsNullOrWhiteSpace(bffCsprojPath))
        {
            var relativeProtoPath = Path.GetRelativePath(
                context.Paths.BffBasePath,
                Path.Combine(context.Paths.WebBasePath, "Grpc", "Protos"));

            csprojUpdater.EnsureProtoInclude(
                bffCsprojPath,
                relativeProtoPath,
                grpcServices: "Client");
        }

        return writeResult;

    }
}
