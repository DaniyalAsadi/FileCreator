using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;

using System.Text.Json;

namespace FileCreator;

public partial class SettingsForm : Window
{
    private readonly GenerationContext _context;

    public SettingsForm(GenerationContext context)
    {
        _context = context;
        InitializeComponent();
        txtSolutionPath.Text = Properties.Settings.Default.SolutionPath;
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Solution Files (*.sln)|*.sln"
        };
        if (ofd.ShowDialog() == true)
        {
            txtSolutionPath.Text = ofd.FileName;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string slnPath = txtSolutionPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(slnPath) || !File.Exists(slnPath))
        {
            MessageBox.Show("Please select a valid Solution (.sln) file.",
                            "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(slnPath);
            string solutionFolder = Path.GetDirectoryName(slnPath)!;
            string solutionName = Path.GetFileNameWithoutExtension(slnPath);
            var projects = ExtractProjects(lines, solutionFolder);
            var projectLines = JsonSerializer.Serialize(projects);
            _context.SolutionPath = solutionFolder;
            _context.SolutionName = solutionName;
            _context.ProjectName = "";
            Properties.Settings.Default.SolutionPath = slnPath;
            Properties.Settings.Default.ProjectPathes = projectLines;
            Properties.Settings.Default.Save();
            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        static Dictionary<string, string> ExtractProjects(string[] lines, string solutionFolder)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            string useCasesPattern = @"Project\(""\{[A-F0-9\-]+\}""\)\s*=\s*""(?<projectName>[^""]+)"",\s*""(?<path>[^""]+\.csproj)""";

            var useCasesMatches = lines.Select(l => Regex.Match(l, useCasesPattern)).Where(e => e.Success).ToList();
            foreach (var line in useCasesMatches)
            {
                string path = line.Groups["path"].Value;
                string projectName = line.Groups["projectName"].Value;
                string useCasesFolder = Path.Combine(solutionFolder, Path.GetDirectoryName(path)!);
                string useCasesCsproj = Path.Combine(useCasesFolder, Path.GetFileName(path));

                if (!File.Exists(useCasesCsproj))
                    throw new FileNotFoundException($"{projectName}.csproj file not found.");

                properties.Add(projectName, path);
            }
            return properties;
        }
    }

    // کمکی برای پاک کردن مقادیر Settings
    private static void ClearSettings()
    {
        Properties.Settings.Default.SolutionPath = string.Empty;
        Properties.Settings.Default.ProjectPathes = string.Empty;
        Properties.Settings.Default.Save();
    }
}
