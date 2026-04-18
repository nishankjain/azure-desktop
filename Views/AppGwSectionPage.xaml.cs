using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwSectionPage : Page
{
    private CancellationTokenSource? _cts;
    public AppGwViewModel ViewModel { get; }
    public string SectionTitle { get; private set; } = "";
    private NavigationContext? _navCtx;
    private AppGwSection _section;
    private ListView? _dataListView;
    private CheckBox? _selectAllCheckBox;
    private bool _suppressSelectAllChanged;
    private readonly HashSet<string> _selectedForDelete = [];

    public AppGwSectionPage()
    {
        ViewModel = App.GetService<AppGwViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx && ctx.Section is not null)
        {
            _navCtx = ctx;
            _section = ctx.Section.Value;
            SectionTitle = SectionToTitle(_section);

            if (ctx.Resource is not null)
            {
                await ViewModel.LoadAsync(ctx.Resource.ResourceId, _cts.Token);
            }

            RenderSection();
        }
    }

    private ObservableCollection<Dictionary<string, string>>? _currentCollection;
    private List<string>? _currentColumns;
    private string _sortColumn = "Name";
    private bool _sortAscending = true;
    private string _searchText = "";
    private string? _confirmDeleteName;
    private bool _isCollectionSection;

    private void RenderSection()
    {
        ContentArea.Children.Clear();
        ContentArea.RowDefinitions.Clear();
        _currentCollection = null;
        _currentColumns = null;
        _selectedForDelete.Clear();
        _confirmDeleteName = null;
        _isCollectionSection = false;
        SearchAddPanel.Visibility = Visibility.Collapsed;

        switch (_section)
        {
            case AppGwSection.Overview:
                AddPropertyCard([
                    ("Name", ViewModel.Name),
                    ("Location", ViewModel.Location),
                    ("SKU Name", ViewModel.SkuName),
                    ("SKU Tier", ViewModel.SkuTier),
                    ("Capacity", ViewModel.SkuCapacity.ToString()),
                    ("Operational State", ViewModel.OperationalState),
                    ("Provisioning State", ViewModel.ProvisioningState),
                    ("Resource ID", ViewModel.ResourceId),
                ]);
                break;

            case AppGwSection.Configuration:
                AddPropertyCard([
                    ("SKU Tier", ViewModel.SkuTier),
                    ("SKU Name", ViewModel.SkuName),
                    ("Capacity", ViewModel.SkuCapacity.ToString()),
                    ("Autoscale Min", ViewModel.AutoscaleMinCapacity),
                    ("Autoscale Max", ViewModel.AutoscaleMaxCapacity),
                    ("HTTP/2", ViewModel.EnableHttp2.ToString()),
                    ("FIPS", ViewModel.EnableFips.ToString()),
                ]);
                break;

            case AppGwSection.Waf:
                AddPropertyCard([
                    ("WAF Enabled", ViewModel.WafEnabled.ToString()),
                    ("Mode", ViewModel.WafMode),
                    ("Rule Set Type", ViewModel.WafRuleSetType),
                    ("Rule Set Version", ViewModel.WafRuleSetVersion),
                    ("Firewall Policy", ViewModel.WafPolicyId),
                ]);
                break;

            case AppGwSection.BackendPools:
                RenderTable(ViewModel.BackendPools, ["Name", "Targets", "Associated Rules"], "No backend pools configured.");
                break;
            case AppGwSection.BackendSettings:
                RenderTable(ViewModel.BackendSettings, ["Name", "Protocol", "Port", "Cookie Affinity", "Request Timeout", "Host Name"], "No backend settings configured.");
                break;
            case AppGwSection.FrontendIP:
                AddSectionHeader("Frontend IP Configurations");
                RenderTable(ViewModel.FrontendIPs, ["Name", "Private IP", "Allocation Method", "Public IP"], "No frontend IPs configured.");
                AddSectionHeader("Frontend Ports");
                RenderTable(ViewModel.FrontendPorts, ["Name", "Port"], "No frontend ports configured.");
                break;
            case AppGwSection.PrivateLink:
                RenderTable(ViewModel.PrivateLinks, ["Name", "IP Configurations"], "No private link configurations.");
                break;
            case AppGwSection.SslSettings:
                RenderTable(ViewModel.SslCertificates, ["Name"], "No SSL certificates or policies.");
                if (ViewModel.SslProfiles.Count > 0)
                {
                    AddSectionHeader("SSL Profiles");
                    RenderTable(ViewModel.SslProfiles, ["Name", "Client Auth"], "");
                }
                break;
            case AppGwSection.Listeners:
                RenderTable(ViewModel.HttpListeners, ["Name", "Protocol", "Host Name", "Frontend Port", "Require SNI"], "No listeners configured.");
                break;
            case AppGwSection.RoutingRules:
                RenderTable(ViewModel.RoutingRules, ["Name", "Rule Type", "Priority", "Listener", "Backend Pool", "Backend Settings"], "No routing rules configured.");
                break;
            case AppGwSection.RewriteSets:
                RenderTable(ViewModel.RewriteSets, ["Name", "Rule Count"], "No rewrite sets configured.");
                break;
            case AppGwSection.HealthProbes:
                RenderTable(ViewModel.HealthProbes, ["Name", "Protocol", "Host", "Path", "Interval", "Timeout", "Unhealthy Threshold"], "No health probes configured.");
                break;
            case AppGwSection.JwtValidation:
                RenderTable(ViewModel.JwtConfigs, ["Name"], "No JWT validation configs.");
                break;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        if (_currentCollection is not null && _currentColumns is not null)
        {
            RefreshTable();
        }
    }

    private void RenderTable(ObservableCollection<Dictionary<string, string>> items, List<string> columns, string emptyMessage)
    {
        _currentCollection = items;
        _currentColumns = columns;
        _sortColumn = "Name";
        _sortAscending = true;
        _searchText = "";
        _confirmDeleteName = null;
        _isCollectionSection = true;
        SearchBox.Text = "";
        SearchAddPanel.Visibility = Visibility.Visible;
        AddNewButton.Visibility = AppGwViewModel.GetEditableFields(_section).Count > 0
            ? Visibility.Visible : Visibility.Collapsed;

        if (items.Count == 0 && !string.IsNullOrEmpty(emptyMessage))
        {
            ContentArea.Children.Add(new TextBlock
            {
                Text = emptyMessage,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontStyle = Windows.UI.Text.FontStyle.Italic,
            });
            return;
        }

        RefreshTable();
    }

    private void RefreshTable()
    {
        if (_currentCollection is null || _currentColumns is null) return;

        for (int i = ContentArea.Children.Count - 1; i >= 0; i--)
        {
            ContentArea.Children.RemoveAt(i);
        }

        ContentArea.RowDefinitions.Clear();

        var filtered = _currentCollection.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(row =>
                row.TryGetValue("Name", out var name) &&
                name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(_sortColumn))
        {
            filtered = _sortAscending
                ? filtered.OrderBy(r => r.TryGetValue(_sortColumn, out var v) ? v : "", NaturalStringComparer.Instance)
                : filtered.OrderByDescending(r => r.TryGetValue(_sortColumn, out var v) ? v : "", NaturalStringComparer.Instance);
        }

        var rows = filtered.ToList();
        var columns = _currentColumns;

        // Build table as a Grid with frozen header
        var tableGrid = new Grid();
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // divider
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // data

        // Header row
        var headerGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };

        if (_isCollectionSection)
        {
            // Select all checkbox
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var selectAllCb = new CheckBox
            {
                MinWidth = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = "_selectAll",
            };
            selectAllCb.Checked += SelectAll_Changed;
            selectAllCb.Unchecked += SelectAll_Changed;
            _selectAllCheckBox = selectAllCb;
            Grid.SetColumn(selectAllCb, 0);
            headerGrid.Children.Add(selectAllCb);
        }
        var headerColOffset = _isCollectionSection ? 1 : 0;

        for (int c = 0; c < columns.Count; c++)
        {
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var col = columns[c];
            var arrow = _sortColumn == col ? (_sortAscending ? " \u2191" : " \u2193") : "";
            var headerBtn = new Button
            {
                Content = col + arrow,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Tag = col,
            };
            headerBtn.Click += HeaderButton_Click;
            Grid.SetColumn(headerBtn, c + headerColOffset);
            headerGrid.Children.Add(headerBtn);
        }

        if (_isCollectionSection)
        {
            // Chevron spacer for navigable rows
            if (_section == AppGwSection.BackendPools || _section == AppGwSection.RoutingRules)
            {
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var chevronSpacer = new Border { Width = 20 };
                Grid.SetColumn(chevronSpacer, columns.Count + headerColOffset);
                headerGrid.Children.Add(chevronSpacer);
            }
        }

        Grid.SetRow(headerGrid, 0);
        tableGrid.Children.Add(headerGrid);

        var divider = new Border
        {
            Height = 1,
            Margin = new Thickness(0, 4, 0, 0),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
        };
        Grid.SetRow(divider, 1);
        tableGrid.Children.Add(divider);

        // Data rows
        var isNavigable = (_section == AppGwSection.BackendPools || _section == AppGwSection.RoutingRules);
        var plainItemStyle = new Style(typeof(ListViewItem));
        plainItemStyle.Setters.Add(new Setter(ListViewItem.PaddingProperty, new Thickness(0)));
        plainItemStyle.Setters.Add(new Setter(ListViewItem.MarginProperty, new Thickness(0, 2, 0, 2)));
        plainItemStyle.Setters.Add(new Setter(ListViewItem.MinHeightProperty, 0.0));
        plainItemStyle.Setters.Add(new Setter(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));

        var dataListView = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = false,
            Padding = new Thickness(0, 4, 0, 12),
            ItemContainerStyle = plainItemStyle,
        };
        _dataListView = dataListView;

        foreach (var row in rows)
        {
            var rowName = row.TryGetValue("Name", out var n) ? n : "";
            var isConfirming = _confirmDeleteName == rowName;

            if (isConfirming)
            {
                // Delete confirmation row
                var confirmGrid = new Grid { Padding = new Thickness(8, 8, 8, 8) };
                confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var confirmText = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                };
                confirmText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = "Delete \"" });
                confirmText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = rowName, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                confirmText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = "\"? This will update the gateway." });

                var confirmBtn = new Button
                {
                    Content = "Delete",
                    Style = (Style)Application.Current.Resources["AccentButtonStyle"],
                    Margin = new Thickness(8, 0, 4, 0),
                    Tag = rowName,
                };
                confirmBtn.Click += ConfirmDelete_Click;
                Grid.SetColumn(confirmBtn, 1);

                var cancelBtn = new Button { Content = "Cancel", Tag = rowName };
                cancelBtn.Click += CancelDelete_Click;
                Grid.SetColumn(cancelBtn, 2);

                confirmGrid.Children.Add(confirmText);
                confirmGrid.Children.Add(confirmBtn);
                confirmGrid.Children.Add(cancelBtn);
                dataListView.Items.Add(confirmGrid);
            }
            else
            {
                // Row: outer grid with checkbox outside the card
                var rowGrid = new Grid { Tag = rowName };

                var colOffset = 0;
                if (_isCollectionSection)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var cb = new CheckBox
                    {
                        Tag = rowName,
                        IsChecked = _selectedForDelete.Contains(rowName),
                        MinWidth = 0,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    cb.Checked += RowCheckBox_Changed;
                    cb.Unchecked += RowCheckBox_Changed;
                    Grid.SetColumn(cb, 0);
                    rowGrid.Children.Add(cb);
                    colOffset = 1;
                }

                // Card border — gets background, bulge on hover, shrink on select
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var cardBorder = new Border
                {
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(0, 8, 0, 8),
                    Tag = rowName,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                    RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 },
                };
                if (isNavigable)
                {
                    cardBorder.Tapped += DataRow_Tapped;
                    cardBorder.PointerEntered += CardBorder_PointerEntered;
                    cardBorder.PointerExited += CardBorder_PointerExited;
                }
                Grid.SetColumn(cardBorder, colOffset);

                var contentGrid = new Grid();
                for (int c = 0; c < columns.Count; c++)
                {
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var value = row.TryGetValue(columns[c], out var v) ? v : "";
                    var cell = new TextBlock
                    {
                        Text = value,
                        FontSize = 13,
                        Padding = new Thickness(8, 0, 8, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    if (c == 0)
                        cell.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                    else
                        cell.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

                    Grid.SetColumn(cell, c);
                    contentGrid.Children.Add(cell);
                }

                if (isNavigable)
                {
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    var chevron = new FontIcon
                    {
                        Glyph = "\uE76C",
                        FontSize = 12,
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    };
                    Grid.SetColumn(chevron, columns.Count);
                    contentGrid.Children.Add(chevron);
                }

                cardBorder.Child = contentGrid;
                rowGrid.Children.Add(cardBorder);

                dataListView.Items.Add(rowGrid);
            }
        }

        Grid.SetRow(dataListView, 2);
        tableGrid.Children.Add(dataListView);

        ContentArea.RowDefinitions.Clear();
        ContentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        ContentArea.Children.Add(tableGrid);
    }

    private void NavigateToSubDetail(string itemName)
    {
        if (_navCtx is null) return;

        if (_section == AppGwSection.BackendPools)
        {
            Frame.Navigate(typeof(AppGwBackendPoolDetailPage), _navCtx! with { DetailItemName = itemName });
        }
        else if (_section == AppGwSection.RoutingRules)
        {
            Frame.Navigate(typeof(AppGwRoutingRuleDetailPage), _navCtx! with { DetailItemName = itemName });
        }
    }

    private void DataRow_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Border { Tag: string name })
        {
            NavigateToSubDetail(name);
        }
    }

    private void CardBorder_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ListViewItemBackgroundPointerOver"];
            if (border.RenderTransform is ScaleTransform st)
                AnimateScale(st, 1.01, 150);
        }
    }

    private void CardBorder_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            if (border.RenderTransform is ScaleTransform st)
            {
                var rowName = border.Tag as string;
                var target = rowName is not null && _selectedForDelete.Contains(rowName) ? 0.99 : 1.0;
                AnimateScale(st, target, 150);
            }
        }
    }

    private static void AnimateScale(ScaleTransform st, double target, int durationMs)
    {
        var animX = new DoubleAnimation { To = target, Duration = TimeSpan.FromMilliseconds(durationMs) };
        animX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        var animY = new DoubleAnimation { To = target, Duration = TimeSpan.FromMilliseconds(durationMs) };
        animY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

        var sb = new Storyboard();
        Storyboard.SetTarget(animX, st);
        Storyboard.SetTargetProperty(animX, "ScaleX");
        Storyboard.SetTarget(animY, st);
        Storyboard.SetTargetProperty(animY, "ScaleY");
        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Begin();
    }

    private Border? FindCardBorder(CheckBox cb)
    {
        if (cb.Parent is Grid parentGrid)
        {
            foreach (var child in parentGrid.Children)
            {
                if (child is Border b && b.Tag is string) return b;
            }
        }
        return null;
    }

    private void RowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string name)
        {
            if (cb.IsChecked == true)
                _selectedForDelete.Add(name);
            else
                _selectedForDelete.Remove(name);

            DeleteSelectedButton.IsEnabled = _selectedForDelete.Count > 0;

            // Animate the card border scale
            var cardBorder = FindCardBorder(cb);
            if (cardBorder?.RenderTransform is ScaleTransform st)
            {
                var targetScale = cb.IsChecked == true ? 0.99 : 1.0;
                AnimateScale(st, targetScale, 150);
            }

            // Update select-all state
            if (!_suppressSelectAllChanged && _selectAllCheckBox is not null && _dataListView is not null)
            {
                _suppressSelectAllChanged = true;
                var totalRows = _dataListView.Items.Count;
                _selectAllCheckBox.IsChecked = _selectedForDelete.Count == totalRows ? true
                    : _selectedForDelete.Count == 0 ? false : null;
                _suppressSelectAllChanged = false;
            }
        }
    }

    private void SelectAll_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSelectAllChanged || _dataListView is null) return;
        _suppressSelectAllChanged = true;

        var selectAll = _selectAllCheckBox?.IsChecked == true;
        _selectedForDelete.Clear();

        foreach (var item in _dataListView.Items)
        {
            if (item is Grid rowGrid)
            {
                foreach (var child in rowGrid.Children)
                {
                    if (child is CheckBox cb && cb.Tag is string name)
                    {
                        cb.IsChecked = selectAll;
                        if (selectAll) _selectedForDelete.Add(name);

                        var cardBorder = FindCardBorder(cb);
                        if (cardBorder?.RenderTransform is ScaleTransform st)
                        {
                            AnimateScale(st, selectAll ? 0.99 : 1.0, 150);
                        }
                    }
                }
            }
        }

        DeleteSelectedButton.IsEnabled = _selectedForDelete.Count > 0;
        _suppressSelectAllChanged = false;
    }

    private void DeleteButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DeleteSelectedButton.Background = DeleteSelectedButton.IsEnabled
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 196, 43, 28))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(68, 196, 43, 28));
    }

    private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedForDelete.Count == 0) return;

        var selectedNames = _selectedForDelete.ToList();
        var deleted = false;

        foreach (var name in selectedNames)
        {
            if (ViewModel.DeleteItem(_section, name))
            {
                deleted = true;
            }
        }

        if (deleted)
        {
            var desc = selectedNames.Count == 1
                ? $"Deleted '{selectedNames[0]}'."
                : $"Deleted {selectedNames.Count} items.";
            try
            {
                await ViewModel.SaveChangesAsync(desc);
            }
            catch { }

            _selectedForDelete.Clear();
            DeleteSelectedButton.IsEnabled = false;
            RenderSection();
        }
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name })
        {
            _confirmDeleteName = name;
            RefreshTable();
        }
    }

    private void CancelDelete_Click(object sender, RoutedEventArgs e)
    {
        _confirmDeleteName = null;
        RefreshTable();
    }

    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name })
        {
            _confirmDeleteName = null;
            var deleted = ViewModel.DeleteItem(_section, name);
            if (deleted)
            {
                try
                {
                    await ViewModel.SaveChangesAsync($"Deleted '{name}'.");
                }
                catch
                {
                }

                
                RenderSection();
            }
        }
    }

    private void EditRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name })
        {
            var row = _currentCollection?.FirstOrDefault(r => r.TryGetValue("Name", out var n) && n == name);
            if (row is not null)
            {
                _ = ShowEditDialogAsync(name, new Dictionary<string, string>(row));
            }
        }
    }

    private async void AddNew_Click(object sender, RoutedEventArgs e)
    {
        var fields = AppGwViewModel.GetEditableFields(_section);
        var values = new Dictionary<string, string>();
        foreach (var (field, _) in fields)
        {
            values[field] = "";
        }

        await ShowAddDialogAsync(values);
    }

    private async Task ShowEditDialogAsync(string originalName, Dictionary<string, string> values)
    {
        var fields = AppGwViewModel.GetEditableFields(_section);
        if (fields.Count == 0) return;

        var editedValues = await ShowFieldDialogAsync($"Edit: {originalName}", fields, values, isEdit: true);
        if (editedValues is not null)
        {
            try
            {
                var edited = ViewModel.EditItem(_section, originalName, editedValues);
                if (edited)
                {
                    await ViewModel.SaveChangesAsync($"Updated '{originalName}'.");
                }
            }
            catch
            {
            }

            
            RenderSection();
        }
    }

    private async Task ShowAddDialogAsync(Dictionary<string, string> values)
    {
        var fields = AppGwViewModel.GetEditableFields(_section);
        if (fields.Count == 0) return;

        var editedValues = await ShowFieldDialogAsync("Add New", fields, values, isEdit: false);
        if (editedValues is not null)
        {
            var name = editedValues.GetValueOrDefault("Name") ?? "";
            try
            {
                var added = ViewModel.AddItem(_section, editedValues);
                if (added)
                {
                    await ViewModel.SaveChangesAsync($"Added '{name}'.");
                }
            }
            catch
            {
            }

            
            RenderSection();
        }
    }

    // Field definitions that should use a ComboBox or RadioButtons instead of TextBox
    // Fields with <= 3 options use RadioButtons, > 3 use ComboBox
    private static readonly Dictionary<string, string[]> ChoiceFields = new()
    {
        ["Protocol"] = ["Http", "Https"],
        ["Cookie Affinity"] = ["Enabled", "Disabled"],
        ["Rule Type"] = ["Basic", "PathBasedRouting"],
        ["Require SNI"] = ["True", "False"],
        ["Pick Host From Backend"] = ["True", "False"],
    };

    private async Task<Dictionary<string, string>?> ShowFieldDialogAsync(
        string title,
        List<(string Field, string Placeholder)> fields,
        Dictionary<string, string> values,
        bool isEdit)
    {
        var stack = new StackPanel { Spacing = 12, MinWidth = 400 };
        var controls = new Dictionary<string, object>(); // TextBox, ComboBox, or RadioButtons (StackPanel)

        foreach (var (field, placeholder) in fields)
        {
            var isNameOnEdit = field == "Name" && isEdit;
            var fieldStack = new StackPanel { Spacing = 4 };
            fieldStack.Children.Add(new TextBlock
            {
                Text = field,
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });

            var currentValue = values.GetValueOrDefault(field) ?? "";

            if (ChoiceFields.TryGetValue(field, out var options) && !isNameOnEdit)
            {
                if (options.Length <= 3)
                {
                    // Use RadioButtons for <= 3 options
                    var radioPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
                    var groupName = $"Radio_{field.Replace(" ", "")}";
                    foreach (var opt in options)
                    {
                        var radio = new RadioButton
                        {
                            Content = opt,
                            GroupName = groupName,
                            IsChecked = opt.Equals(currentValue, StringComparison.OrdinalIgnoreCase),
                        };
                        radioPanel.Children.Add(radio);
                    }

                    // Default first if none selected
                    if (!radioPanel.Children.OfType<RadioButton>().Any(r => r.IsChecked == true) && radioPanel.Children.Count > 0)
                    {
                        ((RadioButton)radioPanel.Children[0]).IsChecked = true;
                    }

                    controls[field] = radioPanel;
                    fieldStack.Children.Add(radioPanel);
                }
                else
                {
                    // Use ComboBox for > 3 options
                    var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
                    foreach (var opt in options)
                    {
                        combo.Items.Add(opt);
                    }

                    combo.SelectedItem = options.FirstOrDefault(o => o.Equals(currentValue, StringComparison.OrdinalIgnoreCase));
                    if (combo.SelectedItem is null && options.Length > 0)
                    {
                        combo.SelectedIndex = 0;
                    }

                    controls[field] = combo;
                    fieldStack.Children.Add(combo);
                }
            }
            else
            {
                var textBox = new TextBox
                {
                    PlaceholderText = placeholder,
                    Text = currentValue,
                    IsEnabled = !isNameOnEdit,
                };
                controls[field] = textBox;
                fieldStack.Children.Add(textBox);
            }

            stack.Children.Add(fieldStack);
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stack,
            PrimaryButtonText = isEdit ? "Save" : "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var editedValues = new Dictionary<string, string>();
            foreach (var (field, _) in fields)
            {
                editedValues[field] = controls[field] switch
                {
                    TextBox tb => tb.Text,
                    ComboBox cb => cb.SelectedItem?.ToString() ?? "",
                    StackPanel rp => rp.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Content?.ToString() ?? "",
                    _ => "",
                };
            }

            return editedValues;
        }

        return null;
    }

    private void HeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string col })
        {
            if (_sortColumn == col)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = col;
                _sortAscending = true;
            }

            RefreshTable();
        }
    }

    private static readonly Dictionary<string, string> FieldIcons = new()
    {
        ["Name"] = "\uE8CB",
        ["Location"] = "\uE81D",
        ["SKU Name"] = "\uE8CB",
        ["SKU Tier"] = "\uE7AC",
        ["Capacity"] = "\uE95E",
        ["Operational State"] = "\uEA18",
        ["Provisioning State"] = "\uE9F5",
        ["Resource ID"] = "\uE71B",
        ["HTTP/2"] = "\uE774",
        ["FIPS"] = "\uE72E",
        ["Autoscale Min"] = "\uE740",
        ["Autoscale Max"] = "\uE741",
        ["WAF Enabled"] = "\uE83D",
        ["Mode"] = "\uE713",
        ["Rule Set Type"] = "\uE8FD",
        ["Rule Set Version"] = "\uE8FD",
        ["Firewall Policy"] = "\uE72E",
    };

    private void AddPropertyCard(List<(string Label, string Value)> properties)
    {
        var stack = new StackPanel { Spacing = 16 };

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0)
            {
                stack.Children.Add(new Border
                {
                    Height = 1,
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
                });
            }

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var glyph = FieldIcons.GetValueOrDefault(properties[i].Label, "\uE946");
            var icon = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
            };

            var label = new TextBlock
            {
                Text = properties[i].Label,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(label, 1);

            var value = new TextBlock
            {
                Text = properties[i].Value,
                IsTextSelectionEnabled = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(value, 2);

            grid.Children.Add(icon);
            grid.Children.Add(label);
            grid.Children.Add(value);
            stack.Children.Add(grid);
        }

        var card = new Border
        {
            Padding = new Thickness(20),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = stack,
        };

        var scrollViewer = new ScrollViewer { Content = card };
        ContentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        ContentArea.Children.Add(scrollViewer);
    }

    private void AddSectionHeader(string text)
    {
        ContentArea.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 0),
        });
    }

    private static string SectionToTitle(AppGwSection section) => section switch
    {
        AppGwSection.Overview => "Overview",
        AppGwSection.BackendPools => "Backend Pools",
        AppGwSection.BackendSettings => "Backend Settings",
        AppGwSection.FrontendIP => "Frontend IP Configuration",
        AppGwSection.PrivateLink => "Private Link",
        AppGwSection.SslSettings => "SSL Settings",
        AppGwSection.Listeners => "Listeners",
        AppGwSection.RoutingRules => "Routing Rules",
        AppGwSection.RewriteSets => "Rewrite Sets",
        AppGwSection.HealthProbes => "Health Probes",
        AppGwSection.Configuration => "Configuration",
        AppGwSection.Waf => "WAF",
        AppGwSection.JwtValidation => "JWT Validation",
        _ => section.ToString(),
    };

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}

internal sealed class NaturalStringComparer : IComparer<string>
{
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int ix = 0, iy = 0;
        while (ix < x.Length && iy < y.Length)
        {
            if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
            {
                // Compare numeric segments by value
                int startX = ix, startY = iy;
                while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                var lenDiff = (ix - startX) - (iy - startY);
                if (lenDiff != 0) return lenDiff; // longer number is larger
                for (int i = 0; i < ix - startX; i++)
                {
                    var diff = x[startX + i] - y[startY + i];
                    if (diff != 0) return diff;
                }
            }
            else
            {
                var cmp = char.ToLowerInvariant(x[ix]).CompareTo(char.ToLowerInvariant(y[iy]));
                if (cmp != 0) return cmp;
                ix++;
                iy++;
            }
        }
        return x.Length - y.Length;
    }
}
