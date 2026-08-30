using FileCreator.Grpc.Factories;
using FileCreator.Grpc.ViewModels;
using FileCreator.Core.Loading;
using GrpcScaffold.Core.Analysis;
using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace FileCreator.Grpc.Discovery;

public interface IEndpointDiscoveryService
{
    Task<ImmutableArray<EndpointModel>> DiscoverAsync(
        GrpcGenerationOptions options,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class EndpointDiscoveryService(
    IWorkspaceCache cache,
    EndpointFilter filter) : IEndpointDiscoveryService
{
    public async Task<ImmutableArray<EndpointModel>> DiscoverAsync(
        GrpcGenerationOptions options,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new LoadingProgress(0, "Preparing endpoint discovery"));
        cancellationToken.ThrowIfCancellationRequested();

        var workspace = cache.Current;
        progress?.Report(new LoadingProgress(5, "Using loaded workspace", options.ProjectName));

        var context = await GrpcAnalysisContextFactory.FromWorkspaceAsync(
                workspace,
                options.ProjectName,
                progress,
                cancellationToken)
            .ConfigureAwait(false);

        var analyzer = new EndpointAnalyzer(
            context,
            new VisibilityResolver(),
            new MediatorSendResolver());

        progress?.Report(new LoadingProgress(70, "Discovering gRPC endpoints", context.EntryProject.Name));
        var analysisProgress = new InlineProgress<EndpointAnalysisProgress>(value =>
        {
            var percentage = value.TotalSyntaxTrees == 0
                ? 90
                : 70 + (int)((value.CompletedSyntaxTrees / (double)value.TotalSyntaxTrees) * 20);

            progress?.Report(new LoadingProgress(
                percentage,
                "Discovering gRPC endpoints",
                string.IsNullOrWhiteSpace(value.FilePath)
                    ? context.EntryProject.Name
                    : Path.GetFileName(value.FilePath)));
        });

        // Roslyn syntax/semantic traversal is CPU-intensive and its async syntax-root
        // reads are often already completed after workspace warm-up. Run only this
        // immutable-compilation analysis on the thread pool so WPF stays responsive.
        var endpoints = await Task
            .Run(
                () => analyzer.DiscoverAsync(
                    context.EntryCompilation,
                    cancellationToken,
                    analysisProgress),
                cancellationToken)
            .ConfigureAwait(false);

        if (options.GenerateAll && !string.IsNullOrWhiteSpace(options.EndpointFilter))
            throw new InvalidOperationException("Generate All نمی‌تواند همراه با Endpoint Filter باشد.");
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new LoadingProgress(95, "Applying endpoint filters"));
        var result = filter.Apply(endpoints, options);
        progress?.Report(new LoadingProgress(100, "Completed", $"{result.Length} endpoints"));
        return result;
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
