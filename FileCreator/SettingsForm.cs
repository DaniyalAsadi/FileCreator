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
    public SettingsForm()
    {
        InitializeComponent();
        txtSolutionPath.Text = Properties.Settings.Default.SolutionPath;
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "Solution Files (*.sln)|*.sln";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtSolutionPath.Text = ofd.FileName;
        }
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        string slnPath = txtSolutionPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(slnPath) || !File.Exists(slnPath))
        {
            MessageBox.Show("Please select a valid Solution (.sln) file.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string[] lines = File.ReadAllLines(slnPath);
        string solutionFolder = Path.GetDirectoryName(slnPath)!;
        string solutionName = Path.GetFileNameWithoutExtension(slnPath);

        try
        {
            Properties.Settings.Default.SolutionPath = slnPath;
            // --- UseCases Project ---
            string useCasesFolder = ExtractFolder("UseCases",lines, solutionFolder, solutionName);

            Properties.Settings.Default.UseCasesPath = useCasesFolder;

            string webFolder = ExtractFolder("Web", lines, solutionFolder, solutionName);
            Properties.Settings.Default.WebPath = webFolder;

            string endpointFolder = Path.Combine(webFolder, "EndPoints");
            Directory.CreateDirectory(endpointFolder);
            string unitTestsFolder = ExtractFolder("UnitTests", lines, solutionFolder, solutionName);
            Properties.Settings.Default.UnitTestPath = unitTestsFolder;
            string functionalTestFolder = ExtractFolder("FunctionalTests", lines, solutionFolder, solutionName);
            Properties.Settings.Default.FunctionalTestPath = functionalTestFolder;
            string sharedKernelFolder = ExtractFolder("SharedKernel", lines, solutionFolder, solutionName);
            Properties.Settings.Default.SharedKernelPath = sharedKernelFolder;
            Properties.Settings.Default.Save();

            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ClearSettings();
        }

        static string ExtractFolder(string projectName,string[] lines, string solutionFolder, string soloutionName)
        {
            string useCasesPattern = $@"Project\(""\{{[A-F0-9\-]+\}}""\)\s*=\s*""{Regex.Escape(soloutionName)}\.{Regex.Escape(projectName)}"",\s*""(?<path>[^""]+\.csproj)""";
            var useCasesMatch = lines.Select(l => Regex.Match(l, useCasesPattern))
                                     .FirstOrDefault(m => m.Success);

            if (useCasesMatch == null)
                throw new FileNotFoundException($"{soloutionName}.{projectName} project not found in Solution.");

            string useCasesRelative = useCasesMatch.Groups["path"].Value;
            string useCasesFolder = Path.Combine(solutionFolder, Path.GetDirectoryName(useCasesRelative)!);
            string useCasesCsproj = Path.Combine(useCasesFolder, Path.GetFileName(useCasesRelative));

            if (!File.Exists(useCasesCsproj))
                throw new FileNotFoundException($"{soloutionName}.{projectName}.csproj file not found.");
            return useCasesFolder;
        }
    }

    // کمکی برای پاک کردن مقادیر Settings
    private void ClearSettings()
    {
        Properties.Settings.Default.SolutionPath = string.Empty;
        Properties.Settings.Default.UseCasesPath = string.Empty;
        Properties.Settings.Default.FunctionalTestPath = string.Empty;
        Properties.Settings.Default.UnitTestPath = string.Empty;
        Properties.Settings.Default.WebPath = string.Empty;
        Properties.Settings.Default.Save();
    }


}
