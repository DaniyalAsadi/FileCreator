using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FileCreator;

public partial class PreviewForm : Window
{
    private readonly IReadOnlyList<GeneratedFile> _files;
    private readonly PreviewWorkspace _workspace;

    private static readonly FontFamily EditorFont = new("Cascadia Mono, Consolas, Courier New");

    public PreviewForm(PreviewWorkspace workspace, IReadOnlyList<GeneratedFile> files)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _files = files ?? throw new ArgumentNullException(nameof(files));

        InitializeComponent();

        Loaded += async (_, _) => await BuildTabsAsync();
    }

    private async Task BuildTabsAsync()
    {
        foreach (var file in _files)
        {
            var editor = CreateEditor();

            var tab = new TabItem
            {
                Header = Path.GetFileName(file.AbsolutePath),
                Content = editor
            };

            tabs.Items.Add(tab);

            var spans = await _workspace.GetClassifiedSpansAsync(file);
            RenderHighlighted(editor, file.Content, spans);
        }
    }

    private static RichTextBox CreateEditor() => new()
    {
        FontFamily = EditorFont,
        FontSize = 13,
        IsReadOnly = true,
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
        Foreground = new SolidColorBrush(PreviewWorkspace.DefaultColor),
        BorderThickness = new Thickness(0),
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Document = new FlowDocument { PageWidth = 4000 }
    };

    private static void RenderHighlighted(
        RichTextBox editor,
        string content,
        IReadOnlyList<Microsoft.CodeAnalysis.Classification.ClassifiedSpan> spans)
    {
        var doc = new FlowDocument
        {
            PageWidth = 4000,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
        };

        var paragraph = new Paragraph
        {
            Margin = new Thickness(0),
            LineHeight = 16,
            FontFamily = EditorFont
        };

        int position = 0;

        foreach (var span in spans.OrderBy(s => s.TextSpan.Start))
        {
            // Emit any un-classified text (whitespace, punctuation) before this span
            // using the default color so nothing is dropped.
            if (span.TextSpan.Start > position)
            {
                var gapText = content.Substring(position, span.TextSpan.Start - position);
                paragraph.Inlines.Add(MakeRun(gapText, PreviewWorkspace.DefaultColor));
            }

            var text = content.Substring(span.TextSpan.Start, span.TextSpan.Length);
            var color = PreviewWorkspace.GetColor(span.ClassificationType);
            paragraph.Inlines.Add(MakeRun(text, color));

            position = span.TextSpan.End;
        }

        // Trailing text after the last classified span.
        if (position < content.Length)
        {
            paragraph.Inlines.Add(MakeRun(content.Substring(position), PreviewWorkspace.DefaultColor));
        }

        doc.Blocks.Add(paragraph);
        editor.Document = doc;
    }

    private static Run MakeRun(string text, Color color) =>
        new(text) { Foreground = new SolidColorBrush(color) };

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
