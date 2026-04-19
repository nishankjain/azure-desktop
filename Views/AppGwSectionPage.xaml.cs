using System.Collections.ObjectModel;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwSectionPage : Page, INavigablePage
{
    private CancellationTokenSource? _cts;
    public AppGwViewModel ViewModel { get; }
    public string SectionTitle { get; private set; } = "";
    private NavigationContext? _navCtx;
    private AppGwSection _section;


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
    private string? _confirmDeleteName;
    private bool _isCollectionSection;

    private void RenderSection()
    {
        // Reset visibility
        PropertyCardArea.Visibility = Visibility.Collapsed;
        PropertyCardStack.Children.Clear();
        SectionTable.Visibility = Visibility.Collapsed;
        _isCollectionSection = false;

        switch (_section)
        {
            case AppGwSection.Overview:
                ShowPropertyCard([
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
                ShowPropertyCard([
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
                ShowPropertyCard([
                    ("WAF Enabled", ViewModel.WafEnabled.ToString()),
                    ("Mode", ViewModel.WafMode),
                    ("Rule Set Type", ViewModel.WafRuleSetType),
                    ("Rule Set Version", ViewModel.WafRuleSetVersion),
                    ("Firewall Policy", ViewModel.WafPolicyId),
                ]);
                break;

            case AppGwSection.BackendPools:
                ShowTable(ViewModel.BackendPools, "Name, Targets, Associated Rules", "No backend pools configured.", isNavigable: true);
                break;
            case AppGwSection.BackendSettings:
                ShowTable(ViewModel.BackendSettings, "Name, Protocol, Port, Cookie Affinity, Request Timeout, Host Name", "No backend settings configured.");
                break;
            case AppGwSection.FrontendIP:
                // Composite: two tables — handled by showing first, adding second below
                ShowTable(ViewModel.FrontendIPs, "Name, Private IP, Allocation Method, Public IP", "No frontend IPs configured.");
                break;
            case AppGwSection.PrivateLink:
                ShowTable(ViewModel.PrivateLinks, "Name, IP Configurations", "No private link configurations.");
                break;
            case AppGwSection.SslSettings:
                ShowTable(ViewModel.SslCertificates, "Name", "No SSL certificates or policies.");
                break;
            case AppGwSection.Listeners:
                ShowTable(ViewModel.HttpListeners, "Name, Protocol, Host Name, Frontend Port, Require SNI", "No listeners configured.");
                break;
            case AppGwSection.RoutingRules:
                ShowTable(ViewModel.RoutingRules, "Name, Rule Type, Priority, Listener, Backend Pool, Backend Settings", "No routing rules configured.", isNavigable: true);
                break;
            case AppGwSection.RewriteSets:
                ShowTable(ViewModel.RewriteSets, "Name, Rule Count", "No rewrite sets configured.");
                break;
            case AppGwSection.HealthProbes:
                ShowTable(ViewModel.HealthProbes, "Name, Protocol, Host, Path, Interval, Timeout, Unhealthy Threshold", "No health probes configured.");
                break;
            case AppGwSection.JwtValidation:
                ShowTable(ViewModel.JwtConfigs, "Name", "No JWT validation configs.");
                break;
        }
    }

    private void ShowPropertyCard(List<(string Label, string Value)> properties)
    {
        PropertyCardArea.Visibility = Visibility.Visible;
        AddPropertyCard(properties);
    }

    private void ShowTable(ObservableCollection<Dictionary<string, string>> items, string columns, string emptyMessage, bool isNavigable = false)
    {
        _isCollectionSection = true;
        _currentCollection = items;

        SectionTable.ItemsSource = items;
        SectionTable.Columns = columns;
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = isNavigable;
        SectionTable.EmptyMessage = emptyMessage;
        SectionTable.ShowAddButton = GetFieldsForSection(_section).Count > 0;
        SectionTable.Visibility = Visibility.Visible;

        // Wire events (clear previous handlers first)
        SectionTable.ItemClick -= OnTableItemClick;
        SectionTable.DeleteClick -= OnTableDeleteClick;
        SectionTable.AddClick -= OnTableAddClick;
        SectionTable.ItemClick += OnTableItemClick;
        SectionTable.DeleteClick += OnTableDeleteClick;
        SectionTable.AddClick += OnTableAddClick;

        SectionTable.Refresh();
    }

    private void OnTableItemClick(object? sender, string name) => NavigateToSubDetail(name);

    private async void OnTableDeleteClick(object? sender, List<string> names)
    {
        var deleted = false;
        foreach (var name in names)
        {
            if (DeleteForSection(_section, name)) deleted = true;
        }
        if (deleted)
        {
            var desc = names.Count == 1 ? $"Deleted '{names[0]}'." : $"Deleted {names.Count} items.";
            try { await ViewModel.SaveChangesAsync(desc); } catch { }
            RenderSection();
        }
    }

    private async void OnTableAddClick(object? sender, EventArgs e)
    {
        var fields = GetFieldsForSection(_section);
        var values = new Dictionary<string, string>();
        foreach (var (field, _) in fields) values[field] = "";
        await ShowAddDialogAsync(values);
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


    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name })
        {
            _confirmDeleteName = name;
            RenderSection();
        }
    }

    private void CancelDelete_Click(object sender, RoutedEventArgs e)
    {
        _confirmDeleteName = null;
        RenderSection();
    }

    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name })
        {
            _confirmDeleteName = null;
            var deleted = DeleteForSection(_section, name);
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


    private async Task ShowEditDialogAsync(string originalName, Dictionary<string, string> values)
    {
        var fields = GetFieldsForSection(_section);
        if (fields.Count == 0) return;

        var editedValues = await ShowFieldDialogAsync($"Edit: {originalName}", fields, values, isEdit: true);
        if (editedValues is not null)
        {
            try
            {
                var edited = EditForSection(_section, originalName, editedValues);
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
        var fields = GetFieldsForSection(_section);
        if (fields.Count == 0) return;

        var editedValues = await ShowFieldDialogAsync("Add New", fields, values, isEdit: false);
        if (editedValues is not null)
        {
            var name = editedValues.GetValueOrDefault("Name") ?? "";
            try
            {
                var added = AddForSection(_section, editedValues);
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


    private static readonly Dictionary<string, string> FieldIcons= new()
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

        PropertyCardStack.Children.Add(card);
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

    // Local dispatch helpers — route section enum to typed ViewModel methods
    private static List<(string Field, string Placeholder)> GetFieldsForSection(AppGwSection section) => section switch
    {
        AppGwSection.BackendPools => AppGwViewModel.GetBackendPoolFields(),
        AppGwSection.BackendSettings => AppGwViewModel.GetBackendSettingFields(),
        AppGwSection.Listeners => AppGwViewModel.GetListenerFields(),
        AppGwSection.RoutingRules => AppGwViewModel.GetRoutingRuleFields(),
        AppGwSection.HealthProbes => AppGwViewModel.GetHealthProbeFields(),
        AppGwSection.FrontendIP => AppGwViewModel.GetFrontendPortFields(),
        AppGwSection.SslSettings => AppGwViewModel.GetSslCertificateFields(),
        AppGwSection.RewriteSets => AppGwViewModel.GetRewriteSetFields(),
        _ => [],
    };

    private bool DeleteForSection(AppGwSection section, string name) => section switch
    {
        AppGwSection.BackendPools => ViewModel.DeleteBackendPool(name),
        AppGwSection.BackendSettings => ViewModel.DeleteBackendSetting(name),
        AppGwSection.Listeners => ViewModel.DeleteListener(name),
        AppGwSection.RoutingRules => ViewModel.DeleteRoutingRule(name),
        AppGwSection.HealthProbes => ViewModel.DeleteHealthProbe(name),
        AppGwSection.RewriteSets => ViewModel.DeleteRewriteSet(name),
        AppGwSection.SslSettings => ViewModel.DeleteSslCertificate(name),
        AppGwSection.PrivateLink => ViewModel.DeletePrivateLink(name),
        _ => false,
    };

    private bool AddForSection(AppGwSection section, Dictionary<string, string> values) => section switch
    {
        AppGwSection.BackendPools => ViewModel.AddBackendPool(values),
        AppGwSection.BackendSettings => ViewModel.AddBackendSetting(values),
        AppGwSection.HealthProbes => ViewModel.AddHealthProbe(values),
        AppGwSection.FrontendIP => ViewModel.AddFrontendPort(values),
        AppGwSection.Listeners => ViewModel.AddListener(values),
        AppGwSection.RoutingRules => ViewModel.AddRoutingRule(values),
        _ => false,
    };

    private bool EditForSection(AppGwSection section, string originalName, Dictionary<string, string> values) => section switch
    {
        AppGwSection.BackendPools => ViewModel.EditBackendPool(originalName, values),
        AppGwSection.BackendSettings => ViewModel.EditBackendSetting(originalName, values),
        AppGwSection.Listeners => ViewModel.EditListener(originalName, values),
        AppGwSection.HealthProbes => ViewModel.EditHealthProbe(originalName, values),
        AppGwSection.RoutingRules => ViewModel.EditRoutingRule(originalName, values),
        _ => false,
    };

    // INavigablePage
    public BreadcrumbEntry[] GetBreadcrumbs()
    {
        var ctx = _navCtx!;
        var subCtx = ctx with { ResourceGroupName = null, Resource = null, Section = null, DetailItemName = null };
        var rgCtx = ctx with { Resource = null, Section = null, DetailItemName = null };
        var gwCtx = ctx with { Section = null, DetailItemName = null };
        return [
            new("Subscriptions", typeof(SubscriptionsPage), subCtx),
            new("Subscription", typeof(SubscriptionDetailPage), subCtx),
            new("Resource Groups", typeof(ResourceGroupsPage), rgCtx),
            new("Resource Group", typeof(ResourceGroupDetailPage), rgCtx),
            new("Application Gateway", typeof(AppGwOverviewPage), gwCtx),
            new(SectionTitle, null, null),
        ];
    }

    public NavItemDefinition[] GetNavItems() => AppGwNavItems.Get();

    public string? ActiveNavTag => _section switch
    {
        AppGwSection.Overview => "AppGwOverview",
        AppGwSection.BackendPools => "AppGwBackendPools",
        AppGwSection.BackendSettings => "AppGwBackendSettings",
        AppGwSection.FrontendIP => "AppGwFrontendIP",
        AppGwSection.PrivateLink => "AppGwPrivateLink",
        AppGwSection.SslSettings => "AppGwSsl",
        AppGwSection.Listeners => "AppGwListeners",
        AppGwSection.RoutingRules => "AppGwRoutingRules",
        AppGwSection.RewriteSets => "AppGwRewriteSets",
        AppGwSection.HealthProbes => "AppGwHealthProbes",
        AppGwSection.Configuration => "AppGwConfig",
        AppGwSection.Waf => "AppGwWaf",
        AppGwSection.JwtValidation => "AppGwJwt",
        _ => null,
    };
}
