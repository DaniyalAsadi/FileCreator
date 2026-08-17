using FileCreator.Core;
using FileCreator.Core.Rewriter;
using FileCreator.Core.Walker;
using FileCreator.FileCreator;
using FileCreator.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace FileCreator;

public partial class FileCreatorForm : Form
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

        LoadSettings(cmbProjectName.Text);
        SetDefaultValue();
    }

    // ----------------------------------------------------
    // Workspace Initialization (ONLY ONCE)
    // ----------------------------------------------------
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (string.IsNullOrWhiteSpace(_slnPath))
        {
            MessageBox.Show("Please set the solution path in settings.");
            return;
        }
        try
        {
            UseWaitCursor = true;
            _workspace = _cache.GetWorkspace();

            await _workspace.WarmupAsync();
        }
        catch
        {
            using var settings = _serviceProvider.GetRequiredService<SettingsForm>();

            if (settings.ShowDialog(this) == DialogResult.OK)
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
            UseWaitCursor = false;
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

        btnGenerate.Enabled =
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

    private void BtnSettings_Click(object sender, EventArgs e)
    {
        using var scope = _serviceProvider.CreateScope();

        var frm = scope.ServiceProvider.GetRequiredService<SettingsForm>();
        
        if (frm.ShowDialog(this) == DialogResult.OK)
        {
            LoadSettings(cmbProjectName.Text);

            _workspace = _cache.GetWorkspace();
        }

    }


    private void BtnGrpcGeneration_Click(object sender, EventArgs e)
    {
        using var scope = _serviceProvider.CreateScope();

        var frm = scope.ServiceProvider.GetRequiredService<GrpcGenerationForm>();

        frm.ShowDialog(this);

    }



    // ----------------------------------------------------
    // GENERATION PIPELINE
    // ----------------------------------------------------
    private async void BtnGenerate_Click(object sender, EventArgs e)
    {
        if (_workspace is null)
        {
            MessageBox.Show("Workspace is not ready yet.");
            return;
        }

        string useCaseName = txtUseCaseName.Text.Trim();
        string route = txtRoute.Text.Trim();

        RequestType type = Enum.Parse<RequestType>(cmbType.SelectedItem?.ToString() ?? "Command");
        ResponseType responseType = Enum.Parse<ResponseType>(cmbResponseType.SelectedItem?.ToString() ?? "Single");
        HttpVerb httpVerb = Enum.Parse<HttpVerb>(cmbVerb.SelectedItem?.ToString()?.ToUpper() ?? "POST");

        bool hasRequest = chkHasRequest.Checked;
        bool hasResponse = chkHasResponse.Checked;

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
            UseWaitCursor = true;

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
            using var previewForm = new PreviewForm(_workspace, previewFiles);

            if (previewForm.ShowDialog() != DialogResult.OK)
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
            UseWaitCursor = false;
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

    private void BtnExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void ChkHasResponse_CheckedChanged(object sender, EventArgs e)
    {
        if (cmbType.SelectedItem == null)
            return;

        var type = (RequestType)Enum.Parse(typeof(RequestType), cmbType.SelectedItem.ToString()!);

        if (type == RequestType.Command)
        {
            if (chkHasResponse.Checked)
            {
                cmbResponseType.Enabled = false;
                cmbResponseType.SelectedItem = ResponseType.Single.ToString();
            }
            else
            {
                cmbResponseType.Enabled = false;
                cmbResponseType.SelectedItem = null;
            }
        }

    }

    private void CmbType_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbType.SelectedItem == null)
            return;

        var type = Enum.Parse<RequestType>(cmbType.SelectedItem.ToString()!);

        switch (type)
        {
            case RequestType.Command:
                {
                    // Command may or may not return data
                    chkHasResponse.Enabled = true;

                    if (!chkHasResponse.Checked)
                    {
                        cmbResponseType.Enabled = false;
                        cmbResponseType.Visible = false;
                    }
                    else
                    {
                        cmbResponseType.Visible = true;
                        cmbResponseType.Enabled = false;
                        cmbResponseType.SelectedItem = ResponseType.Single.ToString();
                    }

                    break;
                }

            case RequestType.Query:
                {
                    chkHasResponse.Checked = true;
                    chkHasResponse.Enabled = false;

                    cmbResponseType.Enabled = true;

                    if (cmbResponseType.SelectedItem == null)
                        cmbResponseType.SelectedIndex = 0;

                    break;
                }
        }
    }
    private async void MenuUpdateEnums_Click(object? sender, EventArgs e)
    {
        try
        {
            menuUpdateEnums.Enabled = false;
            Cursor = Cursors.WaitCursor;

            await Task.Run(UpdateEnumsInSolution);

            MessageBox.Show(
                "Enum Display attributes updated successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            menuUpdateEnums.Enabled = true;
            Cursor = Cursors.Default;
        }
    }
    private void UpdateEnumsInSolution()
    {
        var solutionPath = Properties.Settings.Default.SolutionPath;

        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            throw new InvalidOperationException("Solution path is not configured.");

        var srcPath = Path.Combine(Directory.GetParent(_slnPath)?.FullName!, _solutionName, _projectName, "src");

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
    private void MenuUpdateResx_Click(object sender, EventArgs e)
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

    private void CmbProjectName_SelectedIndexChanged(object sender, EventArgs e)
    {
        var item = cmbProjectName.SelectedItem?.ToString();
        if (item != null)
        {
            LoadSettings(item);
        }

    }

    private void btnGenerate_EnabledChanged(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            if (button.Enabled)
            {
                button.BackColor = Color.FromArgb(0, 122, 204);
                button.ForeColor = Color.White;
            }
            else
            {
                button.BackColor = Color.LightGray;
                button.ForeColor = Color.DarkGray;
                MessageBox.Show("Solution path is not configured.", "sln Path Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}