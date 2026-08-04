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

public sealed class GrpcFileWriter : IGrpcFileWriter
{
    public IReadOnlyList<WriteResult> Write(IReadOnlyList<GeneratedFile> files, GrpcGenerationOptions options)
    {
        var plans = files.Select(f => new WritePlan(
            RelativePath: Path.GetRelativePath(options.OutputFolder, f.Path),
            AbsolutePath: f.Path,
            Content: f.Content,
            Mode: f.Path.Contains(Path.Combine("Grpc", "Mappings"))
                ? WriteMode.RegionMerge
                : WriteMode.FullOverwrite)).ToList();

        var writer = new IdempotentFileWriter(options.OutputFolder, options.Force, options.Strict, options.DryRun);
        return writer.Apply(plans);
    }
}
