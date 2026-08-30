using FileCreator.Core.Projects;
using FluentAssertions;

public sealed class ProjectPathsResolverTests
{
    [Fact]
    public void Configured_project_files_are_resolved_against_the_solution_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), "filecreator-path-tests");
        var solutionPath = Path.Combine(root, "Demo.slnx");
        var projects = new Dictionary<string, string>
        {
            ["Demo.UseCases"] = Path.Combine("src", "Demo.UseCases", "Demo.UseCases.csproj"),
            ["Demo.Web"] = Path.Combine("src", "Demo.Web", "Demo.Web.csproj"),
            ["Presentation.Bff"] = Path.Combine("edge", "Presentation.Bff", "Presentation.Bff.csproj")
        };

        var result = ProjectPathsResolver.Resolve("Demo", solutionPath, projects);

        result.UseCasesBasePath.Should().Be(Path.Combine(root, "src", "Demo.UseCases"));
        result.WebBasePath.Should().Be(Path.Combine(root, "src", "Demo.Web"));
        result.BffBasePath.Should().Be(Path.Combine(root, "edge", "Presentation.Bff"));
        result.InfrastructureBasePath.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_boundary_input_is_rejected_before_generation()
    {
        var act = () => ProjectPathsResolver.Resolve(
            string.Empty,
            Path.Combine(Path.GetTempPath(), "Demo.slnx"),
            new Dictionary<string, string>());

        act.Should().Throw<ArgumentException>();
    }
}
