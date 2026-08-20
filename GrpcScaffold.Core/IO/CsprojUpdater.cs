using System.Xml.Linq;

namespace GrpcScaffold.Core.IO;

public sealed class CsprojUpdater
{
    public void EnsureProtoInclude(
        string csprojPath,
        string protoDirectory = "Protos",
        string? grpcServices = null)
    {
        if (!File.Exists(csprojPath))
        {
            throw new FileNotFoundException(
                "The target .csproj file was not found.",
                csprojPath);
        }

        var document = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);

        var project = document.Element("Project")
            ?? throw new InvalidOperationException($"Invalid .csproj file: '{csprojPath}'.");

        var include = protoDirectory.TrimEnd('/', '\\').Replace('\\', '/') + "/**/*.proto";

        var alreadyExists = project
            .Descendants("Protobuf")
            .Any(x =>
                string.Equals((string?)x.Attribute("Include"), include, StringComparison.OrdinalIgnoreCase) &&
                (grpcServices is null || string.Equals((string?)x.Attribute("GrpcServices"), grpcServices, StringComparison.OrdinalIgnoreCase)));

        if (alreadyExists)
            return;

        var protobuf = new XElement("Protobuf", new XAttribute("Include", include));
        if (!string.IsNullOrWhiteSpace(grpcServices))
            protobuf.SetAttributeValue("GrpcServices", grpcServices);

        var itemGroup = new XElement("ItemGroup", protobuf);
        project.Add(itemGroup);

        document.Save(csprojPath);
    }
}
