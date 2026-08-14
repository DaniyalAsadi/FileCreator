namespace FileCreator;

using global::FileCreator.Grpc.DependencyInjection;
using global::FileCreator.Services;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();

        services.AddSingleton<IWorkspaceCache, WorkspaceCacheService>();

        services.AddGrpcScaffoldServices();

        services.AddTransient<FileCreatorForm>();
        services.AddTransient<GrpcGenerationForm>();
        services.AddSingleton<GenerationContext>();
        services.AddTransient<SettingsForm>();
        services.AddScoped<IProjectPathsProvider, ProjectPathsProvider>();
        var provider = services.BuildServiceProvider();

        Application.Run(provider.GetRequiredService<FileCreatorForm>());
    }
}