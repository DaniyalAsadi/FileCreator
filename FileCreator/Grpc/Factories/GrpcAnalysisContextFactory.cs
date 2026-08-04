using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.Factories;

internal class GrpcAnalysisContextFactory
{
    public static async Task<GrpcScaffold.Core.IO.AnalysisContext> FromWorkspaceAsync(
        PreviewWorkspace workspace,
        string entryProjectName = ".Web")
    {
        var entryProject = workspace.FindProjectByNameSuffix(entryProjectName + ".Web")
            ?? throw new InvalidOperationException($"پروژه‌ی ورودی با پسوند '{entryProjectName + ".Web"}' پیدا نشد.");

        var projects = new Dictionary<ProjectId, Project>();
        var compilations = new Dictionary<ProjectId, Compilation>();

        foreach (var project in workspace.ProjectsInBuildOrder)
        {
            var compilation = await workspace.GetCompilationAsync(project.Id);
            if (compilation is null) continue;

            projects[project.Id] = project;
            compilations[project.Id] = compilation;
        }

        var entryCompilation = compilations[entryProject.Id];

        return new GrpcScaffold.Core.IO.AnalysisContext(entryProject, entryCompilation, projects, compilations);
    }

}
