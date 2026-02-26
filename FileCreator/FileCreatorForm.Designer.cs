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
        SuspendLayout();
        menuStrip = new MenuStrip();
        menuStrip.SuspendLayout();

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

        // -------------------------
        // menuStrip
        // -------------------------
        menuStrip.Items.AddRange(new ToolStripItem[] { menuFile });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(455, 24);
        menuStrip.TabIndex = 0;
        menuStrip.BackColor = Color.FromArgb(45, 45, 48);
        menuStrip.ForeColor = Color.Gainsboro;

        // menuFile
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuSettings, menuExit });
        menuFile.Text = "File";
        menuFile.BackColor = Color.FromArgb(45, 45, 48);
        menuFile.ForeColor = Color.Gainsboro;

        // menuSettings
        menuSettings.Text = "Settings...";
        menuSettings.Click += BtnSettings_Click;

        // menuExit
        menuExit.Text = "Exit";
        menuExit.Click += BtnExit_Click;

        // -------------------------
        // سایر کنترل‌ها (بدون تغییر منطقی)
        // -------------------------
        txtUseCaseGroup.Location = new Point(12, 40);
        txtUseCaseGroup.PlaceholderText = "Use Case Group";
        txtUseCaseGroup.Size = new Size(200, 23);
        txtUseCaseGroup.ForeColor = Color.Gainsboro;
        txtUseCaseGroup.BackColor = Color.FromArgb(45, 45, 48);
        txtUseCaseGroup.BorderStyle = BorderStyle.FixedSingle;

        txtUseCaseName.Location = new Point(220, 40);
        txtUseCaseName.PlaceholderText = "Use Case Name";
        txtUseCaseName.Size = new Size(220, 23);
        txtUseCaseName.ForeColor = Color.Gainsboro;
        txtUseCaseName.BackColor = Color.FromArgb(45, 45, 48);
        txtUseCaseName.BorderStyle = BorderStyle.FixedSingle;

        cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbType.Items.AddRange(new object[] { "Command", "Query" });
        cmbType.Location = new Point(12, 75);
        cmbType.Size = new Size(121, 23);
        cmbType.SelectedIndex = 0;
        cmbType.ForeColor = Color.Gainsboro;
        cmbType.BackColor = Color.FromArgb(45, 45, 48);
        cmbType.FlatStyle = FlatStyle.Flat;
        cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;

        chkHasRequest.Checked = true;
        chkHasRequest.Text = "Request";
        chkHasRequest.Location = new Point(150, 75);
        chkHasRequest.ForeColor = Color.Gainsboro;
        chkHasRequest.BackColor = Color.FromArgb(30, 30, 30);

        chkHasResponse.Checked = true;
        chkHasResponse.Text = "Response";
        chkHasResponse.Location = new Point(280, 75);
        chkHasResponse.ForeColor = Color.Gainsboro;
        chkHasResponse.BackColor = Color.FromArgb(30, 30, 30);
        chkHasResponse.CheckedChanged += ChkHasResponse_CheckedChanged;

        cmbVerb.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVerb.Items.AddRange(new object[] { "GET", "POST", "PUT", "DELETE", "PATCH" });
        cmbVerb.Location = new Point(12, 110);
        cmbVerb.Size = new Size(121, 23);
        cmbVerb.SelectedIndex = 0;
        cmbVerb.ForeColor = Color.Gainsboro;
        cmbVerb.BackColor = Color.FromArgb(45, 45, 48);
        cmbVerb.FlatStyle = FlatStyle.Flat;

        txtRoute.Location = new Point(150, 110);
        txtRoute.PlaceholderText = "tests/create";
        txtRoute.Size = new Size(124, 23);
        txtRoute.ForeColor = Color.Gainsboro;
        txtRoute.BackColor = Color.FromArgb(45, 45, 48);
        txtRoute.BorderStyle = BorderStyle.FixedSingle;

        cmbResponseType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbResponseType.Items.AddRange(new object[] { "Single", "IEnumerable", "PagedList" });
        cmbResponseType.Location = new Point(280, 110);
        cmbResponseType.Size = new Size(160, 23);
        cmbResponseType.SelectedIndex = 0;
        cmbResponseType.ForeColor = Color.Gainsboro;
        cmbResponseType.BackColor = Color.FromArgb(45, 45, 48);
        cmbResponseType.FlatStyle = FlatStyle.Flat;
        cmbResponseType.Enabled = false;

        btnGenerate.Location = new Point(12, 150);
        btnGenerate.Size = new Size(428, 32);
        btnGenerate.Text = "Generate UseCase + Endpoint Files";
        btnGenerate.ForeColor = Color.White;
        btnGenerate.BackColor = Color.FromArgb(0, 122, 204);
        btnGenerate.FlatStyle = FlatStyle.Flat;
        btnGenerate.Click += BtnGenerate_Click;

        // -------------------------
        // Add Controls
        // -------------------------
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

        // -------------------------
        // Form Settings
        // -------------------------
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9);
        ClientSize = new Size(455, 205);
        FormBorderStyle = FormBorderStyle.None;
        MainMenuStrip = menuStrip;
        MaximizeBox = false;
        Text = "UseCase & Endpoint File Creator";
        StartPosition = FormStartPosition.CenterScreen;
        // -------------------------
        // Resume Layout (مهم‌ترین بخش)
        // -------------------------
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();

        SetDefaultValue();
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
