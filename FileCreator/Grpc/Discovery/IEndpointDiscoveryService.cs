using FileCreator.Grpc.Factories;
using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.Analysis;
using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FileCreator.Grpc.Discovery;

public interface IEndpointDiscoveryService
{
    Task<ImmutableArray<EndpointModel>> DiscoverAsync(GrpcGenerationOptions options);
}

public sealed class EndpointDiscoveryService(
    IWorkspaceCache cache,
    EndpointFilter filter) : IEndpointDiscoveryService
{
    public async Task<ImmutableArray<EndpointModel>> DiscoverAsync(GrpcGenerationOptions options)
    {
        var workspace = cache.GetWorkspace();
        var context = await GrpcAnalysisContextFactory.FromWorkspaceAsync(workspace,options.ProjectName);

        var analyzer = new EndpointAnalyzer(
            context,
            new VisibilityResolver(),
            new MediatorSendResolver());

        var endpoints = await analyzer.DiscoverAsync(context.EntryCompilation);

        if (options.GenerateAll && !string.IsNullOrWhiteSpace(options.EndpointFilter))
            throw new InvalidOperationException("Generate All نمی‌تواند همراه با Endpoint Filter باشد.");
        return filter.Apply(endpoints, options);
    }
}
