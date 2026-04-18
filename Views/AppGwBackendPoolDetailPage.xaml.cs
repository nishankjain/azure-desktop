using Azure.ResourceManager.Network.Models;
using AzureDesktop.Services;
using AzureDesktop.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

namespace AzureDesktop.Views;

public partial class BackendTargetRow : ObservableObject
{
    private readonly ApplicationGatewayBackendAddress _address;
    private bool _initializing = true;
    public static readonly string[] TargetTypes = ["IP", "FQDN", "VM", "VMSS", "App Service"];
    private static readonly HashSet<string> ResourceTypes = ["VM", "VMSS", "App Service"];

    public BackendTargetRow(ApplicationGatewayBackendAddress addr)
    {
        _address = addr;
        Type = string.IsNullOrEmpty(addr.IPAddress) ? "FQDN" : "IP";
        Value = addr.IPAddress ?? addr.Fqdn ?? "";
        AvailableResources = [];
        _initializing = false;
    }

    [ObservableProperty]
    public partial string Type { get; set; }

    [ObservableProperty]
    public partial string Value { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BackendTargetResource> AvailableResources { get; set; }

    [ObservableProperty]
    public partial BackendTargetResource? SelectedResource { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingResources { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    partial void OnValueChanged(string value)
    {
        if (_initializing) return;
        if (Type == "IP") { _address.IPAddress = value; _address.Fqdn = null; }
        else { _address.Fqdn = value; _address.IPAddress = null; }
    }

    partial void OnSelectedResourceChanged(BackendTargetResource? value)
    {
        if (value is null) return;
        Value = value.ResolvedValue;
    }

    public void SwitchType(string newType)
    {
        if (newType == Type) return;
        Type = newType;

        if (newType == "IP")
        {
            _address.IPAddress = Value;
            _address.Fqdn = null;
        }
        else
        {
            _address.Fqdn = Value;
            _address.IPAddress = null;
        }
    }

    public static bool IsResourceTargetType(string type) => ResourceTypes.Contains(type);
    public ApplicationGatewayBackendAddress Address => _address;
}

public sealed partial class AppGwBackendPoolDetailPage : Page
{
    private const int MaxTargets = 1200;
    private CancellationTokenSource? _cts;
    public AppGwViewModel ViewModel { get; }
    private readonly BackendTargetResourceService _resourceService;
    private NavigationContext? _navCtx;
    private string _poolName = "";
    private List<BackendTargetRow> _allTargets = [];
    public ObservableCollection<BackendTargetRow> FilteredTargets { get; } = [];
    private DispatcherTimer? _searchDebounceTimer;
    private string _searchText = "";

    public AppGwBackendPoolDetailPage()
    {
        ViewModel = App.GetService<AppGwViewModel>();
        _resourceService = App.GetService<BackendTargetResourceService>();
        InitializeComponent();
        TargetsList.ItemsSource = FilteredTargets;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx && ctx.DetailItemName is not null)
        {
            _navCtx = ctx;
            _poolName = ctx.DetailItemName;
            TitleText.Text = ctx.DetailItemName;

            if (ctx.Resource is not null)
            {
                await ViewModel.LoadAsync(ctx.Resource.ResourceId, _cts.Token);
            }

            LoadTargets();
            RenderRules();
        }
    }

    private void LoadTargets()
    {
        var pool = ViewModel.Data?.BackendAddressPools.FirstOrDefault(p => p.Name == _poolName);
        _allTargets = pool?.BackendAddresses?.Select(a => new BackendTargetRow(a)).ToList() ?? [];
        TargetCountText.Text = $"({_allTargets.Count})";
        UpdateAddButtonState();
        RefreshTargetsList();
    }

    private static string? SubResourceName(Azure.Core.ResourceIdentifier? id)
    {
        if (id is null) return null;
        var name = id.Name;
        if (name is null) return null;
        var idx = name.LastIndexOf('/');
        return idx >= 0 ? name[(idx + 1)..] : name;
    }

    private void UpdateAddButtonState()
    {
        AddTargetButton.IsEnabled = _allTargets.Count < MaxTargets;
    }

    private void RefreshTargetsList()
    {
        FilteredTargets.Clear();
        foreach (var t in _allTargets)
        {
            if (string.IsNullOrWhiteSpace(_searchText) || t.Value.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                FilteredTargets.Add(t);
        }
    }

    private void TargetSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer!.Stop();
            _searchText = TargetSearchBox.Text;
            RefreshTargetsList();
        };
        _searchDebounceTimer.Start();
    }

    private async void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.Tag is not BackendTargetRow row || combo.SelectedItem is not string newType)
            return;

        row.SwitchType(newType);

