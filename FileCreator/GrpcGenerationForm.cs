using FileCreator.Core;
using FileCreator.Grpc.Coordination;
using FileCreator.Grpc.Discovery;
using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FileCreator;

public partial class GrpcGenerationForm : Form
{
    private readonly GrpcGenerationCoordinator _coordinator;

    private readonly IWorkspaceCache _workspaceCache;

    private readonly IEndpointDiscoveryService _endpointDiscovery;

    private readonly GenerationContext _context;

    private List<EndpointSelectionItem> _allEndpoints = [];


    public GrpcGenerationForm(
        GrpcGenerationCoordinator coordinator,
        IWorkspaceCache workspaceCache,
        IEndpointDiscoveryService endpointDiscovery,
        GenerationContext context)
    {
        _coordinator = coordinator;
        _workspaceCache = workspaceCache;
        _endpointDiscovery = endpointDiscovery;
        _context = context;
        InitializeComponent();

        ConfigureEndpointGrid();
    }


    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        await LoadEndpointsAsync();
    }
    private void SetEndpoints(
    IReadOnlyList<EndpointSelectionItem> endpoints)
    {
        _allEndpoints = endpoints.ToList();

        ApplyEndpointFilter();
    }


    private async Task LoadEndpointsAsync()
    {
        try
        {
            UseWaitCursor = true;

            var options = BuildOptionsFromForm();
            var workspace = _workspaceCache.GetWorkspace();


            var endpoints =
                await _endpointDiscovery.DiscoverAsync(options);



            SetEndpoints([.. endpoints
                .Select(EndpointSelectionItem.Map)]);

        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Endpoint Discovery Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }



    private async void BtnGenerateGrpc_Click(
        object sender,
        EventArgs e)
    {

        dgvEndpoints.EndEdit();


        var options = BuildOptionsFromForm();


        if (options.SelectedEndpoints.Count == 0)
        {
            MessageBox.Show(
                "Please select at least one endpoint.");

            return;
        }


        GrpcGenerationResult result;


        try
        {
            btnGenerateGrpc.Enabled = false;


            result = await _coordinator.PrepareAsync(options);

        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(
                ex.Message,
                "gRPC Scaffold",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }
        finally
        {
            btnGenerateGrpc.Enabled = true;
        }



        var workspace =
            _workspaceCache.GetWorkspace();



        using var preview =
            new PreviewForm(
                workspace,
                result.Files);



        if (preview.ShowDialog(this)
            != DialogResult.OK)
            return;



        var writeResults =
            _coordinator.Commit(result);



        ShowWriteSummary(writeResults);
    }



    private GrpcGenerationOptions BuildOptionsFromForm()
    {
        return new()
        {
            ProjectName = _context.ProjectName,

            SolutionPath = _context.SolutionPath,

            Namespace =
                string.IsNullOrWhiteSpace(txtNamespace.Text)
                    ? null
                    : txtNamespace.Text,


            OutputFolder =
                txtOutputFolder.Text,


            GenerateAll =
                chkGenerateAll.Checked,


            EndpointFilter =
                txtEndpointFilter.Text,


            InternalOnly =
                chkInternalOnly.Checked,


            DryRun =
                chkDryRun.Checked,


            Force =
                chkForce.Checked,


            Strict =
                chkStrict.Checked,


            SelectedEndpoints =
                [.. _allEndpoints
                    .Where(x => x.Selected)
                    .Select(x => x.Name)]
        };
    }



    private static void ShowWriteSummary(
        IReadOnlyList<WriteResult> results)
    {
        var summary =
            string.Join(
                Environment.NewLine,
                results.Select(r =>
                    $"[{(r.WasUnchanged
                        ? "unchanged"
                        : r.ManualEditDetected
                            ? "skipped"
                            : "written")}] {r.RelativePath}"));


        MessageBox.Show(
            summary,
            "نتیجه تولید فایل‌های gRPC",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
    private void TxtEndpointFilter_TextChanged(
    object? sender,
    EventArgs e)
    {
        ApplyEndpointFilter();
    }
    private void ApplyEndpointFilter()
    {
        var filter = txtEndpointFilter.Text.Trim();

        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allEndpoints
            : _allEndpoints
                .Where(x =>
                    EndpointFilter.GlobMatch(filter,x.Name))
                .ToList();

        dgvEndpoints.DataSource = null;
        dgvEndpoints.DataSource = filtered;
    }
}