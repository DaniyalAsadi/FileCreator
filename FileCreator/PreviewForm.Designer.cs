namespace FileCreator;

partial class PreviewForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        tabs = new TabControl();
        btnOk = new Button();
        btnCancel = new Button();
        panel = new Panel();
        panel.SuspendLayout();
        SuspendLayout();
        // 
        // tabs
        // 
        tabs.Dock = DockStyle.Fill;
        tabs.Location = new Point(0, 0);
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(1084, 661);
        tabs.TabIndex = 0;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.BackColor = Color.FromArgb(0, 122, 204);
        btnOk.FlatStyle = FlatStyle.Flat;
        btnOk.ForeColor = Color.White;
        btnOk.Location = new Point(864, 10);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(100, 32);
        btnOk.TabIndex = 0;
        btnOk.Text = "Generate";
        btnOk.UseVisualStyleBackColor = false;
        btnOk.Click += BtnOk_Click;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.BackColor = Color.FromArgb(70, 70, 70);
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.ForeColor = Color.White;
        btnCancel.Location = new Point(974, 10);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(100, 32);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = false;
        btnCancel.Click += BtnCancel_Click;
        // 
        // panel
        // 
        panel.Controls.Add(btnOk);
        panel.Controls.Add(btnCancel);
        panel.Dock = DockStyle.Bottom;
        panel.Location = new Point(0, 661);
        panel.Name = "panel";
        panel.Padding = new Padding(10);
        panel.Size = new Size(1084, 50);
        panel.TabIndex = 1;
        panel.BackColor = Color.FromArgb(30, 30, 30);
        panel.ForeColor = Color.Gainsboro;
        panel.Resize += panel_Resize;
        // 
        // PreviewForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(30, 30, 30);
        ClientSize = new Size(1084, 711);
        Controls.Add(tabs);
        Controls.Add(panel);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.Gainsboro;
        Name = "PreviewForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Preview Generated Files";

        // برای اولین بار هم موقع Initialize فرم Location ست شود
        btnCancel.Location = new Point(panel.Width - btnCancel.Width - 10, 10);
        btnOk.Location = new Point(btnCancel.Left - btnOk.Width - 10, 10);
        panel.ResumeLayout(false);
        ResumeLayout(false);

    }

    #endregion

    private Panel panel;
    private TabControl tabs;
    private Button btnOk;
    private Button btnCancel;

    // Event handlers
    private void BtnOk_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }
}