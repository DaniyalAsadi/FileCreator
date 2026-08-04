    // src/GrpcScaffold.Core/IO/AnalysisContext.cs
using Microsoft.CodeAnalysis;

namespace GrpcScaffold.Core.IO;

public sealed class AnalysisContext(
    Project entryProject,
    Compilation entryCompilation,
    IReadOnlyDictionary<ProjectId, Project> projects,
    IReadOnlyDictionary<ProjectId, Compilation> compilations)
{
    public Compilation EntryCompilation { get; } = entryCompilation;

    public Project EntryProject { get; } = entryProject;

    public IReadOnlyDictionary<ProjectId, Project> Projects { get; } = projects;

    public IReadOnlyDictionary<ProjectId, Compilation> Compilations { get; } = compilations;

    public Compilation? FindCompilation(IAssemblySymbol assembly)
    {
        var project = Projects.Values.FirstOrDefault(p =>
            p.AssemblyName == assembly.Name);

        if (project is null)
            return null;

        return Compilations.TryGetValue(project.Id, out var compilation)
            ? compilation
            : null;
    }
}