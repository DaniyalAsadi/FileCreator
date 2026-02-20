using FileCreator.Core;
using FileCreator.Core.Rewriter;
namespace FileCreator;

public partial class FileCreatorForm : Form
{
    private string _solutionName = string.Empty;
    private string _useCasesBasePath = string.Empty;
    private string _webBasePath = string.Empty;
    private string _functionalTestsBasePath = string.Empty;
    private string _unitTestsBasePath = string.Empty;
    private string _sharedKerbalTestsBasePath = string.Empty;   
    public FileCreatorForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    // ----------------------------------------------------
    // SETTINGS
    // ----------------------------------------------------
    private void LoadSettings()
    {
        _solutionName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.SolutionPath);
        _useCasesBasePath = Properties.Settings.Default.UseCasesPath;
        _webBasePath = Properties.Settings.Default.WebPath;
        _functionalTestsBasePath = Properties.Settings.Default.FunctionalTestPath;
        _unitTestsBasePath = Properties.Settings.Default.UnitTestPath;
        _sharedKerbalTestsBasePath = Properties.Settings.Default.SharedKernelPath;

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
    // GENERATION
    // ----------------------------------------------------
    private void BtnGenerate_Click(object sender, EventArgs e)
    {
        string useCaseName = txtUseCaseName.Text.Trim();
        string route = txtRoute.Text.Trim();
        RequestType type = Enum.Parse<RequestType>(cmbType.SelectedItem?.ToString() ?? "Command");
        ResponseType responseType = Enum.Parse<ResponseType>(cmbResponseType.SelectedItem?.ToString() ?? "Single");

        HttpVerb httpVerb = Enum.Parse<HttpVerb>(cmbVerb.SelectedItem?.ToString()?.ToUpper() ?? "POST") ;

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

        if (string.IsNullOrWhiteSpace(_useCasesBasePath))
        {
            MessageBox.Show("Settings not configured.");
            return;
        }
        if (type == RequestType.Command && responseType is ResponseType.IEnumerable or ResponseType.PagedList)
        {
            MessageBox.Show("Command cannot return collections.");
            return;
        }
        if (type == RequestType.Query && !hasResponse)
        {
            MessageBox.Show("Query Must Have Response");
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
            MessageBox.Show($"{txtUseCaseGroup.Text} Must be Singluare");
            return;
        }
        GroupName group = GroupName.Create(txtUseCaseGroup.Text.Trim());


        try
        {

            var roslynFileCreator = new RoslynFileCreator(
                SolutionName: _solutionName,
                group,
                useCaseName,
                _useCasesBasePath,
                _webBasePath,
                _functionalTestsBasePath,
                _unitTestsBasePath,
                hasRequest,
                type,
                hasResponse,
                responseType,
                httpVerb);
            roslynFileCreator.Generate();


            ;
            var apiRoutePath = FindApiRoutes(_sharedKerbalTestsBasePath);
            ApiRoutesUpdater.Update(
                apiRoutePath,
                group.Resource,
                useCaseName,
                httpVerb,
                route);


            MessageBox.Show("Files generated successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error");
        }
    }


    private void BtnExit_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    public static string FindApiRoutes(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException(rootDirectory);

        var file = Directory
            .EnumerateFiles(rootDirectory, "ApiRoutes.cs", SearchOption.AllDirectories)
            .FirstOrDefault();

        return file is null ? throw new FileNotFoundException("ApiRoutes.cs not found.") : file;
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