        // Toggle TextBox/ComboBox visibility in the same row
        if (combo.Parent is Grid grid)
        {
            var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
            var resourceCombo = grid.Children.OfType<ComboBox>().FirstOrDefault(c => c != combo);
            if (textBox is not null && resourceCombo is not null)
            {
                var isResource = BackendTargetRow.IsResourceTargetType(newType);
                textBox.Visibility = isResource ? Visibility.Collapsed : Visibility.Visible;
                resourceCombo.Visibility = isResource ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        if (BackendTargetRow.IsResourceTargetType(newType) && ViewModel.ResourceId is not null)
        {
            row.IsLoadingResources = true;
            try
            {
                var resources = await _resourceService.GetResourcesAsync(
                    newType, ViewModel.ResourceId, _cts?.Token ?? default);
                row.AvailableResources = new ObservableCollection<BackendTargetResource>(resources);
            }
            finally
            {
                row.IsLoadingResources = false;
            }
        }
    }

    private bool _suppressSelectAll;

    private void TargetCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // x:Bind hasn't updated yet, so count from UI state
        if (sender is CheckBox cb && cb.DataContext is BackendTargetRow row)
            row.IsSelected = cb.IsChecked == true;
        UpdateDeleteTargetsState();
    }

    private void UpdateDeleteTargetsState()
    {
        var selectedCount = _allTargets.Count(t => t.IsSelected);
        DeleteTargetsButton.IsEnabled = selectedCount > 0;

        if (!_suppressSelectAll)
        {
            _suppressSelectAll = true;
            SelectAllTargets.IsChecked = selectedCount == _allTargets.Count && selectedCount > 0 ? true
                : selectedCount == 0 ? false : null;
            _suppressSelectAll = false;
        }
    }

    private void SelectAllTargets_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSelectAll) return;
        _suppressSelectAll = true;
        var selectAll = SelectAllTargets.IsChecked == true;
        foreach (var t in FilteredTargets) t.IsSelected = selectAll;
        DeleteTargetsButton.IsEnabled = selectAll && FilteredTargets.Count > 0;
        _suppressSelectAll = false;
    }

    private void DeleteTargets_Click(object sender, RoutedEventArgs e)
    {
        var pool = ViewModel.Data?.BackendAddressPools.FirstOrDefault(p => p.Name == _poolName);
        if (pool is null) return;

        var toRemove = _allTargets.Where(t => t.IsSelected).ToList();
        foreach (var t in toRemove)
        {
            pool.BackendAddresses?.Remove(t.Address);
            _allTargets.Remove(t);
        }

        TargetCountText.Text = $"({_allTargets.Count})";
        UpdateAddButtonState();
        RefreshTargetsList();
        DeleteTargetsButton.IsEnabled = false;
        _suppressSelectAll = true;
        SelectAllTargets.IsChecked = false;
        _suppressSelectAll = false;
    }

    private List<string> _ruleNames = [];

    private void RenderRules()
    {
        if (ViewModel.Data is null)
        {
            RulesList.ItemsSource = null;
            return;
        }

        // Collect pool names referenced by each URL path map
        var pathMapPoolNames = new Dictionary<string, HashSet<string>>();
        foreach (var map in ViewModel.Data.UrlPathMaps)
        {
            var pools = new HashSet<string>();
            var defaultPool = SubResourceName(map.DefaultBackendAddressPoolId);
            if (defaultPool is not null) pools.Add(defaultPool);
            if (map.PathRules is not null)
            {
                foreach (var pr in map.PathRules)
                {
                    var prPool = SubResourceName(pr.BackendAddressPoolId);
                    if (prPool is not null) pools.Add(prPool);
                }
            }
            if (map.Name is not null) pathMapPoolNames[map.Name] = pools;
        }

        // Find rules that reference this pool directly or via URL path maps
        _ruleNames = ViewModel.Data.RequestRoutingRules
            .Where(r =>
            {
                if (SubResourceName(r.BackendAddressPoolId) == _poolName) return true;
                var mapName = SubResourceName(r.UrlPathMapId);
                return mapName is not null
                    && pathMapPoolNames.TryGetValue(mapName, out var pools)
                    && pools.Contains(_poolName);
            })
            .Select(r => r.Name ?? "")
            .ToList();

        RulesList.ItemsSource = _ruleNames;
    }

    private void RuleItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string ruleName && _navCtx is not null)
        {
            Frame.Navigate(typeof(AppGwRoutingRuleDetailPage),
                _navCtx with { Section = AppGwSection.RoutingRules, DetailItemName = ruleName });
        }
    }

    private void AddTarget_Click(object sender, RoutedEventArgs e)
    {
        if (_allTargets.Count >= MaxTargets) return;
        var pool = ViewModel.Data?.BackendAddressPools.FirstOrDefault(p => p.Name == _poolName);
        if (pool is null) return;
        var addr = new ApplicationGatewayBackendAddress { Fqdn = "" };
        pool.BackendAddresses.Insert(0, addr);
        var target = new BackendTargetRow(addr);
        _allTargets.Insert(0, target);
        FilteredTargets.Insert(0, target);
        TargetCountText.Text = $"({_allTargets.Count})";
        UpdateAddButtonState();

        // Scroll to the new item at the top
        TargetsList.ScrollIntoView(target, ScrollIntoViewAlignment.Leading);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveChangesAsync($"Updated pool '{_poolName}'.");
        LoadTargets();
        RenderRules();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
