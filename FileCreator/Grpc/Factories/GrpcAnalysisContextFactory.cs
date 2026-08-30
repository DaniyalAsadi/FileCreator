using Microsoft.CodeAnalysis;
using FileCreator.Core.Loading;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.Factories;

internal class GrpcAnalysisContextFactory
{
    public static async Task<GrpcScaffold.Core.IO.AnalysisContext> FromWorkspaceAsync(
        PreviewWorkspace workspace,
        string entryProjectName,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var entryProject = workspace.FindProjectByNameSuffix(entryProjectName + ".Web")
            ?? throw new InvalidOperationException($"پروژه‌ی ورودی با پسوند '{entryProjectName + ".Web"}' پیدا نشد.");

        var projects = new Dictionary<ProjectId, Project>();
        var compilations = new Dictionary<ProjectId, Compilation>();

        var projectsInBuildOrder = workspace.ProjectsInBuildOrder;
        for (var index = 0; index < projectsInBuildOrder.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var project = projectsInBuildOrder[index];
            var compilation = await workspace
                .GetCompilationAsync(project.Id, cancellationToken)
                .ConfigureAwait(false);

            if (compilation is null) continue;

            projects[project.Id] = project;
            compilations[project.Id] = compilation;

            var percentage = projectsInBuildOrder.Count == 0
                ? 65
                : 10 + (int)(((index + 1) / (double)projectsInBuildOrder.Count) * 55);

            progress?.Report(new LoadingProgress(
                percentage,
                "Loading project compilations",
                project.Name));
        }

        if (!compilations.TryGetValue(entryProject.Id, out var entryCompilation))
            throw new InvalidOperationException($"Compilation for project '{entryProject.Name}' could not be created.");

        return new GrpcScaffold.Core.IO.AnalysisContext(entryProject, entryCompilation, projects, compilations);
    }

}
