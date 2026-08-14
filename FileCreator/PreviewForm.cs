using FastColoredTextBoxNS;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FileCreator;

public partial class PreviewForm : Form
{
    private readonly IReadOnlyList<GeneratedFile> _files;
    private readonly PreviewWorkspace _workspace;

    public PreviewForm(PreviewWorkspace workspace, IReadOnlyList<GeneratedFile> files)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _files = files ?? throw new ArgumentNullException(nameof(files));

        Shown += async (_, __) => await BuildTabsAsync();   // NOT Load
        InitializeComponent();
    }

    private async Task BuildTabsAsync()
    {
        foreach (var file in _files)
        {
            var tab = new TabPage(Path.GetFileName(file.AbsolutePath))
            {
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var editor = CreateEditor();
            editor.Text = file.Content;

            tab.Controls.Add(editor);
            tabs.TabPages.Add(tab);

            await _workspace.HighlightAsync(editor, file);
        }
    }

    private static FastColoredTextBox CreateEditor() => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Cascadia Mono", 10),
        Language = Language.Custom,
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.Gainsboro,
        ShowLineNumbers = true
    };

    private void panel_Resize(object sender, EventArgs e)
    {
        btnCancel.Location = new Point(panel.Width - btnCancel.Width - 10, 10);
        btnOk.Location = new Point(btnCancel.Left - btnOk.Width - 10, 10);
    }
}
