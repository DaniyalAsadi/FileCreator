namespace FileCreator.Core.Projects;

/// <summary>Resolved project directories used by a FileCreator generation session.</summary>
public sealed record ProjectPaths(
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
