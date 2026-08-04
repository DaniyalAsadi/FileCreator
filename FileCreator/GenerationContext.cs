using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator;

public sealed class GenerationContext
{
    public string SolutionPath { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string SolutionName { get; set; } = string.Empty;
}