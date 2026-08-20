// src/GrpcScaffold.Core/Generation/DiRegistrationGenerator.cs
namespace GrpcScaffold.Core.Generation;

public sealed record GrpcClientRegistrationDescriptor(
    string ServiceName,
    string ProtoNamespace,
    string ClientNamespace,
    string ClientClassName);

public sealed class DiRegistrationGenerator(TemplateEngine templates)
{
    public string Generate(IEnumerable<string> serviceNames, string grpcNamespace)
    {
        var model = new Dictionary<string, object?>
        {
            ["registration_kind"] = "server",
            ["grpc_namespace"] = grpcNamespace,
            ["services"] = serviceNames.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList(),
        };
        return templates.Render("di-registration.sbn", model);
    }

    public string GenerateClient(
        IEnumerable<GrpcClientRegistrationDescriptor> services,
        string registrationNamespace,
        string configurationSection = "Grpc:Services")
    {
        ArgumentNullException.ThrowIfNull(services);

        var model = new Dictionary<string, object?>
        {
            ["registration_kind"] = "client",
            ["grpc_namespace"] = registrationNamespace,
            ["configuration_section"] = configurationSection,
            ["client_services"] = services
                .DistinctBy(s => s.ServiceName)
                .OrderBy(s => s.ServiceName, StringComparer.Ordinal)
                .Select(s => new Dictionary<string, object?>
                {
                    ["service_name"] = s.ServiceName,
                    ["proto_namespace"] = s.ProtoNamespace,
                    ["client_namespace"] = s.ClientNamespace,
                    ["client_class_name"] = s.ClientClassName
                })
                .ToList()
        };

        return templates.Render("di-registration.sbn", model);
    }
}
