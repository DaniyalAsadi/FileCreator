using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FileCreator;

public partial class SettingsForm : Form
{
    private readonly GenerationContext _context;
    public SettingsForm(GenerationContext context)
    {
        _context = context;
        InitializeComponent();
        txtSolutionPath.Text = Properties.Settings.Default.SolutionPath;
    }

    private void BtnBrowse_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "Solution Files (*.sln)|*.sln";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtSolutionPath.Text = ofd.FileName;
        }
    }

    private void BtnSave_Click(object sender, EventArgs e)
    {
        string slnPath = txtSolutionPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(slnPath) || !File.Exists(slnPath))
        {
            MessageBox.Show("Please select a valid Solution (.sln) file.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }


        try
        {
            string[] lines = File.ReadAllLines(slnPath);
            string solutionFolder = Path.GetDirectoryName(slnPath)!;
            string solutionName = Path.GetFileNameWithoutExtension(slnPath);
            var projects = ExtractProjects(lines, solutionFolder);
            var projectLines = JsonConvert.SerializeObject(projects);
            _context.SolutionPath = solutionFolder;
            _context.SolutionName = solutionName;
            _context.ProjectName = "";
            Properties.Settings.Default.SolutionPath = slnPath;
            Properties.Settings.Default.ProjectPathes = projectLines;
            Properties.Settings.Default.Save();
            var result = MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

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
