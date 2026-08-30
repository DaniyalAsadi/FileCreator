using FileCreator.Services;
using System;
using System.Collections.Generic;
using System.Text;

using FileCreator.Core.Projects;

namespace FileCreator;

public sealed class GenerationContext
{
    public string SolutionPath { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string SolutionName { get; set; } = string.Empty;

    public ProjectPaths Paths { get; set; } = new ProjectPaths();

}
