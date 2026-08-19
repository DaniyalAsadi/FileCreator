using FileCreator.Grpc.Coordination;
using FileCreator.Grpc.Discovery;
using FileCreator.Grpc.ViewModels;
using FileCreator.Services;
using GrpcScaffold.Core.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace FileCreator;

public partial class GrpcGenerationForm : Form
{
    private const string EmptyCmbText = "----";
    private readonly GrpcGenerationCoordinator _coordinator;

    private readonly IWorkspaceCache _workspaceCache;

    private readonly IEndpointDiscoveryService _endpointDiscovery;

    private readonly GenerationContext _context;

    private List<EndpointSelectionItem> _allEndpoints = [];

    private List<string> _allGroups = [];


    public GrpcGenerationForm(
        GrpcGenerationCoordinator coordinator,
        GenerationContext context,
        IWorkspaceCache workspaceCache,
        IEndpointDiscoveryService endpointDiscovery)
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

        if (_context.Paths is null || string.IsNullOrWhiteSpace(_context.ProjectName))
        {
            MessageBox.Show("لطفاً ابتدا تنظیمات پروژه را در فرم اصلی بارگذاری کنید.");
            Close();
            return;
        }
        await LoadEndpointsAsync();
    }
    private void SetEndpoints(
    IReadOnlyList<EndpointSelectionItem> endpoints)
    {
        _allEndpoints = [.. endpoints];

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
       
            _allGroups = [EmptyCmbText];

            _allGroups.AddRange(endpoints.GroupBy(e => e.EndpointGroupName).Select(e => e.Key));

            cmbGroupName.DataSource = _allGroups.ToList();
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

            GenerateAll =
                chkGenerateAll.Checked,


            EndpointFilter =
                $"*{txtEndpointFilter.Text.Trim()}*",


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
            "عملیات با موفقیت انجام شد",
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

    private void cmbGroupName_SelectedIndexChanged(object sender, EventArgs e)
    {
        ApplyEndpointFilter();
    }
    private void ApplyEndpointFilter()
    {
        var filter = txtEndpointFilter.Text.Trim();

        var groupSearch = cmbGroupName.Text.Trim() == EmptyCmbText ? string.Empty : cmbGroupName.Text.Trim();

        var filtered = _allEndpoints.ToList();
        if(!string.IsNullOrEmpty(groupSearch))
        {
            filtered = [.. filtered.Where(x => x.GroupName == groupSearch)];
        }
        if (!string.IsNullOrEmpty(filter))
            filtered = [.. filtered.Where(x => EndpointFilter.GlobMatch($"*{filter}*", x.Name))];
        dgvEndpoints.DataSource = null;
        dgvEndpoints.DataSource = filtered;
    }
}