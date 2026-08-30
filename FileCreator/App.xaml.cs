using System.Windows;
using FileCreator.Core.DependencyInjection;
using FileCreator.Core.Generation;
using FileCreator.Grpc.DependencyInjection;
using FileCreator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileCreator;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = default!;

    public IServiceProvider Services => _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddSingleton<IWorkspaceCache, WorkspaceCacheService>();
        services.AddScribanCodeGeneration();
        services.AddSingleton<GeneratedFileWriter>();

        services.AddGrpcScaffoldServices();

        services.AddTransient<FileCreatorForm>();
        services.AddTransient<GrpcGenerationForm>();
        services.AddSingleton<GenerationContext>();
        services.AddTransient<SettingsForm>();
        services.AddScoped<IProjectPathsProvider, ProjectPathsProvider>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<FileCreatorForm>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        WorkspaceCache.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
