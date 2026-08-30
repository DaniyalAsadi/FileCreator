using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileCreator.Core.Loading;
using FileCreator.Grpc.Coordination;
using FileCreator.Grpc.Discovery;
using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.IO;
using GrpcScaffold.Core.Analysis.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace FileCreator;

public partial class GrpcGenerationForm : Window
{
    private const string EmptyCmbText = "----";
    private readonly GrpcGenerationCoordinator _coordinator;

    private readonly IWorkspaceCache _workspaceCache;

    private readonly IEndpointDiscoveryService _endpointDiscovery;

    private readonly GenerationContext _context;

    private List<EndpointSelectionItem> _allEndpoints = [];

    private List<string> _allGroups = [];
    private ImmutableArray<EndpointModel> _discoveredEndpoints = [];
    private readonly LoadingOperation<ImmutableArray<EndpointModel>> _endpointLoading = new();
    private CancellationTokenSource? _endpointLoadCancellation;

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

        ContentRendered += OnContentRendered;
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;

        if (_context.Paths is null || string.IsNullOrWhiteSpace(_context.ProjectName))
        {
            MessageBox.Show("لطفاً ابتدا تنظیمات پروژه را در فرم اصلی بارگذاری کنید.");
            Close();
            return;
        }
        await LoadEndpointsAsync();
    }

    private void SetEndpoints(IReadOnlyList<EndpointSelectionItem> endpoints)
    {
        _allEndpoints = [.. endpoints];

        ApplyEndpointFilter();
    }

    private async Task LoadEndpointsAsync()
    {
        _endpointLoadCancellation?.Dispose();
        _endpointLoadCancellation = new CancellationTokenSource();
        var cancellationToken = _endpointLoadCancellation.Token;
        var progress = new Progress<LoadingProgress>(UpdateLoadingUi);

        try
        {
            BeginLoading();

            var options = BuildOptionsFromForm();
            var endpoints = await _endpointLoading.RunAsync(
                (reporter, token) => _endpointDiscovery.DiscoverAsync(options, reporter, token),
                progress,
                cancellationToken);

            _discoveredEndpoints = endpoints;

            SetEndpoints([.. endpoints
                .Select(EndpointSelectionItem.Map)]);

            _allGroups = [EmptyCmbText];

            _allGroups.AddRange(endpoints.GroupBy(e => e.EndpointGroupName).Select(e => e.Key));

            cmbGroupName.ItemsSource = _allGroups.ToList();
            cmbGroupName.SelectedIndex = 0;
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(
                "Endpoint discovery was cancelled.",
                "Loading Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            Trace.TraceError("gRPC endpoint discovery failed: {0}", ex);
            MessageBox.Show(
                ex.Message,
                "Endpoint Discovery Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            EndLoading();
            _endpointLoadCancellation?.Dispose();
            _endpointLoadCancellation = null;
        }
    }

    private void BeginLoading()
    {
        grpcContent.IsEnabled = false;
        loadingOverlay.Visibility = Visibility.Visible;
        btnCancelLoading.IsEnabled = true;
        UpdateLoadingUi(new LoadingProgress(0, "Preparing endpoint discovery"));
    }

    private void EndLoading()
    {
        loadingOverlay.Visibility = Visibility.Collapsed;
        grpcContent.IsEnabled = true;
    }

    private void UpdateLoadingUi(LoadingProgress progress)
    {
        loadingProgressBar.Value = progress.Percentage;
        loadingPercentageText.Text = $"{progress.Percentage}%";
        loadingStageText.Text = progress.Stage;
        loadingDetailText.Text = progress.Detail ?? string.Empty;
        loadingDetailText.Visibility = string.IsNullOrWhiteSpace(progress.Detail)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void BtnCancelLoading_Click(object sender, RoutedEventArgs e)
    {
        btnCancelLoading.IsEnabled = false;
        loadingStageText.Text = "Cancelling...";
        _endpointLoadCancellation?.Cancel();
    }

    private void BtnGenerateGrpc_Click(object sender, RoutedEventArgs e)
    {
        dgvEndpoints.CommitEdit(DataGridEditingUnit.Row, true);

        var options = BuildOptionsFromForm();

        if (options.SelectedEndpoints.Count == 0)
        {
            MessageBox.Show("Please select at least one endpoint.");
            return;
        }

        GrpcGenerationResult result;

        try
        {
            btnGenerateGrpc.IsEnabled = false;

            result = _coordinator.Prepare(options, _discoveredEndpoints);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(
                ex.Message,
                "gRPC Scaffold",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }
        finally
        {
            btnGenerateGrpc.IsEnabled = true;
        }

        var workspace = _workspaceCache.Current;

        var preview = new PreviewForm(workspace, result.Files) { Owner = this };

        if (preview.ShowDialog() != true)
            return;

        var writeResults = _coordinator.Commit(result);

        ShowWriteSummary(writeResults);
    }

    private GrpcGenerationOptions BuildOptionsFromForm()
    {
        var endpointFilter = txtEndpointFilter.Text.Trim();

        return new()
        {
            ProjectName = _context.ProjectName,

            SolutionPath = _context.SolutionPath,

            GenerateAll =
                chkGenerateAll.IsChecked == true,

            EndpointFilter = string.IsNullOrWhiteSpace(endpointFilter)
                ? null
                : $"*{endpointFilter}*",

            InternalOnly =
                chkInternalOnly.IsChecked == true,

            DryRun =
                chkDryRun.IsChecked == true,

            Force =
                chkForce.IsChecked == true,

            Strict =
                chkStrict.IsChecked == true,

            SelectedEndpoints =
                [.. _allEndpoints
                    .Where(x => x.Selected)
                    .Select(x => x.Name)]
        };
    }

    private static void ShowWriteSummary(IReadOnlyList<WriteResult> results)
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
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void TxtEndpointFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyEndpointFilter();
    }

    private void CmbGroupName_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyEndpointFilter();
    }

    private void ApplyEndpointFilter()
    {
        if (dgvEndpoints is null)
            return;

        var filter = txtEndpointFilter.Text.Trim();

        var selectedGroup = cmbGroupName.SelectedItem?.ToString() ?? string.Empty;
        var groupSearch = selectedGroup == EmptyCmbText ? string.Empty : selectedGroup.Trim();

        var filtered = _allEndpoints.ToList();
        if (!string.IsNullOrEmpty(groupSearch))
        {
            filtered = [.. filtered.Where(x => x.GroupName == groupSearch)];
        }
        if (!string.IsNullOrEmpty(filter))
            filtered = [.. filtered.Where(x => EndpointFilter.GlobMatch($"*{filter}*", x.Name))];

        dgvEndpoints.ItemsSource = null;
        dgvEndpoints.ItemsSource = filtered;
    }

    private void ChkSelectAll_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
            return;

        SelectAllEndpoints(checkBox.IsChecked == true);
    }

    private void SelectAllEndpoints(bool selected)
    {
        if (dgvEndpoints.ItemsSource is not IEnumerable<EndpointSelectionItem> endpoints)
            return;

        foreach (var endpoint in endpoints)
        {
            endpoint.Selected = selected;
        }

        dgvEndpoints.Items.Refresh();
    }
}
