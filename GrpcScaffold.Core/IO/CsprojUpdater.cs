using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;


namespace GrpcScaffold.Core.IO;

public sealed class CsprojUpdater
{
    public void EnsureProtoInclude(
        string csprojPath,
        string protoDirectory = "Protos")
    {
        if (!File.Exists(csprojPath))
        {
            throw new FileNotFoundException(
                "The target .csproj file was not found.",
                csprojPath);
        }

        var document = XDocument.Load(
            csprojPath,
            LoadOptions.PreserveWhitespace);

        var project = document.Element("Project")
            ?? throw new InvalidOperationException(
                $"Invalid .csproj file: '{csprojPath}'.");

        var include = $"{protoDirectory.TrimEnd('/', '\\')}/**/*.proto";

        var alreadyExists = project
            .Descendants("Protobuf")
            .Any(x =>
                string.Equals(
                    (string?)x.Attribute("Include"),
                    include,
                    StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            return;

        var itemGroup = new XElement(
            "ItemGroup",
            new XElement(
                "Protobuf",
                new XAttribute("Include", include)));

        project.Add(itemGroup);

        document.Save(csprojPath);
    }
}