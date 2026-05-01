namespace FileCreator;

partial class FileCreatorForm
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuStrip;
    private ToolStripMenuItem menuFile;
    private ToolStripMenuItem menuSettings;
    private ToolStripMenuItem menuExit;
    private ToolStripMenuItem menuUpdateResx;
    private ToolStripMenuItem menuUpdateEnums;
    private ToolStripComboBox cmbProjectName;

    private TextBox txtUseCaseGroup;
    private TextBox txtUseCaseName;
    private ComboBox cmbType;

    private CheckBox chkHasRequest;
    private CheckBox chkHasResponse;

    private ComboBox cmbVerb;
    private TextBox txtRoute;
    private ComboBox cmbResponseType;

    private Button btnGenerate;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        menuStrip = new MenuStrip();
        menuFile = new ToolStripMenuItem();
        menuSettings = new ToolStripMenuItem();
        menuUpdateEnums = new ToolStripMenuItem();
        menuUpdateResx = new ToolStripMenuItem();
        menuExit = new ToolStripMenuItem();
        cmbProjectName = new ToolStripComboBox();
        txtUseCaseGroup = new TextBox();
        txtUseCaseName = new TextBox();
        cmbType = new ComboBox();
        chkHasRequest = new CheckBox();
        chkHasResponse = new CheckBox();
        cmbVerb = new ComboBox();
        txtRoute = new TextBox();
        btnGenerate = new Button();
        cmbResponseType = new ComboBox();
        menuStrip.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip
        // 
        menuStrip.BackColor = Color.FromArgb(45, 45, 48);
        menuStrip.ForeColor = Color.Gainsboro;
        menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, cmbProjectName });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(455, 27);
        menuStrip.TabIndex = 0;
        // 
        // menuFile
        // 
        menuFile.BackColor = Color.FromArgb(45, 45, 48);
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuSettings, menuUpdateEnums, menuUpdateResx, menuExit });
        menuFile.ForeColor = Color.Gainsboro;
        menuFile.Name = "menuFile";
        menuFile.Size = new Size(37, 23);
        menuFile.Text = "File";
        // 
        // menuSettings
        // 
        menuSettings.Name = "menuSettings";
        menuSettings.Size = new Size(242, 22);
        menuSettings.Text = "Settings...";
        menuSettings.Click += BtnSettings_Click;

        // 
        // menuUpdateEnums
        // 
        menuUpdateEnums.Name = "menuUpdateEnums";
        menuUpdateEnums.Size = new Size(242, 22);
        menuUpdateEnums.Text = "Update Enum Display Attributes";
        menuUpdateEnums.Click += MenuUpdateEnums_Click;
        // 
        // menuUpdateResx
        // 
        menuUpdateResx.Name = "menuUpdateResx";
        menuUpdateResx.Size = new Size(242, 22);
        menuUpdateResx.Text = "Sync Resx Keys";
        menuUpdateResx.Click += MenuUpdateResx_Click;
        // 
        // menuExit
        // 
        menuExit.Name = "menuExit";
        menuExit.Size = new Size(242, 22);
        menuExit.Text = "Exit";
        menuExit.Click += BtnExit_Click;
        // 
        // cmbProjectName
        // 
        cmbProjectName.BackColor = Color.FromArgb(45, 45, 48);
        cmbProjectName.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbProjectName.ForeColor = Color.Gainsboro;
        cmbProjectName.Items.AddRange(new object[] { "UserManagement","IdentityServer", "FileStore", "AuditLogging", "TicketManagement" });
        cmbProjectName.Name = "cmbProjectName";
        cmbProjectName.Size = new Size(121, 23);
        cmbProjectName.SelectedIndexChanged += CmbProjectName_SelectedIndexChanged;
        // 
        // txtUseCaseGroup
        // 
        txtUseCaseGroup.BackColor = Color.FromArgb(45, 45, 48);
        txtUseCaseGroup.BorderStyle = BorderStyle.FixedSingle;
        txtUseCaseGroup.ForeColor = Color.Gainsboro;
        txtUseCaseGroup.Location = new Point(12, 40);
        txtUseCaseGroup.Name = "txtUseCaseGroup";
        txtUseCaseGroup.PlaceholderText = "Use Case Group";
        txtUseCaseGroup.Size = new Size(200, 23);
        txtUseCaseGroup.TabIndex = 1;
        // 
        // txtUseCaseName
        // 
        txtUseCaseName.BackColor = Color.FromArgb(45, 45, 48);
        txtUseCaseName.BorderStyle = BorderStyle.FixedSingle;
        txtUseCaseName.ForeColor = Color.Gainsboro;
        txtUseCaseName.Location = new Point(220, 40);
        txtUseCaseName.Name = "txtUseCaseName";
        txtUseCaseName.PlaceholderText = "Use Case Name";
        txtUseCaseName.Size = new Size(220, 23);
        txtUseCaseName.TabIndex = 2;
        // 
        // cmbType
        // 
        cmbType.BackColor = Color.FromArgb(45, 45, 48);
        cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbType.FlatStyle = FlatStyle.Flat;
        cmbType.ForeColor = Color.Gainsboro;
        cmbType.Items.AddRange(new object[] { "Command", "Query" });
        cmbType.Location = new Point(12, 75);
        cmbType.Name = "cmbType";
        cmbType.Size = new Size(121, 23);
        cmbType.TabIndex = 3;
        cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;
        // 
        // chkHasRequest
        // 
        chkHasRequest.BackColor = Color.FromArgb(30, 30, 30);
        chkHasRequest.Checked = true;
        chkHasRequest.CheckState = CheckState.Checked;
        chkHasRequest.ForeColor = Color.Gainsboro;
        chkHasRequest.Location = new Point(150, 75);
        chkHasRequest.Name = "chkHasRequest";
        chkHasRequest.Size = new Size(104, 24);
        chkHasRequest.TabIndex = 4;
        chkHasRequest.Text = "Request";
        chkHasRequest.UseVisualStyleBackColor = false;
        // 
        // chkHasResponse
        // 
        chkHasResponse.BackColor = Color.FromArgb(30, 30, 30);
        chkHasResponse.Checked = true;
        chkHasResponse.CheckState = CheckState.Checked;
        chkHasResponse.ForeColor = Color.Gainsboro;
        chkHasResponse.Location = new Point(280, 75);
        chkHasResponse.Name = "chkHasResponse";
        chkHasResponse.Size = new Size(104, 24);
        chkHasResponse.TabIndex = 5;
        chkHasResponse.Text = "Response";
        chkHasResponse.UseVisualStyleBackColor = false;
        chkHasResponse.CheckedChanged += ChkHasResponse_CheckedChanged;
        // 
        // cmbVerb
        // 
        cmbVerb.BackColor = Color.FromArgb(45, 45, 48);
        cmbVerb.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVerb.FlatStyle = FlatStyle.Flat;
        cmbVerb.ForeColor = Color.Gainsboro;
        cmbVerb.Items.AddRange(new object[] { "GET", "POST", "PUT", "DELETE", "PATCH" });
        cmbVerb.Location = new Point(12, 110);
        cmbVerb.Name = "cmbVerb";
        cmbVerb.Size = new Size(121, 23);
        cmbVerb.TabIndex = 6;
        // 
        // txtRoute
        // 
        txtRoute.BackColor = Color.FromArgb(45, 45, 48);
        txtRoute.BorderStyle = BorderStyle.FixedSingle;
        txtRoute.ForeColor = Color.Gainsboro;
        txtRoute.Location = new Point(150, 110);
        txtRoute.Name = "txtRoute";
        txtRoute.PlaceholderText = "tests/create";
        txtRoute.Size = new Size(124, 23);
        txtRoute.TabIndex = 7;
        // 
        // btnGenerate
        // 
        btnGenerate.BackColor = Color.FromArgb(0, 122, 204);
        btnGenerate.FlatStyle = FlatStyle.Flat;
        btnGenerate.ForeColor = Color.White;
        btnGenerate.Location = new Point(12, 150);
        btnGenerate.Name = "btnGenerate";
        btnGenerate.Size = new Size(428, 32);
        btnGenerate.TabIndex = 9;
        btnGenerate.Enabled = false;
        btnGenerate.Text = "Generate UseCase + Endpoint Files";
        btnGenerate.UseVisualStyleBackColor = false;
        btnGenerate.EnabledChanged += btnGenerate_EnabledChanged;
        btnGenerate.Click += BtnGenerate_Click;
        // 
        // cmbResponseType
        // 
        cmbResponseType.BackColor = Color.FromArgb(45, 45, 48);
        cmbResponseType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbResponseType.Enabled = false;
        cmbResponseType.FlatStyle = FlatStyle.Flat;
        cmbResponseType.ForeColor = Color.Gainsboro;
        cmbResponseType.Items.AddRange(new object[] { "Single", "IEnumerable", "KeyValuePair", "PagedList" });
        cmbResponseType.Location = new Point(280, 110);
        cmbResponseType.Name = "cmbResponseType";
        cmbResponseType.Size = new Size(160, 23);
        cmbResponseType.TabIndex = 8;
        // 
        // FileCreatorForm
        // 
        BackColor = Color.FromArgb(30, 30, 30);
        ClientSize = new Size(455, 205);
        Controls.Add(menuStrip);
        Controls.Add(txtUseCaseGroup);
        Controls.Add(txtUseCaseName);
        Controls.Add(cmbType);
        Controls.Add(chkHasRequest);
        Controls.Add(chkHasResponse);
        Controls.Add(cmbVerb);
        Controls.Add(txtRoute);
        Controls.Add(cmbResponseType);
        Controls.Add(btnGenerate);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.Gainsboro;
        FormBorderStyle = FormBorderStyle.None;
        MainMenuStrip = menuStrip;
        MaximizeBox = false;
        Name = "FileCreatorForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "UseCase & Endpoint File Creator";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
    private void SetDefaultValue()
    {
        cmbProjectName.SelectedIndex = 2;
        txtUseCaseGroup.Text = "ErrorLog";
        txtUseCaseName.Text = "Create";
        txtRoute.Text = "error-logs";
        cmbType.SelectedIndex = 0;
        cmbVerb.SelectedIndex = 0;
        cmbResponseType.SelectedIndex = 0;
        chkHasRequest.Checked = true;
        chkHasResponse.Checked = true;
    }

}
