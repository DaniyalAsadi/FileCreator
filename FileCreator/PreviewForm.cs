using FastColoredTextBoxNS;

namespace FileCreator;

public sealed class PreviewForm : Form
{
    private readonly IReadOnlyList<GeneratedFile> _files;
    private readonly PreviewWorkspace _workspace;

    private readonly TabControl _tabs;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public PreviewForm(PreviewWorkspace workspace, IReadOnlyList<GeneratedFile> files)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _files = files ?? throw new ArgumentNullException(nameof(files));

        Text = "Preview Generated Files";
        Width = 1100;
        Height = 750;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9);

        _tabs = new TabControl { Dock = DockStyle.Fill };

        _btnOk = new Button
        {
            Text = "Generate",
            Width = 120,
            Height = 32,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOk.Click += (_, __) => DialogResult = DialogResult.OK;

        _btnCancel = new Button
        {
            Text = "Cancel",
            Width = 120,
            Height = 32,
            BackColor = Color.FromArgb(70, 70, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

        var panel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        panel.Controls.AddRange([_btnCancel, _btnOk]);

        Controls.Add(_tabs);
        Controls.Add(panel);

        Shown += async (_, __) => await BuildTabsAsync();   // NOT Load
    }

    private async Task BuildTabsAsync()
    {
        foreach (var file in _files)
        {
            var tab = new TabPage(Path.GetFileName(file.Path))
            {
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var editor = CreateEditor();
            editor.Text = file.Content;

            tab.Controls.Add(editor);
            _tabs.TabPages.Add(tab);

            await _workspace.HighlightAsync(editor, file);
        }
    }

    private static FastColoredTextBox CreateEditor() => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10),
        Language = Language.Custom,
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.Gainsboro,
        ShowLineNumbers = true
    };
}