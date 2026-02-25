using FileCreator.Core;
using FileCreator.Core.Rewriter;
using System.Reflection.Emit;
namespace FileCreator;

public partial class FileCreatorForm : Form
{
    private PreviewWorkspace? _workspace;

    private string _slnPath = string.Empty;
    private string _solutionName = string.Empty;
    private string _useCasesBasePath = string.Empty;
    private string _webBasePath = string.Empty;
    private string _functionalTestsBasePath = string.Empty;
    private string _unitTestsBasePath = string.Empty;
    private string _sharedKerbalTestsBasePath = string.Empty;
    private string _infrastructureBasePath = string.Empty;

    public FileCreatorForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    // ----------------------------------------------------
    // Workspace Initialization (ONLY ONCE)
    // ----------------------------------------------------
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if(string.IsNullOrWhiteSpace(_slnPath))
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
    private void LoadSettings()
    {
        _slnPath = Properties.Settings.Default.SolutionPath;
        _solutionName = Path.GetFileNameWithoutExtension(_slnPath);

        _useCasesBasePath = Properties.Settings.Default.UseCasesPath;
        _webBasePath = Properties.Settings.Default.WebPath;
        _functionalTestsBasePath = Properties.Settings.Default.FunctionalTestPath;
        _unitTestsBasePath = Properties.Settings.Default.UnitTestPath;
        _sharedKerbalTestsBasePath = Properties.Settings.Default.SharedKernelPath;
        _infrastructureBasePath = Properties.Settings.Default.InfrastructurePath;

        btnGenerate.Enabled =
            !string.IsNullOrWhiteSpace(_useCasesBasePath) ||
            !string.IsNullOrWhiteSpace(_webBasePath);
    }

    private void BtnSettings_Click(object sender, EventArgs e)
    {
        using var frm = new SettingsForm();
        frm.ShowDialog();
        LoadSettings();
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
                solutionName: _solutionName,
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
}