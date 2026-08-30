// src/GrpcScaffold.Core/IO/GenerationManifest.cs
using System.Text.Json;

namespace GrpcScaffold.Core.IO;

public sealed class GenerationManifest
{
    public Dictionary<string, string> FileHashes { get; set; } = new(); // relative path -> sha256

    public static GenerationManifest Load(string path) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<GenerationManifest>(File.ReadAllText(path)) ?? new GenerationManifest()
            : new GenerationManifest();

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var ordered = new GenerationManifest
        {
            FileHashes = FileHashes
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };
        File.WriteAllText(path, JsonSerializer.Serialize(ordered, new JsonSerializerOptions { WriteIndented = true }));
    }
}
