using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
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
        var plans = files.Select(f => new WritePlan(
            RelativePath: f.RelativePath,
            AbsolutePath: f.AbsolutePath,
            Content: f.Content,
            Mode: f.AbsolutePath.Contains(Path.Combine("Grpc", "Mappings"))
                ? WriteMode.RegionMerge
                : WriteMode.FullOverwrite)).ToList();

        var writer = new IdempotentFileWriter(
            context.Paths.WebBasePath, 
            options.Force, 
            options.Strict, 
            options.DryRun);
        return writer.Apply(plans);
    }
}
