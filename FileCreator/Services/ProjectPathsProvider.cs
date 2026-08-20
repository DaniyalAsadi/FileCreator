using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileCreator.Services;

public interface IProjectPathsProvider
{
    ProjectPaths Load(string projectName, string slnPath);
}

public record ProjectPaths(
    string UseCasesBasePath = "",
    string WebBasePath = "",
    string FunctionalTestsBasePath = "",
    string UnitTestsBasePath = "",
    string SharedKernelTestsBasePath = "",
    string InfrastructureBasePath = "",
    string LocalizationBasePath = "",
    string SharedKernelToolsTestsBasePath = "",
    string BffBasePath = "",
    string PresentationBasePath = "");

public class ProjectPathsProvider : IProjectPathsProvider
{
    public ProjectPaths Load(string projectName, string slnPath)
    {
        var projects = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            Properties.Settings.Default.ProjectPathes) ?? [];
        var solutionFolder = Path.GetDirectoryName(slnPath)!;

        string Resolve(string key) =>
            projects.GetValueOrDefault(key) is { } p
                ? Path.Combine(solutionFolder, Path.GetDirectoryName(p) ?? string.Empty)
                : string.Empty;

        return new ProjectPaths(
            Resolve($"{projectName}.UseCases"),
            Resolve($"{projectName}.Web"),
            Resolve($"{projectName}.FunctionalTests"),
            Resolve($"{projectName}.UnitTests"),
            Resolve("SharedKernel"),
            Resolve($"{projectName}.Infrastructure"),
            Resolve("Localization"),
            Resolve("SharedKernel.Tools"),
            Resolve("Presentation.Bff"),
            Resolve("Presentation"));
    }
}