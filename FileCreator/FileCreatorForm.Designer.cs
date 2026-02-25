namespace FileCreator;

partial class FileCreatorForm
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuStrip;
    private ToolStripMenuItem menuFile;
    private ToolStripMenuItem menuSettings;
    private ToolStripMenuItem menuExit;

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
        menuExit = new ToolStripMenuItem();
        txtUseCaseGroup = new TextBox();
        txtUseCaseName = new TextBox();
        cmbType = new ComboBox();
        chkHasRequest = new CheckBox();
        chkHasResponse = new CheckBox();
        cmbVerb = new ComboBox();
        txtRoute = new TextBox();
        btnGenerate = new Button();
        cmbResponseType = new ComboBox();
        
        // 
        // menuStrip
        // 
        menuStrip.Items.AddRange(new ToolStripItem[] { menuFile });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(455, 24);
        menuStrip.TabIndex = 0;
        menuStrip.BackColor = Color.FromArgb(45, 45, 48);
        menuStrip.ForeColor = Color.Gainsboro;
        // 
        // menuFile
        // 
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuSettings, menuExit });
        menuFile.Name = "menuFile";
        menuFile.Size = new Size(37, 20);
        menuFile.Text = "File";
        menuFile.BackColor = Color.FromArgb(45, 45, 48);
        menuFile.ForeColor = Color.Gainsboro;
        // 
        // menuSettings
        // 
        menuSettings.Name = "menuSettings";
        menuSettings.Size = new Size(125, 22);
        menuSettings.Text = "Settings...";
        menuSettings.Click += BtnSettings_Click;
        // 
        // menuExit
        // 
        menuExit.Name = "menuExit";
        menuExit.Size = new Size(125, 22);
        menuExit.Text = "Exit";
        menuExit.Click += BtnExit_Click;
        // 
        // txtUseCaseGroup
        // 
        txtUseCaseGroup.Location = new Point(12, 40);
        txtUseCaseGroup.Name = "txtUseCaseGroup";
        txtUseCaseGroup.PlaceholderText = "Use Case Group";
        txtUseCaseGroup.Size = new Size(200, 23);
        txtUseCaseGroup.TabIndex = 1;
        txtUseCaseGroup.ForeColor = Color.Gainsboro;
        txtUseCaseGroup.BackColor = Color.FromArgb(45, 45, 48);
        txtUseCaseGroup.BorderStyle = BorderStyle.FixedSingle;
        // 
        // txtUseCaseName
        // 
        txtUseCaseName.Location = new Point(220, 40);
        txtUseCaseName.Name = "txtUseCaseName";
        txtUseCaseName.PlaceholderText = "Use Case Name";
        txtUseCaseName.Size = new Size(220, 23);
        txtUseCaseName.TabIndex = 2;
        txtUseCaseName.ForeColor = Color.Gainsboro;
        txtUseCaseName.BackColor = Color.FromArgb(45, 45, 48);
        txtUseCaseName.BorderStyle = BorderStyle.FixedSingle;
        // 
        // cmbType
        // 
        cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbType.Items.AddRange(new object[] { "Command", "Query" });
        cmbType.Location = new Point(12, 75);
        cmbType.Name = "cmbType";
        cmbType.Size = new Size(121, 23);
        cmbType.SelectedIndex = 0;
        cmbType.TabIndex = 3;
        cmbType.ForeColor = Color.Gainsboro;
        cmbType.BackColor = Color.FromArgb(45, 45, 48);
        cmbType.FlatStyle = FlatStyle.Flat;
        cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;
        // 
        // chkHasRequest
        // 
        chkHasRequest.Checked = true;
        chkHasRequest.CheckState = CheckState.Checked;
        chkHasRequest.Location = new Point(150, 75);
        chkHasRequest.Name = "chkHasRequest";
        chkHasRequest.Size = new Size(104, 24);
        chkHasRequest.TabIndex = 4;
        chkHasRequest.ForeColor = Color.Gainsboro;
        chkHasRequest.BackColor = Color.FromArgb(30, 30, 30);
        chkHasRequest.Text = "Request";
        // 
        // chkHasResponse
        // 
        chkHasResponse.Checked = true;
        chkHasResponse.CheckState = CheckState.Checked;
        chkHasResponse.Location = new Point(280, 75);
        chkHasResponse.Name = "chkHasResponse";
        chkHasResponse.Size = new Size(104, 24);
        chkHasResponse.TabIndex = 5;
        chkHasResponse.Text = "Response";
        chkHasResponse.ForeColor = Color.Gainsboro;
        chkHasResponse.BackColor = Color.FromArgb(30, 30, 30);
        chkHasResponse.CheckedChanged += ChkHasResponse_CheckedChanged;
        // 
        // cmbVerb
        // 
        cmbVerb.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVerb.Items.AddRange(new object[] { "GET", "POST", "PUT", "DELETE", "PATCH" });
        cmbVerb.Location = new Point(12, 110);
        cmbVerb.Name = "cmbVerb";
        cmbVerb.Size = new Size(121, 23);
        cmbVerb.TabIndex = 6;
        cmbVerb.ForeColor = Color.Gainsboro;
        cmbVerb.BackColor = Color.FromArgb(45, 45, 48);
        cmbVerb.FlatStyle = FlatStyle.Flat;
        cmbVerb.SelectedIndex = 0;
        // 
        // txtRoute
        // 
        txtRoute.Location = new Point(150, 110);
        txtRoute.Name = "txtRoute";
        txtRoute.PlaceholderText = "tests/create";
        txtRoute.Size = new Size(124, 23);
        txtRoute.TabIndex = 7;
        txtRoute.ForeColor = Color.Gainsboro;
        txtRoute.BackColor = Color.FromArgb(45, 45, 48);
        txtRoute.BorderStyle = BorderStyle.FixedSingle;
        // 
        // cmbResponseType
        // 
        cmbResponseType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbResponseType.Items.AddRange(new object[] { "Single", "IEnumerable", "PagedList" });
        cmbResponseType.Location = new Point(280, 110);
        cmbResponseType.Name = "cmbResponseType";
        cmbResponseType.Size = new Size(160, 23);
        cmbResponseType.TabIndex = 8;
        cmbResponseType.SelectedIndex = 0;
        cmbResponseType.ForeColor = Color.Gainsboro;
        cmbResponseType.BackColor = Color.FromArgb(45, 45, 48);
        cmbResponseType.FlatStyle = FlatStyle.Flat;
        cmbResponseType.Enabled = false;
        cmbResponseType.Visible = true;
        // 
        // btnGenerate
        // 
        btnGenerate.Location = new Point(12, 150);
        btnGenerate.Name = "btnGenerate";
        btnGenerate.Size = new Size(428, 32);
        btnGenerate.TabIndex = 9;
        btnGenerate.Text = "Generate UseCase + Endpoint Files";
        btnGenerate.Click += BtnGenerate_Click;
        btnGenerate.ForeColor = Color.White;
        btnGenerate.BackColor = Color.FromArgb(0, 122, 204);
        btnGenerate.FlatStyle = FlatStyle.Flat;
        // 
        // FileCreatorForm
        // 
        Controls.Add(menuStrip);
        Controls.Add(txtUseCaseGroup);
        Controls.Add(txtUseCaseName);
        Controls.Add(cmbType);
        Controls.Add(chkHasRequest);
        Controls.Add(chkHasResponse);
        Controls.Add(cmbVerb);
        Controls.Add(txtRoute);
        Controls.Add(btnGenerate);
        Controls.Add(cmbResponseType);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9);
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(455, 205);
        FormBorderStyle = FormBorderStyle.None;
        MainMenuStrip = menuStrip;
        MaximizeBox = false;
        Name = "FileCreatorForm";
        Text = "UseCase & Endpoint File Creator";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
        SetDefaultValue();
        menuStrip.SuspendLayout();
        SuspendLayout();

    }
    private void SetDefaultValue()
    {
        txtUseCaseGroup.Text = "Authentication";
        txtUseCaseName.Text = "Create";
        txtRoute.Text = "tests/create";
        cmbType.SelectedIndex = 0;
        cmbVerb.SelectedIndex = 0;
        cmbResponseType.SelectedIndex = 0;
        chkHasRequest.Checked = true;
        chkHasResponse.Checked = true;

    }
}
