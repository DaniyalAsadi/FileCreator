namespace FileCreator;

partial class SettingsForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    private TextBox txtSolutionPath;
    private Button btnBrowse;
    private Button btnSave;
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
        this.txtSolutionPath = new TextBox();
        this.btnBrowse = new Button();
        this.btnSave = new Button();
        this.SuspendLayout();

        // txtSolutionPath
        this.txtSolutionPath.Location = new System.Drawing.Point(12, 12);
        this.txtSolutionPath.Size = new System.Drawing.Size(300, 23);
        this.txtSolutionPath.ReadOnly = true;

        // btnBrowse
        this.btnBrowse.Location = new System.Drawing.Point(320, 12);
        this.btnBrowse.Size = new System.Drawing.Size(75, 23);
        this.btnBrowse.Text = "Browse";
        this.btnBrowse.Click += new EventHandler(this.BtnBrowse_Click);

        // btnSave
        this.btnSave.Location = new System.Drawing.Point(12, 50);
        this.btnSave.Size = new System.Drawing.Size(383, 30);
        this.btnSave.Text = "Save";
        this.btnSave.Click += new EventHandler(this.BtnSave_Click);

        // SettingsForm
        this.ClientSize = new System.Drawing.Size(407, 100);
        this.Controls.Add(this.txtSolutionPath);
        this.Controls.Add(this.btnBrowse);
        this.Controls.Add(this.btnSave);
        this.Text = "Settings";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}