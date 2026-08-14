using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.ViewModels;

public sealed class GrpcGenerationOptions
{
    public string ProjectName { get; set; } = string.Empty;
    public string SolutionPath { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string OutputFolder { get; set; } = string.Empty;
    public bool GenerateAll { get; set; } = true;
    public string? EndpointFilter { get; set; }
    public bool InternalOnly { get; set; }
    public bool DryRun { get; set; }
    public bool Force { get; set; }
    public bool Strict { get; set; }
    public IReadOnlyList<string> SelectedEndpoints { get; set; }
    = [];

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (GenerateAll && !string.IsNullOrWhiteSpace(EndpointFilter))
            errors.Add("Generate All نمی‌تواند همراه با Endpoint Filter استفاده شود.");

        return errors;
    }
}
