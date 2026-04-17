using Azure.ResourceManager.Network.Models;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwBackendPoolDetailPage : Page
{
    public AppGwViewModel ViewModel { get; }
    private NavigationContext? _navCtx;
    private string _poolName = "";
    private List<TargetRow> _allTargets = [];
    private List<(TargetRow Row, ListViewItem Item)> _cachedItems = [];
    private DispatcherTimer? _searchDebounceTimer;
    private string _searchText = "";

    public AppGwBackendPoolDetailPage()
    {
        ViewModel = App.GetService<AppGwViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx && ctx.DetailItemName is not null)
        {
            _navCtx = ctx;
            _poolName = ctx.DetailItemName;
            TitleText.Text = ctx.DetailItemName;

            if (ctx.Resource is not null)
            {
                await ViewModel.LoadAsync(ctx.Resource.ResourceId);
            }

            LoadTargets();
            RenderRules();
        }
    }

    private sealed class TargetRow
    {
        public ApplicationGatewayBackendAddress Address { get; }
        public string Type => string.IsNullOrEmpty(Address.IPAddress) ? "FQDN" : "IP";
        public string Value => Address.IPAddress ?? Address.Fqdn ?? "";
        public TargetRow(ApplicationGatewayBackendAddress addr) => Address = addr;
    }

    private static string? SubResourceName(Azure.Core.ResourceIdentifier? id)
    {
        if (id is null) return null;
        var name = id.Name;
        if (name is null) return null;
        var idx = name.LastIndexOf('/');
        return idx >= 0 ? name[(idx + 1)..] : name;
    }

    private void LoadTargets()
    {
        var pool = ViewModel.Data?.BackendAddressPools.FirstOrDefault(p => p.Name == _poolName);
        _allTargets = pool?.BackendAddresses?.Select(a => new TargetRow(a)).ToList() ?? [];
        _cachedItems = _allTargets.Select(t => (t, BuildTargetItem(t))).ToList();
        TargetCountText.Text = $"({_allTargets.Count})";
        RefreshTargetsList();
    }

    private ListViewItem BuildTargetItem(TargetRow target)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var typeText = new TextBlock { Text = target.Type, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };

        var valueBox = new TextBox { Text = target.Value, MinWidth = 200, Margin = new Thickness(8, 0, 8, 0) };
        var addr = target.Address;
        valueBox.TextChanged += (s, _) =>
        {
            if (s is TextBox tb)
            {
                if (!string.IsNullOrEmpty(addr.IPAddress)) addr.IPAddress = tb.Text;
                else addr.Fqdn = tb.Text;
            }
        };
        Grid.SetColumn(valueBox, 1);

        var removeBtn = new Button
        {
            Width = 32, Height = 32, Padding = new Thickness(0),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
        };
        removeBtn.Content = new FontIcon { Glyph = "\uE711", FontSize = 14 };
        removeBtn.Click += (s, _) =>
        {
            var pool = ViewModel.Data?.BackendAddressPools.FirstOrDefault(p => p.Name == _poolName);
            pool?.BackendAddresses?.Remove(addr);
            _allTargets.RemoveAll(t => t.Address == addr);
            _cachedItems.RemoveAll(c => c.Row.Address == addr);
            TargetCountText.Text = $"({_allTargets.Count})";
            RefreshTargetsList();
        };
        Grid.SetColumn(removeBtn, 2);

        grid.Children.Add(typeText);
        grid.Children.Add(valueBox);
        grid.Children.Add(removeBtn);

        return new ListViewItem
        {
            Content = grid,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0, 2, 0, 2),
        };
    }

    private void RefreshTargetsList()
    {
        IEnumerable<(TargetRow Row, ListViewItem Item)> filtered = _cachedItems;
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(c => c.Row.Value.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        TargetsList.ItemsSource = filtered.Select(c => c.Item).ToList();
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

    private void RenderRules()
    {
        RulesArea.Children.Clear();

        var rules = ViewModel.Data?.RequestRoutingRules
            .Where(r => SubResourceName(r.BackendAddressPoolId) == _poolName)
            .ToList() ?? [];

        if (rules.Count == 0)
        {
            RulesArea.Children.Add(new TextBlock
            {
                Text = "No routing rules associated with this pool.",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontStyle = Windows.UI.Text.FontStyle.Italic,
            });
        }
        else
        {
            foreach (var rule in rules)
            {
                var ruleName = rule.Name ?? "";
                var border = new Border
                {
                    Padding = new Thickness(12, 8, 12, 8),
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 2, 0, 2),
                };
                border.PointerPressed += (s, _) =>
                {
                    if (_navCtx is not null) Frame.Navigate(typeof(AppGwRoutingRuleDetailPage), _navCtx with { Section = AppGwSection.RoutingRules, DetailItemName = ruleName });
                };
                border.PointerEntered += (s, _) => ((Border)s).Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
                border.PointerExited += (s, _) => ((Border)s).Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.Children.Add(new TextBlock { Text = ruleName, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
                var chevron = new FontIcon { Glyph = "\uE76C", FontSize = 12, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
                Grid.SetColumn(chevron, 1);
                grid.Children.Add(chevron);

                border.Child = grid;
                RulesArea.Children.Add(border);
            }
        }
    }

    private void AddTarget_Click(object sender, RoutedEventArgs e)
    {
        var pool = ViewModel.Data?.BackendAddressPools.FirstOrDefault(p => p.Name == _poolName);
        if (pool is null) return;
        var addr = new ApplicationGatewayBackendAddress { Fqdn = "" };
        pool.BackendAddresses.Add(addr);
        var target = new TargetRow(addr);
        _allTargets.Add(target);
        _cachedItems.Add((target, BuildTargetItem(target)));
        TargetCountText.Text = $"({_allTargets.Count})";
        RefreshTargetsList();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveChangesAsync($"Updated pool '{_poolName}'.");
        LoadTargets();
        RenderRules();
    }

}
