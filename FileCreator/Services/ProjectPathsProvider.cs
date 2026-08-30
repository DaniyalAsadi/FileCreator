using System.Collections.Generic;
using System.Text.Json;
using FileCreator.Core.Projects;

namespace FileCreator.Services;

public interface IProjectPathsProvider
{
    ProjectPaths Load(string projectName, string slnPath);
}

public class ProjectPathsProvider : IProjectPathsProvider
{
    public ProjectPaths Load(string projectName, string slnPath)
    {
        var projects = JsonSerializer.Deserialize<Dictionary<string, string>>(
            Properties.Settings.Default.ProjectPathes) ?? [];
        return ProjectPathsResolver.Resolve(projectName, slnPath, projects);
    }
}
