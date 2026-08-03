using FastColoredTextBoxNS;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;
using Range = FastColoredTextBoxNS.Range;
using System.Collections.Concurrent;

namespace FileCreator;


public sealed class PreviewWorkspace : IDisposable
{
    private readonly MSBuildWorkspace _workspace;

    private Solution _solution;

    // Build order واقعی
    private readonly List<ProjectId> _buildOrder = [];

    // Cache پروژه‌ها
    private readonly Dictionary<ProjectId, Project> _projects = [];

    // Map سریع مسیر → پروژه
    private readonly List<(string dir, ProjectId id)> _projectDirs = [];

    // Cache Style ها
    private static readonly Dictionary<string, TextStyle> _predefinedStyles = new()
    {
        [ClassificationTypeNames.Keyword] = new TextStyle(new SolidBrush(Color.FromArgb(86, 156, 214)), null, FontStyle.Regular),
        [ClassificationTypeNames.ClassName] = new TextStyle(new SolidBrush(Color.FromArgb(78, 201, 176)), null, FontStyle.Regular),
        [ClassificationTypeNames.StructName] = new TextStyle(new SolidBrush(Color.FromArgb(134, 198, 145)), null, FontStyle.Regular),
        [ClassificationTypeNames.InterfaceName] = new TextStyle(new SolidBrush(Color.FromArgb(184, 215, 163)), null, FontStyle.Regular),
        [ClassificationTypeNames.EnumName] = new TextStyle(new SolidBrush(Color.FromArgb(184, 215, 163)), null, FontStyle.Regular),
        [ClassificationTypeNames.MethodName] = new TextStyle(new SolidBrush(Color.FromArgb(220, 220, 170)), null, FontStyle.Regular),
        [ClassificationTypeNames.PropertyName] = new TextStyle(new SolidBrush(Color.FromArgb(220, 220, 170)), null, FontStyle.Regular),
        [ClassificationTypeNames.StringLiteral] = new TextStyle(new SolidBrush(Color.FromArgb(214, 157, 133)), null, FontStyle.Regular),
        [ClassificationTypeNames.NumericLiteral] = new TextStyle(new SolidBrush(Color.FromArgb(181, 206, 168)), null, FontStyle.Regular),
        [ClassificationTypeNames.Comment] = new TextStyle(new SolidBrush(Color.FromArgb(87, 166, 74)), null, FontStyle.Regular),
        ["Default"] = new TextStyle(new SolidBrush(Color.Gainsboro), null, FontStyle.Regular)
    };

    private bool _isWarmedUp;

    public PreviewWorkspace(string slnPath)
    {
        _workspace = MSBuildWorkspace.Create();

        _solution = _workspace.OpenSolutionAsync(slnPath).Result;

        BuildProjectGraph();
    }

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
    public async Task WarmupAsync()
    {
        if (_isWarmedUp)
            return;

        foreach (var projectId in _buildOrder)
        {
            var project = _solution.GetProject(projectId)!;
            _ = await project.GetCompilationAsync();
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
            var projectId = ResolveProjectForFile(file.Path);

            var docId = DocumentId.CreateNewId(projectId, file.Path);

            solution = solution.AddDocument(docId, file.Path, SourceText.From(file.Content));
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
    // --------------------------------------------
    public async Task HighlightAsync(FastColoredTextBox editor, GeneratedFile file)
    {
        if (!_isWarmedUp)
            await WarmupAsync();

        var projectId = ResolveProjectForFile(file.Path);
        var project = _solution.GetProject(projectId)!;

        var tempDoc = project.AddDocument("Preview_" + Guid.NewGuid() + ".cs",
            SourceText.From(file.Content));

        var spans = await Classifier.GetClassifiedSpansAsync(
            tempDoc,
            new TextSpan(0, file.Content.Length));

        ApplyHighlight(editor, spans);
    }

    // --------------------------------------------
    // Apply syntax coloring
    // --------------------------------------------
    private void ApplyHighlight(FastColoredTextBox editor, IEnumerable<ClassifiedSpan> spans)
    {
        editor.BeginUpdate();
        editor.ClearStylesBuffer();

        foreach (var span in spans)
        {
            var style = _predefinedStyles.TryGetValue(span.ClassificationType, out TextStyle? value)
                ? value : _predefinedStyles["Default"];

            var range = GetRange(editor, span.TextSpan);
            range.SetStyle(style);
        }

        editor.EndUpdate();
    }
    private static Range GetRange(FastColoredTextBox editor, TextSpan span)
    {
        var start = editor.PositionToPlace(span.Start);
        var end = editor.PositionToPlace(span.End);
        return new Range(editor, start, end);
    }


    public void Dispose() => _workspace.Dispose();
}