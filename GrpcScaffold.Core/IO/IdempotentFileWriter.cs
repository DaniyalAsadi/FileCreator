// src/GrpcScaffold.Core/IO/IdempotentFileWriter.cs
using System.Security.Cryptography;
using System.Text;

namespace GrpcScaffold.Core.IO;

public enum WriteMode { FullOverwrite, RegionMerge }

public sealed record WritePlan(string RelativePath, string AbsolutePath, string Content, WriteMode Mode);

public sealed record WriteResult(string RelativePath, bool Written, bool WasUnchanged, bool ManualEditDetected);

public sealed class IdempotentFileWriter(string outputRoot, bool force, bool strict, bool dryRun)
{
    private readonly string _manifestPath = Path.Combine(outputRoot, ".grpc-scaffold", "manifest.json");
    private readonly GenerationManifest _manifest = GenerationManifest.Load(
        Path.Combine(outputRoot, ".grpc-scaffold", "manifest.json"));

    public IReadOnlyList<WriteResult> Apply(IEnumerable<WritePlan> plans)
    {
        var results = new List<WriteResult>();

        foreach (var plan in plans)
        {
            var existing = File.Exists(plan.AbsolutePath) ? File.ReadAllText(plan.AbsolutePath) : null;
            var knownHash = _manifest.FileHashes.GetValueOrDefault(plan.RelativePath);
            var manualEditDetected = existing is not null && knownHash is not null &&
                                      Hash(existing) != knownHash;

            string finalContent = plan.Mode switch
            {
                WriteMode.FullOverwrite => plan.Content,
                WriteMode.RegionMerge when existing is null => plan.Content,
                WriteMode.RegionMerge when manualEditDetected && !force => existing!, // preserve, don't touch
                WriteMode.RegionMerge => RegionMerger.Merge(existing!, plan.Content),
                _ => plan.Content
            };

            var unchanged = existing == finalContent;

            if (manualEditDetected && strict && !force)
            {
                results.Add(new WriteResult(plan.RelativePath, Written: false, unchanged, ManualEditDetected: true));
                continue;
            }

            if (!unchanged && !dryRun)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(plan.AbsolutePath)!);
                File.WriteAllText(plan.AbsolutePath, finalContent);
                _manifest.FileHashes[plan.RelativePath] = Hash(finalContent);
            }

            results.Add(new WriteResult(plan.RelativePath, Written: !unchanged && !dryRun, unchanged, manualEditDetected));
        }

        if (!dryRun)
            _manifest.Save(_manifestPath);

        return results;
    }

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}