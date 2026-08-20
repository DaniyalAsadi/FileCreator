using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.Writers;

public interface IGrpcFileWriter
{
    IReadOnlyList<WriteResult> Write(IReadOnlyList<GeneratedFile> files, GrpcGenerationOptions options);
}

public sealed class GrpcFileWriter(GenerationContext context) : IGrpcFileWriter
{
    public IReadOnlyList<WriteResult> Write(
        IReadOnlyList<GeneratedFile> files,
        GrpcGenerationOptions options)
    {
        var results = new List<WriteResult>();

        foreach (var group in files.GroupBy(f => f.BasePath, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(group.Key))
                continue;

            var plans = group.Select(f => new WritePlan(
                RelativePath: f.RelativePath,
                AbsolutePath: f.AbsolutePath,
                Content: f.Content,
                Mode: f.AbsolutePath.Contains(Path.Combine("Grpc", "Mappings"))
                    ? WriteMode.RegionMerge
                    : WriteMode.FullOverwrite)).ToList();

            var writer = new IdempotentFileWriter(
                group.Key,
                options.Force,
                options.Strict,
                options.DryRun);

            results.AddRange(writer.Apply(plans));
        }

        return results;
    }
}
