using Azure.ResourceManager.Network.Models;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwRoutingRuleDetailPage : AppGwPageBase
{
    public override string PageLabel => "Routing Rule";
    public override string? ActiveNavTag => "AppGwRoutingRules";

    private string _ruleName = "";

    // Form controls
    private RadioButton? _ruleTypeBasic;
    private RadioButton? _ruleTypePathBased;
    private TextBox? _priorityBox;
    private ComboBox? _listenerCombo;

    // Target type: Backend Pool or Redirection
    private RadioButton? _targetTypeBackend;
    private RadioButton? _targetTypeRedirect;

    // Backend target controls
    private StackPanel? _backendSection;
    private ComboBox? _backendPoolCombo;
    private ComboBox? _backendSettingsCombo;

    // Redirect target controls
    private StackPanel? _redirectSection;
    private ComboBox? _redirectTypeCombo;
    private RadioButton? _redirectTargetListener;
    private RadioButton? _redirectTargetExternal;
    private StackPanel? _redirectListenerSection;
    private ComboBox? _redirectListenerCombo;
    private StackPanel? _redirectUrlSection;
    private TextBox? _redirectUrlBox;
    private CheckBox? _redirectIncludePath;
    private CheckBox? _redirectIncludeQueryString;

    public AppGwRoutingRuleDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnContextReady(NavigationContext? ctx)
    {
        if (ctx?.DetailItemName is not null)
        {
            _ruleName = ctx.DetailItemName;
            TitleText.Text = ctx.DetailItemName;
        }
        base.OnContextReady(ctx);
    }

    protected override void OnDataLoaded()
    {
        RenderForm();
    }

    public override BreadcrumbEntry[] GetBreadcrumbs()
    {
        var baseCrumbs = base.GetBreadcrumbs();
        var list = baseCrumbs.ToList();
        list.Insert(list.Count - 1, new("Routing Rules", typeof(AppGwRoutingRulesPage), NavCtx with { DetailItemName = null }));
        return list.ToArray();
    }
    private static void ScrollDropDownToTop(ComboBox combo)
    {
        combo.DropDownOpened += (s, _) =>
        {
            if (s is not ComboBox cb) return;
            cb.DispatcherQueue.TryEnqueue(() =>
            {
                var popup = FindChild<Microsoft.UI.Xaml.Controls.Primitives.Popup>(cb);
                if (popup?.Child is FrameworkElement popupRoot)
                {
                    var sv = FindChild<ScrollViewer>(popupRoot);
                    sv?.ChangeView(null, 0, null, true);
                }
            });
        };
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindChild<T>(child);
            if (found is not null) return found;
        }

        return null;
    }

    /// <summary>
    /// Extracts the last segment from a ResourceIdentifier name.
    /// Sub-resource names may be compound (e.g. "gwName/poolName").
    /// </summary>
    private static string? SubResourceName(Azure.Core.ResourceIdentifier? id)
    {
        if (id is null) return null;
        var name = id.Name;
        if (name is null) return null;
        var idx = name.LastIndexOf('/');
        return idx >= 0 ? name[(idx + 1)..] : name;
    }

    private void RenderForm()
    {
        FormArea.Children.Clear();
        PathRulesList.Items.Clear();

        var rule = ViewModel.Data?.RequestRoutingRules.FirstOrDefault(r => r.Name == _ruleName);
        if (rule is null || ViewModel.Data is null) return;

        var isPathBased = rule.RuleType == ApplicationGatewayRequestRoutingRuleType.PathBasedRouting;
        var hasRedirect = rule.RedirectConfigurationId is not null;

        // Name (read-only, disabled)
        AddLabel("Name");
        FormArea.Children.Add(new TextBox
        {
            Text = _ruleName,
            IsEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        });

        // Rule Type (2 options -> radio buttons)
        AddLabel("Rule Type");
        var ruleTypePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        _ruleTypeBasic = new RadioButton { Content = "Basic", GroupName = "RuleType", IsChecked = !isPathBased };
        _ruleTypePathBased = new RadioButton { Content = "Path-based", GroupName = "RuleType", IsChecked = isPathBased };
        _ruleTypeBasic.Checked += RuleType_Changed;
        _ruleTypePathBased.Checked += RuleType_Changed;
        ruleTypePanel.Children.Add(_ruleTypeBasic);
        ruleTypePanel.Children.Add(_ruleTypePathBased);
        FormArea.Children.Add(ruleTypePanel);

        // Priority
        AddLabel("Priority");
        _priorityBox = new TextBox { Text = rule.Priority?.ToString() ?? "", PlaceholderText = "100", HorizontalAlignment = HorizontalAlignment.Stretch };
        FormArea.Children.Add(_priorityBox);

        // Listener dropdown (only unassigned + current)
        AddLabel("Listener");
        _listenerCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        var currentListenerName = SubResourceName(rule.HttpListenerId);
        var assignedListeners = new HashSet<string>(
            ViewModel.Data.RequestRoutingRules
                .Where(r => r.Name != _ruleName)
                .Select(r => SubResourceName(r.HttpListenerId))
                .Where(n => n is not null)!);

        var listenerSelectedIndex = -1;
        foreach (var l in ViewModel.Data.HttpListeners)
        {
            var name = l.Name ?? "";
            if (name == currentListenerName || !assignedListeners.Contains(name))
            {
                _listenerCombo.Items.Add(name);
                if (name == currentListenerName)
                    listenerSelectedIndex = _listenerCombo.Items.Count - 1;
            }
        }

        _listenerCombo.SelectedIndex = listenerSelectedIndex >= 0 ? listenerSelectedIndex : (_listenerCombo.Items.Count > 0 ? 0 : -1);
        ScrollDropDownToTop(_listenerCombo);
        FormArea.Children.Add(_listenerCombo);

        // Target Type (2 options -> radio buttons): Backend Pool or Redirection
        AddLabel("Target Type");
        var targetTypePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        _targetTypeBackend = new RadioButton { Content = "Backend Pool", GroupName = "TargetType", IsChecked = !hasRedirect };
        _targetTypeRedirect = new RadioButton { Content = "Redirection", GroupName = "TargetType", IsChecked = hasRedirect };
        _targetTypeBackend.Checked += TargetType_Changed;
        _targetTypeRedirect.Checked += TargetType_Changed;
        targetTypePanel.Children.Add(_targetTypeBackend);
        targetTypePanel.Children.Add(_targetTypeRedirect);
        FormArea.Children.Add(targetTypePanel);

        // Backend section
        _backendSection = new StackPanel { Spacing = 12, Visibility = hasRedirect ? Visibility.Collapsed : Visibility.Visible };
        BuildBackendSection(rule);
        FormArea.Children.Add(_backendSection);

        // Redirect section
        _redirectSection = new StackPanel { Spacing = 12, Visibility = hasRedirect ? Visibility.Visible : Visibility.Collapsed };
        BuildRedirectSection(rule);
        FormArea.Children.Add(_redirectSection);

        // Path rules panel (right side)
        PathRulesPanel.Visibility = isPathBased ? Visibility.Visible : Visibility.Collapsed;
        BuildPathRulesSection(rule);
    }

    private void BuildBackendSection(ApplicationGatewayRequestRoutingRule rule)
    {
        if (_backendSection is null || ViewModel.Data is null) return;

        _backendSection.Children.Clear();
        var currentPoolName = SubResourceName(rule.BackendAddressPoolId);
        var currentSettingsName = SubResourceName(rule.BackendHttpSettingsId);

        AddLabelTo(_backendSection, "Backend Pool");
        _backendPoolCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "Select a backend pool..." };
        var poolSelectedIndex = -1;
        for (int i = 0; i < ViewModel.Data.BackendAddressPools.Count; i++)
        {
            var name = ViewModel.Data.BackendAddressPools[i].Name ?? "";
            _backendPoolCombo.Items.Add(name);
            if (currentPoolName is not null && name.Equals(currentPoolName, StringComparison.OrdinalIgnoreCase))
                poolSelectedIndex = i;
        }

        _backendPoolCombo.SelectedIndex = poolSelectedIndex;
        ScrollDropDownToTop(_backendPoolCombo);
        _backendSection.Children.Add(_backendPoolCombo);

        AddLabelTo(_backendSection, "Backend Settings");
        _backendSettingsCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "Select backend settings..." };
        var settingsSelectedIndex = -1;
        for (int i = 0; i < ViewModel.Data.BackendHttpSettingsCollection.Count; i++)
        {
            var name = ViewModel.Data.BackendHttpSettingsCollection[i].Name ?? "";
            _backendSettingsCombo.Items.Add(name);
            if (currentSettingsName is not null && name.Equals(currentSettingsName, StringComparison.OrdinalIgnoreCase))
                settingsSelectedIndex = i;
        }

        _backendSettingsCombo.SelectedIndex = settingsSelectedIndex;
        ScrollDropDownToTop(_backendSettingsCombo);
        _backendSection.Children.Add(_backendSettingsCombo);
    }

    private void BuildRedirectSection(ApplicationGatewayRequestRoutingRule rule)
    {
        if (_redirectSection is null || ViewModel.Data is null) return;

        _redirectSection.Children.Clear();

        // Find existing redirect config for this rule
        var currentRedirectName = SubResourceName(rule.RedirectConfigurationId);
        var existingConfig = currentRedirectName is not null
            ? ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name?.Equals(currentRedirectName, StringComparison.OrdinalIgnoreCase) == true)
            : null;

        // Redirect Type (4 options = ComboBox)
        AddLabelTo(_redirectSection, "Redirect Type");
        _redirectTypeCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "Select redirect type..." };
        _redirectTypeCombo.Items.Add("Permanent (301)");
        _redirectTypeCombo.Items.Add("Found (302)");
        _redirectTypeCombo.Items.Add("SeeOther (303)");
        _redirectTypeCombo.Items.Add("Temporary (307)");
        _redirectTypeCombo.SelectedIndex = existingConfig?.RedirectType?.ToString() switch
        {
            "Permanent" => 0,
            "Found" => 1,
            "SeeOther" => 2,
            "Temporary" => 3,
            _ => 0,
        };
        _redirectSection.Children.Add(_redirectTypeCombo);
        ScrollDropDownToTop(_redirectTypeCombo);

        // Target: Listener or External URL
        var hasTargetListener = existingConfig?.TargetListenerId is not null;
        var hasTargetUrl = !string.IsNullOrEmpty(existingConfig?.TargetUri?.ToString());
        var isListenerTarget = hasTargetListener || !hasTargetUrl;

        AddLabelTo(_redirectSection, "Redirect Target");
        var targetPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        _redirectTargetListener = new RadioButton { Content = "Listener", GroupName = "RedirectTarget", IsChecked = isListenerTarget };
        _redirectTargetExternal = new RadioButton { Content = "External URL", GroupName = "RedirectTarget", IsChecked = !isListenerTarget };
        _redirectTargetListener.Checked += (_, _) =>
        {
            if (_redirectListenerSection is not null) _redirectListenerSection.Visibility = Visibility.Visible;
            if (_redirectUrlSection is not null) _redirectUrlSection.Visibility = Visibility.Collapsed;
        };
        _redirectTargetExternal.Checked += (_, _) =>
        {
            if (_redirectListenerSection is not null) _redirectListenerSection.Visibility = Visibility.Collapsed;
            if (_redirectUrlSection is not null) _redirectUrlSection.Visibility = Visibility.Visible;
        };
        targetPanel.Children.Add(_redirectTargetListener);
        targetPanel.Children.Add(_redirectTargetExternal);
        _redirectSection.Children.Add(targetPanel);

        // Listener target
        _redirectListenerSection = new StackPanel { Spacing = 4, Visibility = isListenerTarget ? Visibility.Visible : Visibility.Collapsed };
        AddLabelTo(_redirectListenerSection, "Target Listener");
        _redirectListenerCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "Select target listener..." };
        var currentTargetListenerName = SubResourceName(existingConfig?.TargetListenerId);
        var listenerSelectedIndex = -1;
        for (int i = 0; i < ViewModel.Data.HttpListeners.Count; i++)
        {
            var name = ViewModel.Data.HttpListeners[i].Name ?? "";
            _redirectListenerCombo.Items.Add(name);
            if (currentTargetListenerName is not null && name.Equals(currentTargetListenerName, StringComparison.OrdinalIgnoreCase))
                listenerSelectedIndex = i;
        }

        _redirectListenerCombo.SelectedIndex = listenerSelectedIndex;
        ScrollDropDownToTop(_redirectListenerCombo);
        _redirectListenerSection.Children.Add(_redirectListenerCombo);
        _redirectSection.Children.Add(_redirectListenerSection);

        // External URL target
        _redirectUrlSection = new StackPanel { Spacing = 4, Visibility = isListenerTarget ? Visibility.Collapsed : Visibility.Visible };
        AddLabelTo(_redirectUrlSection, "Target URL");
        _redirectUrlBox = new TextBox
        {
            Text = existingConfig?.TargetUri?.ToString() ?? "",
            PlaceholderText = "https://www.contoso.com",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _redirectUrlSection.Children.Add(_redirectUrlBox);
        _redirectSection.Children.Add(_redirectUrlSection);

        // Include path & query string
        _redirectIncludePath = new CheckBox
        {
            Content = "Include path in redirect",
            IsChecked = existingConfig?.IncludePath ?? true,
            Margin = new Thickness(0, 8, 0, 0),
        };
        _redirectSection.Children.Add(_redirectIncludePath);

        _redirectIncludeQueryString = new CheckBox
        {
            Content = "Include query string in redirect",
            IsChecked = existingConfig?.IncludeQueryString ?? true,
        };
        _redirectSection.Children.Add(_redirectIncludeQueryString);
    }

    private void BuildPathRulesSection(ApplicationGatewayRequestRoutingRule rule)
    {
        PathRulesList.Items.Clear();
        PathMapDefaultText.Visibility = Visibility.Collapsed;

        var pathMap = FindOrGetPathMap(rule);
        if (pathMap is null)
        {
            PathRulesList.Items.Add(new ListViewItem
            {
                Content = new TextBlock
                {
                    Text = "No URL path map associated. Add a path rule to create one.",
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                },
                IsEnabled = false,
            });
            return;
        }

        // Default backend (shown as info)
        if (pathMap.DefaultBackendAddressPoolId is not null || pathMap.DefaultRedirectConfigurationId is not null)
        {
            var defaultText = pathMap.DefaultBackendAddressPoolId is not null
                ? $"Default backend: {SubResourceName(pathMap.DefaultBackendAddressPoolId)} / {SubResourceName(pathMap.DefaultBackendHttpSettingsId) ?? "(none)"}"
                : $"Default redirect: {SubResourceName(pathMap.DefaultRedirectConfigurationId) ?? "(none)"}";
            PathMapDefaultText.Text = defaultText;
            PathMapDefaultText.Visibility = Visibility.Visible;
        }

        foreach (var pathRule in pathMap.PathRules)
        {
            var pathRuleName = pathRule.Name ?? "";
            var paths = string.Join(", ", pathRule.Paths);

            // Determine target display text with actual config details
            string target;
            if (pathRule.RedirectConfigurationId is not null)
            {
                var rdName = SubResourceName(pathRule.RedirectConfigurationId);
                var rdConfig = rdName is not null && ViewModel.Data is not null
                    ? ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name?.Equals(rdName, StringComparison.OrdinalIgnoreCase) == true)
                    : null;
                if (rdConfig is not null)
                {
                    var rdType = rdConfig.RedirectType?.ToString() ?? "Permanent";
                    var rdTarget = rdConfig.TargetListenerId is not null
                        ? $"Listener: {SubResourceName(rdConfig.TargetListenerId)}"
                        : rdConfig.TargetUri is not null
                            ? $"URL: {rdConfig.TargetUri}"
                            : "(no target)";
                    target = $"Redirect ({rdType}) \u2192 {rdTarget}";
                }
                else
                {
                    target = $"Redirect: {rdName}";
                }
            }
            else if (pathRule.BackendAddressPoolId is not null)
            {
                var settings = SubResourceName(pathRule.BackendHttpSettingsId);
                target = $"Pool: {SubResourceName(pathRule.BackendAddressPoolId)}" +
                    (settings is not null ? $" / Settings: {settings}" : "");
            }
            else
            {
                target = "(no target)";
            }

            var card = new Border
            {
                Padding = new Thickness(12, 8, 12, 8),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
            };

            var cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoStack = new StackPanel { Spacing = 2 };
            infoStack.Children.Add(new TextBlock { Text = pathRuleName, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Paths: {paths}",
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = target,
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            cardGrid.Children.Add(infoStack);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            var editBtn = new Button
            {
                Width = 32, Height = 32, Padding = new Thickness(0),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Tag = pathRuleName,
            };
            editBtn.Content = new FontIcon { Glyph = "\uE70F", FontSize = 14 };
            ToolTipService.SetToolTip(editBtn, "Edit");
            editBtn.Click += EditPathRule_Click;

            var deleteBtn = new Button
            {
                Width = 32, Height = 32, Padding = new Thickness(0),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 },
                Tag = pathRuleName,
            };
            ToolTipService.SetToolTip(deleteBtn, "Delete");
            deleteBtn.Click += DeletePathRule_Click;

            actions.Children.Add(editBtn);
            actions.Children.Add(deleteBtn);
            Grid.SetColumn(actions, 1);
            cardGrid.Children.Add(actions);

            card.Child = cardGrid;
            PathRulesList.Items.Add(new ListViewItem
            {
                Content = card,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0, 2, 0, 2),
            });
        }

        if (pathMap.PathRules.Count == 0)
        {
            PathRulesList.Items.Add(new ListViewItem
            {
                Content = new TextBlock
                {
                    Text = "No path rules defined. Add a path rule to configure URL-based routing.",
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                },
                IsEnabled = false,
            });
        }
    }

    private ApplicationGatewayUrlPathMap? FindOrGetPathMap(ApplicationGatewayRequestRoutingRule rule)
    {
        if (ViewModel.Data is null) return null;
        var mapName = SubResourceName(rule.UrlPathMapId);
        if (mapName is not null)
        {
            return ViewModel.Data.UrlPathMaps.FirstOrDefault(m => m.Name == mapName);
        }

        return null;
    }

    private void RuleType_Changed(object sender, RoutedEventArgs e)
    {
        var isPathBased = _ruleTypePathBased?.IsChecked == true;
        PathRulesPanel.Visibility = isPathBased ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TargetType_Changed(object sender, RoutedEventArgs e)
    {
        var isRedirect = _targetTypeRedirect?.IsChecked == true;
        if (_backendSection is not null)
            _backendSection.Visibility = isRedirect ? Visibility.Collapsed : Visibility.Visible;
        if (_redirectSection is not null)
            _redirectSection.Visibility = isRedirect ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AddPathRule_Click(object sender, RoutedEventArgs e)
    {
        var result = await ShowPathRuleDialogAsync("Add Path Rule", null);
        if (result is not null)
        {
            var rule = ViewModel.Data?.RequestRoutingRules.FirstOrDefault(r => r.Name == _ruleName);
            if (rule is null || ViewModel.Data is null) return;

            var pathMap = EnsureUrlPathMap(rule);
            var pathRule = new ApplicationGatewayPathRule { Name = result.Name };
            foreach (var p in result.Paths)
                pathRule.Paths.Add(p);

            if (result.UseRedirect)
            {
                var redirectConfig = CreateOrUpdateRedirectConfig(result.Name, result);
                pathRule.RedirectConfigurationId = redirectConfig.Id;
            }
            else
            {
                if (!string.IsNullOrEmpty(result.BackendPoolName))
                {
                    var bp = ViewModel.Data.BackendAddressPools.FirstOrDefault(p => p.Name == result.BackendPoolName);
                    if (bp is not null) pathRule.BackendAddressPoolId = bp.Id;
                }

                if (!string.IsNullOrEmpty(result.BackendSettingsName))
                {
                    var bs = ViewModel.Data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == result.BackendSettingsName);
                    if (bs is not null) pathRule.BackendHttpSettingsId = bs.Id;
                }
            }

            pathMap.PathRules.Add(pathRule);
            RenderForm();
        }
    }

    private async void EditPathRule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string pathRuleName }) return;
        var rule = ViewModel.Data?.RequestRoutingRules.FirstOrDefault(r => r.Name == _ruleName);
        if (rule is null || ViewModel.Data is null) return;

        var pathMap = FindOrGetPathMap(rule);
        var pathRule = pathMap?.PathRules.FirstOrDefault(p => p.Name == pathRuleName);
        if (pathRule is null) return;

        var result = await ShowPathRuleDialogAsync($"Edit: {pathRuleName}", pathRule);
        if (result is not null)
        {
            pathRule.Paths.Clear();
            foreach (var p in result.Paths)
                pathRule.Paths.Add(p);

            if (result.UseRedirect)
            {
                var redirectConfig = CreateOrUpdateRedirectConfig(pathRuleName, result);
                pathRule.RedirectConfigurationId = redirectConfig.Id;
                pathRule.BackendAddressPoolId = null;
                pathRule.BackendHttpSettingsId = null;
            }
            else
            {
                // Clean up old redirect config if switching away
                RemovePathRuleRedirectConfig(pathRuleName, pathRule);
                pathRule.RedirectConfigurationId = null;

                if (!string.IsNullOrEmpty(result.BackendPoolName))
                {
                    var bp = ViewModel.Data.BackendAddressPools.FirstOrDefault(p => p.Name == result.BackendPoolName);
                    pathRule.BackendAddressPoolId = bp?.Id;
                }
                else
                {
                    pathRule.BackendAddressPoolId = null;
                }

                if (!string.IsNullOrEmpty(result.BackendSettingsName))
                {
                    var bs = ViewModel.Data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == result.BackendSettingsName);
                    pathRule.BackendHttpSettingsId = bs?.Id;
                }
                else
                {
                    pathRule.BackendHttpSettingsId = null;
                }
            }

            RenderForm();
        }
    }

    private void DeletePathRule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string pathRuleName }) return;
        var rule = ViewModel.Data?.RequestRoutingRules.FirstOrDefault(r => r.Name == _ruleName);
        if (rule is null) return;

        var pathMap = FindOrGetPathMap(rule);
        var pathRule = pathMap?.PathRules.FirstOrDefault(p => p.Name == pathRuleName);
        if (pathRule is not null)
        {
            RemovePathRuleRedirectConfig(pathRuleName, pathRule);
            pathMap!.PathRules.Remove(pathRule);
            RenderForm();
        }
    }

    private ApplicationGatewayRedirectConfiguration CreateOrUpdateRedirectConfig(string pathRuleName, PathRuleDialogResult result)
    {
        if (ViewModel.Data is null) throw new InvalidOperationException("No gateway data");

        var configName = $"{_ruleName}-{pathRuleName}-redirect";
        var config = ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name == configName);
        if (config is null)
        {
            config = new ApplicationGatewayRedirectConfiguration { Name = configName };
            ViewModel.Data.RedirectConfigurations.Add(config);
        }

        config.RedirectType = result.RedirectType ?? ApplicationGatewayRedirectType.Permanent;

        if (result.RedirectToListener)
        {
            if (!string.IsNullOrEmpty(result.RedirectTargetListenerName))
            {
                var tl = ViewModel.Data.HttpListeners.FirstOrDefault(l => l.Name == result.RedirectTargetListenerName);
                if (tl is not null) config.TargetListenerId = tl.Id;
            }

            config.TargetUri = null;
        }
        else
        {
            config.TargetListenerId = null;
            if (!string.IsNullOrEmpty(result.RedirectTargetUrl))
                config.TargetUri = new Uri(result.RedirectTargetUrl);
        }

        config.IncludePath = result.RedirectIncludePath;
        config.IncludeQueryString = result.RedirectIncludeQueryString;

        return config;
    }

    private void RemovePathRuleRedirectConfig(string pathRuleName, ApplicationGatewayPathRule pathRule)
    {
        if (ViewModel.Data is null) return;
        var oldRedirectName = SubResourceName(pathRule.RedirectConfigurationId);
        if (oldRedirectName is not null)
        {
            var oldConfig = ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name == oldRedirectName);
            if (oldConfig is not null)
                ViewModel.Data.RedirectConfigurations.Remove(oldConfig);
        }
    }

    private sealed record PathRuleDialogResult(
        string Name,
        List<string> Paths,
        string? BackendPoolName,
        string? BackendSettingsName,
        bool UseRedirect,
        ApplicationGatewayRedirectType? RedirectType,
        bool RedirectToListener,
        string? RedirectTargetListenerName,
        string? RedirectTargetUrl,
        bool RedirectIncludePath,
        bool RedirectIncludeQueryString);

    private async Task<PathRuleDialogResult?> ShowPathRuleDialogAsync(string title, ApplicationGatewayPathRule? existing)
    {
        if (ViewModel.Data is null) return null;

        var stack = new StackPanel { Spacing = 12, MinWidth = 400 };

        // Name (disabled on edit)
        AddLabelTo(stack, "Name");
        var nameBox = new TextBox
        {
            Text = existing?.Name ?? "",
            PlaceholderText = "path-rule-name",
            IsEnabled = existing is null,
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        stack.Children.Add(nameBox);

        // Paths
        AddLabelTo(stack, "Paths (comma-separated)");
        var pathsBox = new TextBox
        {
            Text = existing is not null ? string.Join(", ", existing.Paths) : "",
            PlaceholderText = "/api/*, /images/*",
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        stack.Children.Add(pathsBox);

        // Resolve existing redirect config if any
        var existingRedirectName = SubResourceName(existing?.RedirectConfigurationId);
        var existingRedirectConfig = existingRedirectName is not null
            ? ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name?.Equals(existingRedirectName, StringComparison.OrdinalIgnoreCase) == true)
            : null;
        var hasRedirect = existingRedirectConfig is not null;

        // Target type radio
        AddLabelTo(stack, "Target Type");
        var targetPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        var targetBackendRadio = new RadioButton { Content = "Backend Pool", GroupName = "PathTarget", IsChecked = !hasRedirect };
        var targetRedirectRadio = new RadioButton { Content = "Redirection", GroupName = "PathTarget", IsChecked = hasRedirect };
        targetPanel.Children.Add(targetBackendRadio);
        targetPanel.Children.Add(targetRedirectRadio);
        stack.Children.Add(targetPanel);

        // Backend target fields
        var backendFields = new StackPanel { Spacing = 8, Visibility = hasRedirect ? Visibility.Collapsed : Visibility.Visible };
        AddLabelTo(backendFields, "Backend Pool");
        var poolCombo = new ComboBox { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Select a backend pool..." };
        var poolIdx = -1;
        var existingPoolName = SubResourceName(existing?.BackendAddressPoolId);
        for (int i = 0; i < ViewModel.Data.BackendAddressPools.Count; i++)
        {
            var name = ViewModel.Data.BackendAddressPools[i].Name ?? "";
            poolCombo.Items.Add(name);
            if (existingPoolName is not null && name.Equals(existingPoolName, StringComparison.OrdinalIgnoreCase))
                poolIdx = i;
        }

        poolCombo.SelectedIndex = poolIdx;
        ScrollDropDownToTop(poolCombo);
        backendFields.Children.Add(poolCombo);

        AddLabelTo(backendFields, "Backend Settings");
        var settingsCombo = new ComboBox { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Select backend settings..." };
        var settingsIdx = -1;
        var existingSettingsName = SubResourceName(existing?.BackendHttpSettingsId);
        for (int i = 0; i < ViewModel.Data.BackendHttpSettingsCollection.Count; i++)
        {
            var name = ViewModel.Data.BackendHttpSettingsCollection[i].Name ?? "";
            settingsCombo.Items.Add(name);
            if (existingSettingsName is not null && name.Equals(existingSettingsName, StringComparison.OrdinalIgnoreCase))
                settingsIdx = i;
        }

        settingsCombo.SelectedIndex = settingsIdx;
        ScrollDropDownToTop(settingsCombo);
        backendFields.Children.Add(settingsCombo);
        stack.Children.Add(backendFields);

        // Redirect inline fields
        var redirectFields = new StackPanel { Spacing = 8, Visibility = hasRedirect ? Visibility.Visible : Visibility.Collapsed };

        AddLabelTo(redirectFields, "Redirect Type");
        var redirectTypeCombo = new ComboBox { Width = 400, HorizontalAlignment = HorizontalAlignment.Left };
        redirectTypeCombo.Items.Add("Permanent (301)");
        redirectTypeCombo.Items.Add("Found (302)");
        redirectTypeCombo.Items.Add("SeeOther (303)");
        redirectTypeCombo.Items.Add("Temporary (307)");
        redirectTypeCombo.SelectedIndex = existingRedirectConfig?.RedirectType?.ToString() switch
        {
            "Permanent" => 0,
            "Found" => 1,
            "SeeOther" => 2,
            "Temporary" => 3,
            _ => 0,
        };
        redirectFields.Children.Add(redirectTypeCombo);
        ScrollDropDownToTop(redirectTypeCombo);

        var hasTargetListener = existingRedirectConfig?.TargetListenerId is not null;
        var hasTargetUrl = !string.IsNullOrEmpty(existingRedirectConfig?.TargetUri?.ToString());
        var isListenerTarget = hasTargetListener || !hasTargetUrl;

        AddLabelTo(redirectFields, "Redirect Target");
        var rdTargetPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        var rdListenerRadio = new RadioButton { Content = "Listener", GroupName = "PathRdTarget", IsChecked = isListenerTarget };
        var rdExternalRadio = new RadioButton { Content = "External URL", GroupName = "PathRdTarget", IsChecked = !isListenerTarget };
        rdTargetPanel.Children.Add(rdListenerRadio);
        rdTargetPanel.Children.Add(rdExternalRadio);
        redirectFields.Children.Add(rdTargetPanel);

        var rdListenerSection = new StackPanel { Spacing = 4, Visibility = isListenerTarget ? Visibility.Visible : Visibility.Collapsed };
        AddLabelTo(rdListenerSection, "Target Listener");
        var rdListenerCombo = new ComboBox { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Select target listener..." };
        var currentTargetListenerName = SubResourceName(existingRedirectConfig?.TargetListenerId);
        var rdListenerIdx = -1;
        for (int i = 0; i < ViewModel.Data.HttpListeners.Count; i++)
        {
            var name = ViewModel.Data.HttpListeners[i].Name ?? "";
            rdListenerCombo.Items.Add(name);
            if (currentTargetListenerName is not null && name.Equals(currentTargetListenerName, StringComparison.OrdinalIgnoreCase))
                rdListenerIdx = i;
        }

        rdListenerCombo.SelectedIndex = rdListenerIdx;
        ScrollDropDownToTop(rdListenerCombo);
        rdListenerSection.Children.Add(rdListenerCombo);
        redirectFields.Children.Add(rdListenerSection);

        var rdUrlSection = new StackPanel { Spacing = 4, Visibility = isListenerTarget ? Visibility.Collapsed : Visibility.Visible };
        AddLabelTo(rdUrlSection, "Target URL");
        var rdUrlBox = new TextBox
        {
            Text = existingRedirectConfig?.TargetUri?.ToString() ?? "",
            PlaceholderText = "https://www.contoso.com",
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        rdUrlSection.Children.Add(rdUrlBox);
        redirectFields.Children.Add(rdUrlSection);

        rdListenerRadio.Checked += (_, _) => { rdListenerSection.Visibility = Visibility.Visible; rdUrlSection.Visibility = Visibility.Collapsed; };
        rdExternalRadio.Checked += (_, _) => { rdListenerSection.Visibility = Visibility.Collapsed; rdUrlSection.Visibility = Visibility.Visible; };

        var rdIncludePath = new CheckBox
        {
            Content = "Include path in redirect",
            IsChecked = existingRedirectConfig?.IncludePath ?? true,
            Margin = new Thickness(0, 4, 0, 0),
        };
        redirectFields.Children.Add(rdIncludePath);

        var rdIncludeQs = new CheckBox
        {
            Content = "Include query string in redirect",
            IsChecked = existingRedirectConfig?.IncludeQueryString ?? true,
        };
        redirectFields.Children.Add(rdIncludeQs);

        stack.Children.Add(redirectFields);

        targetBackendRadio.Checked += (_, _) => { backendFields.Visibility = Visibility.Visible; redirectFields.Visibility = Visibility.Collapsed; };
        targetRedirectRadio.Checked += (_, _) => { backendFields.Visibility = Visibility.Collapsed; redirectFields.Visibility = Visibility.Visible; };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stack,
            PrimaryButtonText = existing is not null ? "Save" : "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var dialogResult = await dialog.ShowAsync();
        if (dialogResult != ContentDialogResult.Primary) return null;

        var ruleName = nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ruleName)) return null;

        var paths = pathsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

        var useRedirect = targetRedirectRadio.IsChecked == true;
        var poolName = poolCombo.SelectedIndex >= 0 ? poolCombo.SelectedItem?.ToString() : null;
        var settingsName = settingsCombo.SelectedIndex >= 0 ? settingsCombo.SelectedItem?.ToString() : null;

        ApplicationGatewayRedirectType? rdType = redirectTypeCombo.SelectedIndex switch
        {
            0 => ApplicationGatewayRedirectType.Permanent,
            1 => ApplicationGatewayRedirectType.Found,
            2 => ApplicationGatewayRedirectType.SeeOther,
            3 => ApplicationGatewayRedirectType.Temporary,
            _ => ApplicationGatewayRedirectType.Permanent,
        };

        return new PathRuleDialogResult(
            ruleName, paths, poolName, settingsName, useRedirect,
            rdType,
            rdListenerRadio.IsChecked == true,
            rdListenerCombo.SelectedItem?.ToString(),
            rdUrlBox.Text.Trim(),
            rdIncludePath.IsChecked ?? true,
            rdIncludeQs.IsChecked ?? true);
    }

    private ApplicationGatewayUrlPathMap EnsureUrlPathMap(ApplicationGatewayRequestRoutingRule rule)
    {
        if (ViewModel.Data is null) throw new InvalidOperationException("No gateway data");

        var existingMap = FindOrGetPathMap(rule);
        if (existingMap is not null) return existingMap;

        var mapName = $"{_ruleName}-pathmap";
        var newMap = new ApplicationGatewayUrlPathMap { Name = mapName };

        if (rule.BackendAddressPoolId is not null)
            newMap.DefaultBackendAddressPoolId = rule.BackendAddressPoolId;
        if (rule.BackendHttpSettingsId is not null)
            newMap.DefaultBackendHttpSettingsId = rule.BackendHttpSettingsId;

        ViewModel.Data.UrlPathMaps.Add(newMap);
        rule.UrlPathMapId = newMap.Id;

        return newMap;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var rule = ViewModel.Data?.RequestRoutingRules.FirstOrDefault(r => r.Name == _ruleName);
        if (rule is null || ViewModel.Data is null) return;

        var isPathBased = _ruleTypePathBased?.IsChecked == true;
        rule.RuleType = isPathBased
            ? ApplicationGatewayRequestRoutingRuleType.PathBasedRouting
            : ApplicationGatewayRequestRoutingRuleType.Basic;

        if (int.TryParse(_priorityBox?.Text, out var priority))
            rule.Priority = priority;

        var listenerName = _listenerCombo?.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(listenerName))
        {
            var hl = ViewModel.Data.HttpListeners.FirstOrDefault(l => l.Name == listenerName);
            if (hl is not null) rule.HttpListenerId = hl.Id;
        }

        var isRedirect = _targetTypeRedirect?.IsChecked == true;
        if (isRedirect)
        {
            rule.BackendAddressPoolId = null;
            rule.BackendHttpSettingsId = null;

            // Create or update the redirect configuration
            var redirectConfigName = $"{_ruleName}-redirect";
            var redirectConfig = ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name == redirectConfigName);
            if (redirectConfig is null)
            {
                redirectConfig = new ApplicationGatewayRedirectConfiguration { Name = redirectConfigName };
                ViewModel.Data.RedirectConfigurations.Add(redirectConfig);
            }

            redirectConfig.RedirectType = _redirectTypeCombo?.SelectedIndex switch
            {
                0 => ApplicationGatewayRedirectType.Permanent,
                1 => ApplicationGatewayRedirectType.Found,
                2 => ApplicationGatewayRedirectType.SeeOther,
                3 => ApplicationGatewayRedirectType.Temporary,
                _ => ApplicationGatewayRedirectType.Permanent,
            };

            if (_redirectTargetListener?.IsChecked == true)
            {
                var targetListenerName = _redirectListenerCombo?.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(targetListenerName))
                {
                    var tl = ViewModel.Data.HttpListeners.FirstOrDefault(l => l.Name == targetListenerName);
                    if (tl is not null) redirectConfig.TargetListenerId = tl.Id;
                }

                redirectConfig.TargetUri = null;
            }
            else
            {
                redirectConfig.TargetListenerId = null;
                var targetUrl = _redirectUrlBox?.Text?.Trim();
                if (!string.IsNullOrEmpty(targetUrl))
                {
                    redirectConfig.TargetUri = new Uri(targetUrl);
                }
            }

            redirectConfig.IncludePath = _redirectIncludePath?.IsChecked ?? true;
            redirectConfig.IncludeQueryString = _redirectIncludeQueryString?.IsChecked ?? true;

            rule.RedirectConfigurationId = redirectConfig.Id;
        }
        else
        {
            // Clear redirect: remove the redirect config if it was auto-created
            var currentRedirectName = SubResourceName(rule.RedirectConfigurationId);
            if (currentRedirectName is not null)
            {
                var oldConfig = ViewModel.Data.RedirectConfigurations.FirstOrDefault(r => r.Name == currentRedirectName);
                if (oldConfig is not null)
                    ViewModel.Data.RedirectConfigurations.Remove(oldConfig);
            }

            rule.RedirectConfigurationId = null;

            var poolName = _backendPoolCombo?.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(poolName))
            {
                var bp = ViewModel.Data.BackendAddressPools.FirstOrDefault(p => p.Name == poolName);
                if (bp is not null) rule.BackendAddressPoolId = bp.Id;
            }
            else
            {
                rule.BackendAddressPoolId = null;
            }

            var settingsName = _backendSettingsCombo?.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(settingsName))
            {
                var bs = ViewModel.Data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == settingsName);
                if (bs is not null) rule.BackendHttpSettingsId = bs.Id;
            }
            else
            {
                rule.BackendHttpSettingsId = null;
            }
        }

        if (!isPathBased)
        {
            var pathMap = FindOrGetPathMap(rule);
            if (pathMap is not null)
            {
                ViewModel.Data.UrlPathMaps.Remove(pathMap);
            }

            rule.UrlPathMapId = null;
        }

        await ViewModel.SaveChangesAsync($"Updated rule '{_ruleName}'.");

        RenderForm();
    }

    private void AddLabel(string text)
    {
        AddLabelTo(FormArea, text);
    }

    private static void AddLabelTo(Panel parent, string text)
    {
        parent.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 4, 0, 0),
        });
    }
}
