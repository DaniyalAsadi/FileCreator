using FileCreator.Grpc.ViewModels;
using FileCreator.Services;

namespace FileCreator;

partial class GrpcGenerationForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private TextBox txtNamespace ;
    private TextBox txtOutputFolder ;
    private TextBox txtEndpointFilter ;

    private CheckBox chkGenerateAll;
    private CheckBox chkInternalOnly;
    private CheckBox chkDryRun;
    private CheckBox chkForce;
    private CheckBox chkStrict;

    private DataGridView dgvEndpoints;

    private Button btnGenerateGrpc;

    private void ConfigureEndpointGrid()
    {
        dgvEndpoints.AutoGenerateColumns = false;
        dgvEndpoints.Columns.Clear();

        var selectAllHeader = new DataGridViewCheckBoxHeaderCell();

        selectAllHeader.CheckedChanged += (_, _) =>
        {
            SelectAllEndpoints(selectAllHeader.Checked);
        };

        var selectionColumn = new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(EndpointSelectionItem.Selected),
            HeaderText = "",
            Width = 40,
            HeaderCell = selectAllHeader
        };

        dgvEndpoints.Columns.Add(selectionColumn);

        dgvEndpoints.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(EndpointSelectionItem.Name),
                HeaderText = "Endpoint",
                Width = 200
            });

        dgvEndpoints.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(EndpointSelectionItem.HttpVerb),
                HeaderText = "Verb",
                Width = 80
            });

        dgvEndpoints.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(EndpointSelectionItem.Route),
                HeaderText = "Route",
                Width = 300
            });

        dgvEndpoints.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(EndpointSelectionItem.ResponseType),
                HeaderText = "Response",
                Width = 150
            });
    }
    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        txtNamespace = new TextBox();
        txtOutputFolder = new TextBox();
        txtEndpointFilter = new TextBox();
        chkGenerateAll = new CheckBox();
        chkInternalOnly = new CheckBox();
        chkDryRun = new CheckBox();
        chkForce = new CheckBox();
        chkStrict = new CheckBox();
        dgvEndpoints = new DataGridView();
        btnGenerateGrpc = new Button();
        ((System.ComponentModel.ISupportInitialize)dgvEndpoints).BeginInit();
        SuspendLayout();
        // 
        // txtNamespace
        // 
        txtNamespace.Location = new Point(20, 30);
        txtNamespace.Name = "txtNamespace";
        txtNamespace.PlaceholderText = "Namespace";
        txtNamespace.Size = new Size(420, 23);
        txtNamespace.TabIndex = 0;
        // 
        // txtOutputFolder
        // 
        txtOutputFolder.Location = new Point(20, 75);
        txtOutputFolder.Name = "txtOutputFolder";
        txtOutputFolder.PlaceholderText = "Output Folder";
        txtOutputFolder.Size = new Size(420, 23);
        txtOutputFolder.TabIndex = 1;
        // 
        // txtEndpointFilter
        // 
        txtEndpointFilter.Location = new Point(20, 120);
        txtEndpointFilter.Name = "txtEndpointFilter";
        txtEndpointFilter.PlaceholderText = "Endpoint Filter (*User*)";
        txtEndpointFilter.Size = new Size(420, 23);
        txtEndpointFilter.TabIndex = 2;
        // 
        // chkGenerateAll
        // 
        chkGenerateAll.AutoSize = true;
        chkGenerateAll.Location = new Point(20, 170);
        chkGenerateAll.Name = "chkGenerateAll";
        chkGenerateAll.Size = new Size(90, 19);
        chkGenerateAll.TabIndex = 3;
        chkGenerateAll.Text = "Generate All";
        // 
        // chkInternalOnly
        // 
        chkInternalOnly.AutoSize = true;
        chkInternalOnly.Location = new Point(160, 170);
        chkInternalOnly.Name = "chkInternalOnly";
        chkInternalOnly.Size = new Size(94, 19);
        chkInternalOnly.TabIndex = 4;
        chkInternalOnly.Text = "Internal Only";
        // 
        // chkDryRun
        // 
        chkDryRun.AutoSize = true;
        chkDryRun.Location = new Point(20, 205);
        chkDryRun.Name = "chkDryRun";
        chkDryRun.Size = new Size(68, 19);
        chkDryRun.TabIndex = 5;
        chkDryRun.Text = "Dry Run";
        // 
        // chkForce
        // 
        chkForce.AutoSize = true;
        chkForce.Location = new Point(160, 205);
        chkForce.Name = "chkForce";
        chkForce.Size = new Size(55, 19);
        chkForce.TabIndex = 6;
        chkForce.Text = "Force";
        // 
        // chkStrict
        // 
        chkStrict.AutoSize = true;
        chkStrict.Location = new Point(280, 205);
        chkStrict.Name = "chkStrict";
        chkStrict.Size = new Size(53, 19);
        chkStrict.TabIndex = 7;
        chkStrict.Text = "Strict";
        // 
        // dgvEndpoints
        // 
        dgvEndpoints.Location = new Point(20, 260);
        dgvEndpoints.Name = "dgvEndpoints";
        dgvEndpoints.Size = new Size(420, 299);
        dgvEndpoints.TabIndex = 8;
        dgvEndpoints.Text = "Generate gRPC";
        // 
        // btnGenerateGrpc
        // 
        btnGenerateGrpc.Location = new Point(20, 565);
        btnGenerateGrpc.Name = "btnGenerateGrpc";
        btnGenerateGrpc.Size = new Size(420, 40);
        btnGenerateGrpc.TabIndex = 9;
        btnGenerateGrpc.Text = "Generate gRPC";
        btnGenerateGrpc.UseVisualStyleBackColor = true;
        btnGenerateGrpc.Click += BtnGenerateGrpc_Click;
        // 
        // GrpcGenerationForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(470, 615);
        Controls.Add(txtNamespace);
        Controls.Add(txtOutputFolder);
        Controls.Add(txtEndpointFilter);
        Controls.Add(chkGenerateAll);
        Controls.Add(chkInternalOnly);
        Controls.Add(chkDryRun);
        Controls.Add(chkForce);
        Controls.Add(chkStrict);
        Controls.Add(dgvEndpoints);
        Controls.Add(btnGenerateGrpc);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "GrpcGenerationForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "gRPC Scaffold Generator";
        ((System.ComponentModel.ISupportInitialize)dgvEndpoints).EndInit();
        ResumeLayout(false);
        PerformLayout();

    }
    private void SelectAllEndpoints(bool selected)
    {
        if (dgvEndpoints.DataSource is not IEnumerable<EndpointSelectionItem> endpoints)
            return;

        foreach (var endpoint in endpoints)
        {
            endpoint.Selected = selected;
        }

        dgvEndpoints.Refresh();
    }
    #endregion
}