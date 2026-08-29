using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileCreator.Core;
using FileCreator.Core.Rewriter;
using FileCreator.Core.Walker;
using FileCreator.FileCreatorService;
using FileCreator.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace FileCreator;

public partial class FileCreatorForm : Window
{
    private string _slnPath = string.Empty;
    private string _projectName = string.Empty;
    private string _solutionName = string.Empty;
    private PreviewWorkspace _workspace = default!;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceCache _cache;
    private readonly IProjectPathsProvider _pathsProvider; // جدید
    private readonly GenerationContext _context;

    public FileCreatorForm(
        IServiceProvider serviceProvider,
        IProjectPathsProvider pathsProvider,
        IWorkspaceCache cache,
        GenerationContext context)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _context = context;
        _pathsProvider = pathsProvider;

        InitializeComponent();

        SetDefaultValue();
        LoadSettings(GetSelectedText(cmbProjectName));

        Loaded += OnLoaded;
    }

    // ----------------------------------------------------
    // Workspace Initialization (ONLY ONCE)
    // ----------------------------------------------------
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_slnPath))
        {
            MessageBox.Show("Please set the solution path in settings.");
            return;
        }
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _workspace = _cache.GetWorkspace();

            await _workspace.WarmupAsync();
        }
        catch
        {
            var settings = _serviceProvider.GetRequiredService<SettingsForm>();
            settings.Owner = this;

            if (settings.ShowDialog() == true)
            {
                _workspace = _cache.GetWorkspace();
                await _workspace.WarmupAsync();
            }
            else
            {
                Close();
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    // ----------------------------------------------------
    // SETTINGS
    // ----------------------------------------------------
    private void LoadSettings(string projectName)
    {
        _slnPath = Properties.Settings.Default.SolutionPath;
        _solutionName = Path.GetFileNameWithoutExtension(_slnPath);
        _projectName = projectName;
        _context.ProjectName = _projectName;
        _context.SolutionPath = _slnPath;
        _context.SolutionName = _solutionName;

        // همه‌ی مسیرها یک‌جا resolve می‌شن و در GenerationContext می‌شینن
        _context.Paths = _pathsProvider.Load(projectName, _slnPath);

        btnGenerate.IsEnabled =
            !string.IsNullOrWhiteSpace(_context.SolutionPath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.UseCasesBasePath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.WebBasePath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.FunctionalTestsBasePath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.UnitTestsBasePath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.SharedKernelTestsBasePath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.InfrastructureBasePath) &&
            !string.IsNullOrWhiteSpace(_context.Paths.LocalizationBasePath) &&
            !string.IsNullOrEmpty(_context.Paths.SharedKernelToolsTestsBasePath);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _serviceProvider.CreateScope();

        var frm = scope.ServiceProvider.GetRequiredService<SettingsForm>();
        frm.Owner = this;

        if (frm.ShowDialog() == true)
        {
            LoadSettings(GetSelectedText(cmbProjectName));

            _workspace = _cache.GetWorkspace();
        }
    }

    private void BtnGrpcGeneration_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _serviceProvider.CreateScope();

        var frm = scope.ServiceProvider.GetRequiredService<GrpcGenerationForm>();
        frm.Owner = this;

        frm.ShowDialog();
    }

    // ----------------------------------------------------
    // GENERATION PIPELINE
    // ----------------------------------------------------
    private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        if (_workspace is null)
        {
            MessageBox.Show("Workspace is not ready yet.");
            return;
        }

        string useCaseName = txtUseCaseName.Text.Trim();
        string route = txtRoute.Text.Trim();

        RequestType type = Enum.Parse<RequestType>(GetSelectedText(cmbType) is { Length: > 0 } t ? t : "Command");
        ResponseType responseType = Enum.Parse<ResponseType>(GetSelectedText(cmbResponseType) is { Length: > 0 } r ? r : "Single");
        HttpVerb httpVerb = Enum.Parse<HttpVerb>((GetSelectedText(cmbVerb) is { Length: > 0 } v ? v : "POST").ToUpper());

        bool hasRequest = chkHasRequest.IsChecked == true;
        bool hasResponse = chkHasResponse.IsChecked == true;

        // -------- VALIDATION --------

        if (string.IsNullOrWhiteSpace(useCaseName))
        {
            MessageBox.Show("UseCase Name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(route))
        {
            MessageBox.Show("Route is required.");
            return;
        }

        if (type == RequestType.Command && responseType is ResponseType.IEnumerable or ResponseType.PagedList)
        {
            MessageBox.Show("Command cannot return collections.");
            return;
        }

        if (type == RequestType.Query && !hasResponse)
        {
            MessageBox.Show("Query must have response.");
            return;
        }

        var groupString = txtUseCaseGroup.Text.Trim();
        if (string.IsNullOrWhiteSpace(groupString))
        {
            MessageBox.Show("UseCase Group is required.");
            return;
        }

        if (GroupName.IsPlural(groupString))
        {
            MessageBox.Show($"{groupString} must be singular.");
            return;
        }

        GroupName group = GroupName.Create(groupString);

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            // 1️⃣ Generate Roslyn Files (memory only)
            var generator = new RoslynFileCreator(
                projectName: _projectName,
                groupName: group,
                usecaseName: useCaseName,
                useCasePath: _context.Paths.UseCasesBasePath,
                webPath: _context.Paths.WebBasePath,
                functionalTestPath: _context.Paths.FunctionalTestsBasePath,
                unitTestPath: _context.Paths.UnitTestsBasePath,
                infrastructurePath: _context.Paths.InfrastructureBasePath,
                hasRequest: hasRequest,
                requestType: type,
                hasResponse: hasResponse,
                responseType: responseType,
                httpVerb: httpVerb);

            var previewFiles = await generator.GeneratePreview();

            // 2️⃣ Inject into Roslyn Solution Snapshot
            _workspace.InjectGeneratedFiles(previewFiles);

            // 3️⃣ Show Preview (Viewer Only)
            var previewForm = new PreviewForm(_workspace, previewFiles) { Owner = this };

            if (previewForm.ShowDialog() != true)
            {
                return;
            }
            // 4️⃣ Write to Disk
            RoslynFileCreator.WriteFiles(previewFiles);

            var apiRoutePath = FindApiRoutes(_context.Paths.SharedKernelToolsTestsBasePath);
            ApiRoutesUpdater.Update(
                apiRoutePath,
                _projectName,
                group.Resource,
                useCaseName,
                httpVerb,
                route);

            MessageBox.Show("Files generated successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    // ----------------------------------------------------
    // HELPERS
    // ----------------------------------------------------
    public static string FindApiRoutes(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException(rootDirectory);

        var file = Directory
            .EnumerateFiles(rootDirectory, "ApiRoutes.cs", SearchOption.AllDirectories)
            .FirstOrDefault();

        return file ?? throw new FileNotFoundException("ApiRoutes.cs not found.");
    }

    private static string GetSelectedText(ComboBox combo)
    {
        return combo.SelectedItem switch
        {
            ComboBoxItem item => item.Content?.ToString() ?? string.Empty,
            null => string.Empty,
            var other => other.ToString() ?? string.Empty
        };
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ChkHasResponse_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (cmbType.SelectedItem == null)
            return;

        var type = Enum.Parse<RequestType>(GetSelectedText(cmbType));

        if (type == RequestType.Command)
        {
            if (chkHasResponse.IsChecked == true)
            {
                cmbResponseType.IsEnabled = false;
                SelectComboItem(cmbResponseType, ResponseType.Single.ToString());
            }
            else
            {
                cmbResponseType.IsEnabled = false;
                cmbResponseType.SelectedItem = null;
            }
        }
    }

    private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbType.SelectedItem == null)
            return;

        var type = Enum.Parse<RequestType>(GetSelectedText(cmbType));

        switch (type)
        {
            case RequestType.Command:
                {
                    // Command may or may not return data
                    chkHasResponse.IsEnabled = true;

                    if (chkHasResponse.IsChecked != true)
                    {
                        cmbResponseType.IsEnabled = false;
                        cmbResponseType.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        cmbResponseType.Visibility = Visibility.Visible;
                        cmbResponseType.IsEnabled = false;
                        SelectComboItem(cmbResponseType, ResponseType.Single.ToString());
                    }

                    break;
                }

            case RequestType.Query:
                {
                    chkHasResponse.IsChecked = true;
                    chkHasResponse.IsEnabled = false;

                    cmbResponseType.IsEnabled = true;

                    if (cmbResponseType.SelectedItem == null)
                        cmbResponseType.SelectedIndex = 0;

                    break;
                }
        }
    }

    private async void MenuUpdateEnums_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            menuUpdateEnums.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            await Task.Run(UpdateEnumsInSolution);

            MessageBox.Show(
                "Enum Display attributes updated successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            menuUpdateEnums.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
    }

    private void UpdateEnumsInSolution()
    {
        var solutionPath = Properties.Settings.Default.SolutionPath;

        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            throw new InvalidOperationException("Solution path is not configured.");

        var srcPath = Directory.GetParent(_context.Paths.WebBasePath)?.ToString() ?? throw new ArgumentNullException();

        var csFiles = Directory
            .GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\bin\") && !f.Contains(@"\obj\"))
            .ToList();

        foreach (var file in csFiles)
        {
            var source = File.ReadAllText(file);

            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();

            var rewriter = new EnumRewriter();
            var newRoot = rewriter.Visit(root);

            if (!ReferenceEquals(root, newRoot))
            {
                var workspace = new AdhocWorkspace();
                var formatted = Formatter.Format(newRoot, workspace);

                File.WriteAllText(file, formatted.ToFullString());
            }
        }
    }

    private void MenuUpdateResx_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var resxPathes = Directory.GetFiles(Path.Combine(_context.Paths.LocalizationBasePath,
                "Resources"), "*.resx");
            var resxDesignPath = Directory.GetFiles(Path.Combine(_context.Paths.LocalizationBasePath,
                "Resources"), "*.cs").First();

            if (resxPathes.Length == 0 || !File.Exists(resxPathes.First()))
            {
                MessageBox.Show("Resx file not found.");
                return;
            }

            var collector = new EnumKeyCollector();
            var srcPath = Path.Combine(Directory.GetParent(_slnPath)?.FullName!, _solutionName, _projectName, "src");

            var csFiles = Directory
                .GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\bin\") && !f.Contains(@"\obj\"))
                .ToList();
            foreach (var file in csFiles)
            {
                var text = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(text);
                collector.Visit(tree.GetRoot());
            }

            var updater = new ResxUpdater();

            foreach (var resxPath in resxPathes)
                foreach (var key in collector.Keys)
                {
                    ResxUpdater.EnsureKeyExists(resxPath, key, key);
                }

            var designFile = File.ReadAllText(resxDesignPath);
            var designTree = CSharpSyntaxTree.ParseText(designFile);
            var designWriter = new ResxDesignRewriter("Enum_Authentication_Test");

            var newDesignClass = designWriter.Visit(designTree.GetRoot());

            var newClass = newDesignClass.NormalizeWhitespace().ToFullString();

            MessageBox.Show("Resx sync completed successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void CmbProjectName_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var item = GetSelectedText(cmbProjectName);
        if (!string.IsNullOrEmpty(item))
        {
            LoadSettings(item);
        }
    }

    private void BtnGenerate_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is Button button)
        {
            if (button.IsEnabled)
            {
                button.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(255, 0, 122, 204));
                button.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                button.Background = System.Windows.Media.Brushes.LightGray;
                button.Foreground = System.Windows.Media.Brushes.DarkGray;
                MessageBox.Show("Solution path is not configured.", "sln Path Required", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static void SelectComboItem(ComboBox combo, string text)
    {
        foreach (var obj in combo.Items)
        {
            if (obj is ComboBoxItem item && string.Equals(item.Content?.ToString(), text, StringComparison.Ordinal))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private void SetDefaultValue()
    {
        cmbProjectName.SelectedIndex = 4;
        txtUseCaseGroup.Text = "ErrorLog";
        txtUseCaseName.Text = "Create";
        txtRoute.Text = "error-logs";
        cmbType.SelectedIndex = 0;
        cmbVerb.SelectedIndex = 0;
        cmbResponseType.SelectedIndex = 0;
        chkHasRequest.IsChecked = true;
        chkHasResponse.IsChecked = true;
    }

    // Allow dragging the borderless window by its menu bar.
    private void MenuBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
