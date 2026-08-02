// FileCreator.Core/DependencyInjection/ServiceCollectionExtensions.cs
// Requires the Microsoft.Extensions.DependencyInjection.Abstractions package
// (add it to FileCreator.Core.csproj if the project doesn't already reference it).
using FileCreator.Core.Generators.V2;
using FileCreator.Core.Templating;
using Microsoft.Extensions.DependencyInjection;

namespace FileCreator.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Scriban-based code-generation pipeline: template source,
    /// renderer, and every concrete generator. Add one line here per new
    /// generator (Response/Mapper/Proto/GrpcService/...) — nothing else in
    /// the pipeline needs to change.
    /// </summary>
    public static IServiceCollection AddScribanCodeGeneration(this IServiceCollection services)
    {
        services.AddSingleton<IScribanTemplateSource, EmbeddedResourceTemplateSource>();
        services.AddSingleton<IScribanTemplateRenderer, ScribanTemplateRenderer>();

        services.AddSingleton<EndpointGenerator>();
        services.AddSingleton<EndpointRequestGenerator>();
        // services.AddSingleton<ResponseGenerator>();
        // services.AddSingleton<ValidatorGenerator>();
        // services.AddSingleton<HandlerGenerator>();
        // ... one line per generator, same shape every time.

        return services;
    }
}