using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

using FileCreator.Core.Generation;
using FileCreator.Core.Loading;

namespace FileCreator;


public sealed class PreviewWorkspace : IDisposable
{
    private readonly MSBuildWorkspace _workspace;

    private Solution _solution;

    // Build order واقعی
    private readonly List<ProjectId> _buildOrder = [];

    // Cache پروژه‌ها
    private readonly Dictionary<ProjectId, Project> _projects = [];

    // Cache compilations so endpoint discovery does not compile every project twice.
    private readonly Dictionary<ProjectId, Compilation> _compilations = [];

    // Map سریع مسیر → پروژه
    private readonly List<(string dir, ProjectId id)> _projectDirs = [];

    public Solution CurrentSolution => _solution;

    public IReadOnlyList<Project> ProjectsInBuildOrder =>
        _buildOrder.Select(id => _projects[id]).ToList();

    public Project? FindProjectByNameSuffix(string suffix) =>
        ProjectsInBuildOrder.FirstOrDefault(p => p.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    public async Task<Compilation?> GetCompilationAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default)
    {
        if (!_isWarmedUp)
            await WarmupAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return _compilations.GetValueOrDefault(projectId);
    }



    // Cache رنگ‌ها (WPF colors) — همان تم قبلی (VS Dark)
    private static readonly Dictionary<string, Color> _predefinedColors = new()
    {
        [ClassificationTypeNames.Keyword] = Color.FromRgb(86, 156, 214),
        [ClassificationTypeNames.ClassName] = Color.FromRgb(78, 201, 176),
        [ClassificationTypeNames.StructName] = Color.FromRgb(134, 198, 145),
        [ClassificationTypeNames.InterfaceName] = Color.FromRgb(184, 215, 163),
        [ClassificationTypeNames.EnumName] = Color.FromRgb(184, 215, 163),
        [ClassificationTypeNames.MethodName] = Color.FromRgb(220, 220, 170),
        [ClassificationTypeNames.PropertyName] = Color.FromRgb(220, 220, 170),
        [ClassificationTypeNames.StringLiteral] = Color.FromRgb(214, 157, 133),
        [ClassificationTypeNames.NumericLiteral] = Color.FromRgb(181, 206, 168),
        [ClassificationTypeNames.Comment] = Color.FromRgb(87, 166, 74),
        [ClassificationTypeNames.ControlKeyword] = Color.FromRgb(216, 160, 223),
        ["Default"] = Color.FromRgb(220, 220, 220) // Gainsboro-ish
    };

    public static Color DefaultColor => _predefinedColors["Default"];

    public static Color GetColor(string classificationType) =>
        _predefinedColors.TryGetValue(classificationType, out var color)
            ? color
            : _predefinedColors["Default"];

    private bool _isWarmedUp;

    private PreviewWorkspace(
        MSBuildWorkspace workspace,
        Solution solution)
    {
        _workspace = workspace;
        _solution = solution;
        BuildProjectGraph();
    }

    public static async Task<PreviewWorkspace> LoadAsync(
        string solutionPath,
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new LoadingProgress(0, "Initializing"));
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(new LoadingProgress(5, "Locating solution", solutionPath));
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException("The configured solution file was not found.", solutionPath);

        progress?.Report(new LoadingProgress(10, "Creating MSBuild workspace"));
        var workspace = MSBuildWorkspace.Create();
        var loadEventCount = 0;

        workspace.RegisterWorkspaceFailedHandler(args =>
        {
            Trace.TraceWarning(
                "Roslyn workspace diagnostic while loading {0}: {1}",
                solutionPath,
                args.Diagnostic.Message);

            progress?.Report(new LoadingProgress(
                Math.Min(49, 15 + Volatile.Read(ref loadEventCount)),
                "Loading solution",
                args.Diagnostic.Message));
        });

        var roslynProgress = new InlineProgress<ProjectLoadProgress>(value =>
        {
            var eventNumber = Interlocked.Increment(ref loadEventCount);
            progress?.Report(new LoadingProgress(
                Math.Min(49, 15 + eventNumber),
                DescribeLoadOperation(value.Operation),
                string.IsNullOrWhiteSpace(value.FilePath)
                    ? null
                    : Path.GetFileName(value.FilePath)));
        });

