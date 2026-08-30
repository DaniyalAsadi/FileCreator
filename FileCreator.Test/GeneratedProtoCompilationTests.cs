using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;

public sealed class GeneratedProtoCompilationTests
{
    [Fact]
    public void Nullable_map_fixture_compiles_with_protoc()
    {
        var grpcToolsRoot = ResolveGrpcToolsRoot();
        var protoc = Path.Combine(grpcToolsRoot, "tools", "windows_x64", "protoc.exe");
        var wellKnownTypes = Path.Combine(grpcToolsRoot, "build", "native", "include");
        File.Exists(protoc).Should().BeTrue($"Grpc.Tools protoc should exist at {protoc}");

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "filecreator-proto-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var protoPath = Path.Combine(tempRoot, "MapService.proto");
            var descriptorPath = Path.Combine(tempRoot, "MapService.pb");
            File.WriteAllText(protoPath, MapNullableValueTests.GenerateProto());

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = protoc,
                ArgumentList =
                {
                    $"--proto_path={tempRoot}",
                    $"--proto_path={wellKnownTypes}",
                    $"--descriptor_set_out={descriptorPath}",
                    protoPath
                },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process.Should().NotBeNull();
            process!.WaitForExit();
            var standardError = process.StandardError.ReadToEnd();

            process.ExitCode.Should().Be(0, standardError);
            File.Exists(descriptorPath).Should().BeTrue();
            new FileInfo(descriptorPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string ResolveGrpcToolsRoot()
    {
        const string packageRelativePath = "grpc.tools/2.80.0";
        var configuredRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var configuredPackage = Path.Combine(configuredRoot, packageRelativePath);
            if (Directory.Exists(configuredPackage))
            {
                return configuredPackage;
            }
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var assetsPath = Path.Combine(directory.FullName, "obj", "project.assets.json");
            if (!File.Exists(assetsPath))
            {
                continue;
            }

            using var assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
            foreach (var packageFolder in assets.RootElement.GetProperty("packageFolders").EnumerateObject())
            {
                var resolvedPackage = Path.Combine(packageFolder.Name, packageRelativePath);
                if (Directory.Exists(resolvedPackage))
                {
                    return resolvedPackage;
                }
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not resolve {packageRelativePath} from NUGET_PACKAGES or project.assets.json.");
    }
}
