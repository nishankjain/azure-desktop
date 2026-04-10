using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwSectionPage : Page
{
    public AppGwViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];
    public string SectionTitle { get; private set; } = "";

    private NavigationContext? _navCtx;
    private AppGwSection _section;

    public AppGwSectionPage()
    {
        ViewModel = App.GetService<AppGwViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (NavigationContext ctx, AppGwSection section))
        {
            _navCtx = ctx;
            _section = section;
            SectionTitle = SectionToTitle(section);
            SectionTitleText.Text = SectionTitle;
            ClearNotifications();
            

            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(ctx.SubscriptionName);
            BreadcrumbItems.Add(ctx.ResourceGroupName ?? "");
            BreadcrumbItems.Add(ctx.Resource?.Name ?? "");
            BreadcrumbItems.Add(SectionTitle);
            Breadcrumb.ItemsSource = BreadcrumbItems;

            // Data should already be loaded by the singleton ViewModel
            RenderSection();
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (_navCtx is null) return;

        switch (args.Index)
        {
            case 0:
                Frame.BackStack.Clear();
                Frame.Navigate(typeof(SubscriptionsPage));
                break;
            case 1:
                Frame.Navigate(typeof(SubscriptionDetailPage), _navCtx.Subscription);
                break;
            case 2:
                Frame.Navigate(typeof(ResourceGroupDetailPage), _navCtx with { Resource = null });
                break;
            case 3:
                Frame.Navigate(typeof(ResourceDetailPage), _navCtx);
                break;
        }
    }

    private ObservableCollection<Dictionary<string, string>>? _currentCollection;
    private List<string>? _currentColumns;
    private string _sortColumn = "";
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
        _sortColumn = "";
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
                ? filtered.OrderBy(r => r.TryGetValue(_sortColumn, out var v) ? v : "")
                : filtered.OrderByDescending(r => r.TryGetValue(_sortColumn, out var v) ? v : "");
        }

        var rows = filtered.ToList();
        var columns = _currentColumns;

        // Build table as a Grid with frozen header
        var tableGrid = new Grid();
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // divider
        tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // data

        // Header row
        var headerGrid = new Grid { Margin = new Thickness(12, 8, 12, 0) };
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
            Grid.SetColumn(headerBtn, c);
            headerGrid.Children.Add(headerBtn);
        }

        if (_isCollectionSection)
        {
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var actionsHeader = new TextBlock
            {
                Text = "Actions",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Padding = new Thickness(8, 4, 8, 4),
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            };
            Grid.SetColumn(actionsHeader, columns.Count);
            headerGrid.Children.Add(actionsHeader);
        }

        Grid.SetRow(headerGrid, 0);
        tableGrid.Children.Add(headerGrid);

        var divider = new Border
        {
            Height = 1,
            Margin = new Thickness(12, 4, 12, 0),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
        };
        Grid.SetRow(divider, 1);
        tableGrid.Children.Add(divider);

        // Data rows (ListView provides UI virtualization)
        var dataListView = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            Padding = new Thickness(12, 4, 12, 12),
        };

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
                // Normal data row
                var rowGrid = new Grid { Padding = new Thickness(0, 6, 0, 6) };

                // Make rows clickable for sections with detail pages
                if (_section == AppGwSection.BackendPools || _section == AppGwSection.RoutingRules)
                {
                    rowGrid.PointerPressed += (s, _) => NavigateToSubDetail(rowName);
                    rowGrid.PointerEntered += (s, _) => ((Grid)s).Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
                    rowGrid.PointerExited += (s, _) => ((Grid)s).Background = null;
                }

                for (int c = 0; c < columns.Count; c++)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
                    rowGrid.Children.Add(cell);
                }

                // Action buttons
                if (_isCollectionSection)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };

                    var editFields = AppGwViewModel.GetEditableFields(_section);
                    if (editFields.Count > 0)
                    {
                        var editBtn = new Button
                        {
                            Width = 32, Height = 32, Padding = new Thickness(0),
                            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                            BorderThickness = new Thickness(0),
                            Tag = rowName,
                        };
                        editBtn.Content = new FontIcon { Glyph = "\uE70F", FontSize = 14 };
                        ToolTipService.SetToolTip(editBtn, "Edit");
                        editBtn.Click += EditRow_Click;
                        actionsPanel.Children.Add(editBtn);
                    }

                    var deleteBtn = new Button
                    {
                        Width = 32, Height = 32, Padding = new Thickness(0),
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        Tag = rowName,
                    };
                    deleteBtn.Content = new FontIcon { Glyph = "\uE711", FontSize = 14 };
                    ToolTipService.SetToolTip(deleteBtn, "Delete");
                    deleteBtn.Click += DeleteRow_Click;

                    actionsPanel.Children.Add(deleteBtn);
                    Grid.SetColumn(actionsPanel, columns.Count);
                    rowGrid.Children.Add(actionsPanel);
                }

                dataListView.Items.Add(rowGrid);
            }
        }

        Grid.SetRow(dataListView, 2);
        tableGrid.Children.Add(dataListView);

        var tableBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = tableGrid,
            Tag = "table",
        };

        ContentArea.RowDefinitions.Clear();
        ContentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        ContentArea.Children.Add(tableBorder);
    }

    private void NavigateToSubDetail(string itemName)
    {
        if (_navCtx is null) return;

        if (_section == AppGwSection.BackendPools)
        {
            Frame.Navigate(typeof(AppGwBackendPoolDetailPage), (_navCtx, itemName));
        }
        else if (_section == AppGwSection.RoutingRules)
        {
            Frame.Navigate(typeof(AppGwRoutingRuleDetailPage), (_navCtx, itemName));
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
                ShowSaving();
                try
                {
                    var saved = await ViewModel.SaveChangesAsync($"Deleted '{name}'.");
                    if (saved)
                    {
                        ShowStatus($"Deleted '{name}' successfully.");
                    }
                    else
                    {
                        ShowStatus(ViewModel.ErrorMessage ?? "Failed to delete.", isError: true);
                    }
                }
                catch (Exception ex)
                {
                    ShowStatus($"Error: {ex.Message}", isError: true);
                }

                
                RenderSection();
            }
        }
    }

    private DispatcherTimer? _autoDismissTimer;

    private void ShowStatus(string message, bool isError = false)
    {
        _autoDismissTimer?.Stop();
        StatusSpinner.Visibility = Visibility.Collapsed;
        StatusIcon.Visibility = Visibility.Visible;

        if (isError)
        {
            StatusIcon.Glyph = "\uEA39";
            StatusIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        else
        {
            StatusIcon.Glyph = "\uE73E";
            StatusIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }

        StatusText.Text = message;
        StatusPanel.Visibility = Visibility.Visible;

        if (!isError)
        {
            _autoDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _autoDismissTimer.Tick += (_, _) => { StatusPanel.Visibility = Visibility.Collapsed; _autoDismissTimer.Stop(); };
            _autoDismissTimer.Start();
        }
    }

    private void ShowSaving()
    {
        _autoDismissTimer?.Stop();
        StatusSpinner.Visibility = Visibility.Visible;
        StatusIcon.Visibility = Visibility.Collapsed;
        StatusText.Text = "Saving to Azure...";
        StatusPanel.Visibility = Visibility.Visible;
    }

    private void ClearNotifications()
    {
        _autoDismissTimer?.Stop();
        StatusPanel.Visibility = Visibility.Collapsed;
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
            ShowSaving();
            try
            {
                var edited = ViewModel.EditItem(_section, originalName, editedValues);
                if (edited)
                {
                    var saved = await ViewModel.SaveChangesAsync($"Updated '{originalName}'.");
                    if (saved)
                    {
                        ShowStatus($"Updated '{originalName}' successfully.");
                    }
                    else
                    {
                        ShowStatus(ViewModel.ErrorMessage ?? "Failed to save.", isError: true);
                    }
                }
                else
                {
                    ShowStatus($"Could not edit '{originalName}'.", isError: true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", isError: true);
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
            ShowSaving();
            try
            {
                var added = ViewModel.AddItem(_section, editedValues);
                if (added)
                {
                    var saved = await ViewModel.SaveChangesAsync($"Added '{name}'.");
                    if (saved)
                    {
                        ShowStatus($"Added '{name}' successfully.");
                    }
                    else
                    {
                        ShowStatus(ViewModel.ErrorMessage ?? "Failed to save.", isError: true);
                    }
                }
                else
                {
                    ShowStatus("Could not add item. Check required fields.", isError: true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", isError: true);
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

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock { Text = properties[i].Label, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
            var value = new TextBlock { Text = properties[i].Value, IsTextSelectionEnabled = true, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(value, 1);

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
            CornerRadius = new CornerRadius(8),
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
}
