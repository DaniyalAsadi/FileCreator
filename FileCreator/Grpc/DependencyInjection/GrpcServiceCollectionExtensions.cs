using FileCreator.Grpc.Coordination;
using FileCreator.Grpc.Discovery;
using FileCreator.Grpc.Generation;
using FileCreator.Grpc.Preview;
using FileCreator.Grpc.Writers;
using GrpcScaffold.Core.Generation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.DependencyInjection;

public static class GrpcServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcScaffoldServices(this IServiceCollection services)
    {
        services.AddSingleton<EndpointFilter>();
        services.AddSingleton<IEndpointDiscoveryService, EndpointDiscoveryService>();
        services.AddSingleton<ProtoGenerator>();
        services.AddSingleton<TemplateEngine>();
        services.AddSingleton<GrpcServiceGenerator>();
        services.AddSingleton<MappingGenerator>();
        services.AddSingleton<DiRegistrationGenerator>();
        services.AddSingleton<IGrpcCodeGenerator, GrpcCodeGenerator>();
        services.AddSingleton<IGrpcPreviewGenerator, GrpcPreviewGenerator>();
        services.AddSingleton<IGrpcFileWriter, GrpcFileWriter>();
        services.AddSingleton<GrpcGenerationCoordinator>();
        return services;
    }
}
