// src/GrpcScaffold.Core/Generation/DiRegistrationGenerator.cs
namespace GrpcScaffold.Core.Generation;

public sealed class DiRegistrationGenerator(TemplateEngine templates)
{
    public string Generate(IEnumerable<string> serviceNames, string grpcNamespace)
    {
        var model = new Dictionary<string, object?>
        {
            ["grpc_namespace"] = grpcNamespace,
            ["services"] = serviceNames.Distinct().OrderBy(s => s).ToList(),
        };
        return templates.Render("di-registration.sbn", model);
    }
}