        try
        {
            progress?.Report(new LoadingProgress(15, "Loading solution", Path.GetFileName(solutionPath)));
            var solution = await workspace.OpenSolutionAsync(
                    solutionPath,
                    roslynProgress,
                    cancellationToken)
                .ConfigureAwait(false);

            progress?.Report(new LoadingProgress(50, "Solution loaded", solution.FilePath));

            var result = new PreviewWorkspace(workspace, solution);
            progress?.Report(new LoadingProgress(
                55,
                "Building project dependency graph",
                $"{result._buildOrder.Count} C# projects"));

            await result.WarmupAsync(progress, cancellationToken).ConfigureAwait(false);
            progress?.Report(new LoadingProgress(98, "Finalizing workspace", Path.GetFileName(solutionPath)));
            return result;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static string DescribeLoadOperation(ProjectLoadOperation operation) => operation switch
    {
        ProjectLoadOperation.Evaluate => "Evaluating project",
        ProjectLoadOperation.Build => "Resolving project references",
        ProjectLoadOperation.Resolve => "Resolving project",
        _ => "Loading solution"
    };

    // --------------------------------------------
    // ساخت Dependency Graph (همان Build Order)
    // --------------------------------------------
    private void BuildProjectGraph()
    {
        var graph = _solution.GetProjectDependencyGraph();

        foreach (var projectId in graph.GetTopologicallySortedProjects())
        {
            var project = _solution.GetProject(projectId)!;

            if (project.Language != LanguageNames.CSharp)
                continue;

            _buildOrder.Add(projectId);
            _projects[projectId] = project;

            var dir = Path.GetDirectoryName(project.FilePath!)!;
            _projectDirs.Add((dir, projectId));
        }
    }

    // --------------------------------------------
    // Warmup — دقیقاً کاری که VS انجام می‌دهد
    // --------------------------------------------
    public async Task WarmupAsync(
        IProgress<LoadingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_isWarmedUp)
            return;

        for (var index = 0; index < _buildOrder.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectId = _buildOrder[index];
            var project = _solution.GetProject(projectId)!;
            var compilation = await project
                .GetCompilationAsync(cancellationToken)
                .ConfigureAwait(false);

            if (compilation is not null)
                _compilations[projectId] = compilation;

            var percentage = _buildOrder.Count == 0
                ? 95
                : 60 + (int)(((index + 1) / (double)_buildOrder.Count) * 35);

            progress?.Report(new LoadingProgress(
                percentage,
                "Analyzing Roslyn compilation",
                project.Name));
        }

        _isWarmedUp = true;
    }

    // --------------------------------------------
    // Inject فایل‌های Generated داخل Workspace
    // --------------------------------------------
    public void InjectGeneratedFiles(IEnumerable<GeneratedFile> files)
    {
        var solution = _solution;

        foreach (var file in files)
        {
            var projectId = ResolveProjectForFile(file.AbsolutePath);

            var docId = DocumentId.CreateNewId(projectId, file.AbsolutePath);

            solution = solution.AddDocument(docId, file.AbsolutePath, SourceText.From(file.Content));
        }

        _solution = solution;

        RefreshProjectCache();
    }

    // --------------------------------------------
    // بعد از Inject باید Project reference ها refresh شوند
    // --------------------------------------------
    private void RefreshProjectCache()
    {
        _projects.Clear();
        _compilations.Clear();

        foreach (var id in _buildOrder)
        {
            var project = _solution.GetProject(id)!;
            _projects[id] = project;
        }

        _isWarmedUp = false;
    }

    // --------------------------------------------
    // Resolve سریع پروژه برای هر فایل
    // --------------------------------------------
    private ProjectId ResolveProjectForFile(string filePath)
    {
        foreach (var (dir, id) in _projectDirs)
        {
            if (filePath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                return id;
        }

        return _buildOrder[0];
    }

    // --------------------------------------------
    // Highlight واقعی با Semantic Model
    // خروجی: لیست بازه‌های رنگی برای رندر در RichTextBox
    // --------------------------------------------
    public async Task<IReadOnlyList<ClassifiedSpan>> GetClassifiedSpansAsync(GeneratedFile file)
    {
        if (!_isWarmedUp)
            await WarmupAsync();

        var projectId = ResolveProjectForFile(file.AbsolutePath);
        var project = _solution.GetProject(projectId)!;

        var tempDoc = project.AddDocument("Preview_" + Guid.NewGuid() + ".cs",
            SourceText.From(file.Content));

        var spans = await Classifier.GetClassifiedSpansAsync(
            tempDoc,
            new TextSpan(0, file.Content.Length));

        return [.. spans];
    }

    public void Dispose() => _workspace.Dispose();

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
