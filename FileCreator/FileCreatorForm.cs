using FileCreator.Core;
using FileCreator.Core.Rewriter;
using FileCreator.Core.Walker;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Newtonsoft.Json;
using System.Reflection.Emit;
using static System.Net.Mime.MediaTypeNames;
namespace FileCreator;

public partial class FileCreatorForm : Form
{
    private PreviewWorkspace? _workspace;

    private string _slnPath = string.Empty;
    private string _projectName = string.Empty;
    private string _solutionName = string.Empty;
    private string _useCasesBasePath = string.Empty;
    private string _webBasePath = string.Empty;
    private string _functionalTestsBasePath = string.Empty;
    private string _unitTestsBasePath = string.Empty;
    private string _sharedKerbalTestsBasePath = string.Empty;
    private string _infrastructureBasePath = string.Empty;
    private string _localizationBasePath = string.Empty;
    public FileCreatorForm()
    {
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

            _workspace = WorkspaceCache.GetWorkspace(_slnPath);

            // Warmup Roslyn (build graph, load metadata, etc.)
            await _workspace.WarmupAsync();
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
        Dictionary<string, string> projects = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.ProjectPathes) ?? [];
        _slnPath = Properties.Settings.Default.SolutionPath;
        _solutionName = Path.GetFileNameWithoutExtension(_slnPath);
        _projectName = projectName;
        string solutionFolder = Path.GetDirectoryName(_slnPath)!;
        

        _useCasesBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault($"{projectName}.UseCases") ?? string.Empty)??string.Empty);
        _webBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault($"{projectName}.Web") ?? string.Empty)??string.Empty);
        _functionalTestsBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault($"{projectName}.FunctionalTests") ?? string.Empty)??string.Empty);
        _unitTestsBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault($"{projectName}.UnitTests") ?? string.Empty)??string.Empty);
        _infrastructureBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault($"{projectName}.Infrastructure") ?? string.Empty)??string.Empty);
        _sharedKerbalTestsBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault("SharedKernel") ?? string.Empty)??string.Empty);
        _localizationBasePath = Path.Combine(solutionFolder, Path.GetDirectoryName(projects.GetValueOrDefault("Localization") ?? string.Empty)??string.Empty);
        btnGenerate.Enabled =
            !string.IsNullOrWhiteSpace(_slnPath) &&
            !string.IsNullOrWhiteSpace(_useCasesBasePath) &&
            !string.IsNullOrWhiteSpace(_webBasePath) &&
            !string.IsNullOrWhiteSpace(_functionalTestsBasePath) &&
            !string.IsNullOrWhiteSpace(_unitTestsBasePath) &&
            !string.IsNullOrWhiteSpace(_sharedKerbalTestsBasePath) &&
            !string.IsNullOrWhiteSpace(_infrastructureBasePath) &&
            !string.IsNullOrWhiteSpace(_localizationBasePath);

    }

    private void BtnSettings_Click(object sender, EventArgs e)
    {
        using var frm = new SettingsForm();
        frm.ShowDialog();
        LoadSettings(cmbProjectName.Text);
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
                useCasePath: _useCasesBasePath,
                webPath: _webBasePath,
                functionalTestPath: _functionalTestsBasePath,
                unitTestPath: _unitTestsBasePath,
                infrastructurePath: _infrastructureBasePath,
                hasRequest: hasRequest,
                requestType: type,
                hasResponse: hasResponse,
                responseType: responseType,
                httpVerb: httpVerb);

            var previewFiles = generator.GeneratePreview();

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

            var apiRoutePath = FindApiRoutes(_sharedKerbalTestsBasePath);
            ApiRoutesUpdater.Update(
                apiRoutePath,
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
            var resxPathes = Directory.GetFiles(Path.Combine(_localizationBasePath,
                "Resources"), "*.resx");
            var resxDesignPath = Directory.GetFiles(Path.Combine(_localizationBasePath,
                "Resources"), "*.cs").First();

            if (resxPathes.Length == 0 || !File.Exists(resxPathes.First()))
            {
                MessageBox.Show("Resx file not found.");
                return;
            }

            var collector = new EnumKeyCollector();
            var srcPath = Path.Combine(Directory.GetParent(_slnPath)?.FullName!, _solutionName,_projectName, "src");

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
            }
        }
    }
}