namespace FileCreator.Core.Projects;

/// <summary>
/// Resolves configured project-file paths against a solution directory without loading
/// application settings or touching the file system.
/// </summary>
public static class ProjectPathsResolver
{
    public static ProjectPaths Resolve(
        string projectName,
        string solutionPath,
        IReadOnlyDictionary<string, string> configuredProjects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        ArgumentNullException.ThrowIfNull(configuredProjects);

        var absoluteSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(absoluteSolutionPath)
            ?? throw new ArgumentException("The solution path must include a directory.", nameof(solutionPath));

        string ResolveProjectDirectory(string key)
        {
            if (!configuredProjects.TryGetValue(key, out var projectPath) ||
                string.IsNullOrWhiteSpace(projectPath))
            {
                return string.Empty;
            }

            var configuredDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(solutionDirectory, configuredDirectory));
        }

        return new ProjectPaths(
            ResolveProjectDirectory($"{projectName}.UseCases"),
            ResolveProjectDirectory($"{projectName}.Web"),
            ResolveProjectDirectory($"{projectName}.FunctionalTests"),
            ResolveProjectDirectory($"{projectName}.UnitTests"),
            ResolveProjectDirectory("SharedKernel"),
            ResolveProjectDirectory($"{projectName}.Infrastructure"),
            ResolveProjectDirectory("Localization"),
            ResolveProjectDirectory("SharedKernel.Tools"),
            ResolveProjectDirectory("Presentation.Bff"),
            ResolveProjectDirectory("Presentation"));
    }
}
