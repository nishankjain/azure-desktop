using System.Collections.ObjectModel;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public enum AppGwSection
{
    Overview,
    BackendPools,
    BackendSettings,
    FrontendIP,
    PrivateLink,
    SslSettings,
    Listeners,
    RoutingRules,
    RewriteSets,
    HealthProbes,
    Configuration,
    Waf,
    JwtValidation,
}

public partial class AppGwViewModel(IAzureAuthService authService, IApplicationGatewayService gatewayService) : ObservableObject
{
    private ApplicationGatewayData? _data;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? SaveMessage { get; set; }

    [ObservableProperty]
    public partial AppGwSection CurrentSection { get; set; }

    // Overview
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Location { get; set; } = "";

    [ObservableProperty]
    public partial string ResourceId { get; set; } = "";

    [ObservableProperty]
    public partial string SkuName { get; set; } = "";

    [ObservableProperty]
    public partial string SkuTier { get; set; } = "";

    [ObservableProperty]
    public partial int SkuCapacity { get; set; }

    [ObservableProperty]
    public partial string OperationalState { get; set; } = "";

    [ObservableProperty]
    public partial string ProvisioningState { get; set; } = "";

    // Configuration
    [ObservableProperty]
    public partial bool EnableHttp2 { get; set; }

    [ObservableProperty]
    public partial bool EnableFips { get; set; }

    [ObservableProperty]
    public partial string AutoscaleMinCapacity { get; set; } = "";

    [ObservableProperty]
    public partial string AutoscaleMaxCapacity { get; set; } = "";

    // WAF
    [ObservableProperty]
    public partial string WafPolicyId { get; set; } = "";

    [ObservableProperty]
    public partial bool WafEnabled { get; set; }

    [ObservableProperty]
    public partial string WafMode { get; set; } = "";

    [ObservableProperty]
    public partial string WafRuleSetType { get; set; } = "";

    [ObservableProperty]
    public partial string WafRuleSetVersion { get; set; } = "";

    // Collections for each section
    public ObservableCollection<Dictionary<string, string>> BackendPools { get; } = [];
    public ObservableCollection<Dictionary<string, string>> BackendSettings { get; } = [];
    public ObservableCollection<Dictionary<string, string>> FrontendIPs { get; } = [];
    public ObservableCollection<Dictionary<string, string>> FrontendPorts { get; } = [];
    public ObservableCollection<Dictionary<string, string>> PrivateLinks { get; } = [];
    public ObservableCollection<Dictionary<string, string>> SslCertificates { get; } = [];
    public ObservableCollection<Dictionary<string, string>> SslProfiles { get; } = [];
    public ObservableCollection<Dictionary<string, string>> HttpListeners { get; } = [];
    public ObservableCollection<Dictionary<string, string>> RoutingRules { get; } = [];
    public ObservableCollection<Dictionary<string, string>> RewriteSets { get; } = [];
    public ObservableCollection<Dictionary<string, string>> HealthProbes { get; } = [];
    public ObservableCollection<Dictionary<string, string>> JwtConfigs { get; } = [];

    public async Task LoadAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        ResourceId = resourceId;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var client = new ArmClient(authService.Credential);
            var gwResource = client.GetApplicationGatewayResource(new Azure.Core.ResourceIdentifier(resourceId));
            var gw = await gwResource.GetAsync(cancellationToken);
            _data = gw.Value.Data;

            PopulateOverview();
            PopulateConfiguration();
            PopulateWaf();
            PopulateBackendPools();
            PopulateBackendSettings();
            PopulateFrontendIPs();
            PopulatePrivateLinks();
            PopulateSslSettings();
            PopulateListeners();
            PopulateRoutingRules();
            PopulateRewriteSets();
            PopulateHealthProbes();
            PopulateJwtConfigs();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulateOverview()
    {
        if (_data is null) return;
        Name = _data.Name ?? "";
        Location = _data.Location?.ToString() ?? "";
        SkuName = _data.Sku?.Name?.ToString() ?? "";
        SkuTier = _data.Sku?.Tier?.ToString() ?? "";
        SkuCapacity = _data.Sku?.Capacity ?? 0;
        OperationalState = _data.OperationalState?.ToString() ?? "";
        ProvisioningState = _data.ProvisioningState?.ToString() ?? "";
    }

    private void PopulateConfiguration()
    {
        if (_data is null) return;
        EnableHttp2 = _data.EnableHttp2 ?? false;
        EnableFips = _data.EnableFips ?? false;
        var asc = _data.AutoscaleConfiguration;
        AutoscaleMinCapacity = asc?.MinCapacity.ToString() ?? "N/A";
        AutoscaleMaxCapacity = asc?.MaxCapacity?.ToString() ?? "N/A";
    }

    private void PopulateWaf()
    {
        if (_data is null) return;
        WafPolicyId = _data.FirewallPolicyId?.ToString() ?? "";
        var waf = _data.WebApplicationFirewallConfiguration;
        if (waf is not null)
        {
            WafEnabled = waf.Enabled;
            WafMode = waf.FirewallMode.ToString();
            WafRuleSetType = waf.RuleSetType ?? "";
            WafRuleSetVersion = waf.RuleSetVersion ?? "";
        }
    }

    private void PopulateBackendPools()
    {
        BackendPools.Clear();
        if (_data is null) return;
        foreach (var pool in _data.BackendAddressPools)
        {
            var d = new Dictionary<string, string>
            {
                ["Name"] = pool.Name ?? "",
                ["Addresses"] = string.Join(", ", pool.BackendAddresses?.Select(a => a.Fqdn ?? a.IPAddress ?? "") ?? []),
                ["Address Count"] = (pool.BackendAddresses?.Count ?? 0).ToString(),
            };
            BackendPools.Add(d);
        }
    }

    private void PopulateBackendSettings()
    {
        BackendSettings.Clear();
        if (_data is null) return;
        foreach (var s in _data.BackendHttpSettingsCollection)
        {
            BackendSettings.Add(new Dictionary<string, string>
            {
                ["Name"] = s.Name ?? "",
                ["Protocol"] = s.Protocol?.ToString() ?? "",
                ["Port"] = s.Port?.ToString() ?? "",
                ["Cookie Affinity"] = s.CookieBasedAffinity?.ToString() ?? "",
                ["Request Timeout"] = $"{s.RequestTimeoutInSeconds ?? 0}s",
                ["Host Name"] = s.HostName ?? "",
            });
        }
    }

    private void PopulateFrontendIPs()
    {
        FrontendIPs.Clear();
        FrontendPorts.Clear();
        if (_data is null) return;
        foreach (var fip in _data.FrontendIPConfigurations)
        {
            FrontendIPs.Add(new Dictionary<string, string>
            {
                ["Name"] = fip.Name ?? "",
                ["Private IP"] = fip.PrivateIPAddress ?? "",
                ["Allocation Method"] = fip.PrivateIPAllocationMethod?.ToString() ?? "",
                ["Public IP"] = fip.PublicIPAddressId?.ToString() ?? "",
            });
        }

        foreach (var port in _data.FrontendPorts)
        {
            FrontendPorts.Add(new Dictionary<string, string>
            {
                ["Name"] = port.Name ?? "",
                ["Port"] = port.Port?.ToString() ?? "",
            });
        }
    }

    private void PopulatePrivateLinks()
    {
        PrivateLinks.Clear();
        if (_data is null) return;
        foreach (var pl in _data.PrivateLinkConfigurations)
        {
            PrivateLinks.Add(new Dictionary<string, string>
            {
                ["Name"] = pl.Name ?? "",
                ["IP Configurations"] = (pl.IPConfigurations?.Count ?? 0).ToString(),
            });
        }
    }

    private void PopulateSslSettings()
    {
        SslCertificates.Clear();
        SslProfiles.Clear();
        if (_data is null) return;
        foreach (var cert in _data.SslCertificates)
        {
            SslCertificates.Add(new Dictionary<string, string>
            {
                ["Name"] = cert.Name ?? "",
            });
        }

        foreach (var profile in _data.SslProfiles)
        {
            SslProfiles.Add(new Dictionary<string, string>
            {
                ["Name"] = profile.Name ?? "",
                ["Client Auth"] = (profile.ClientAuthConfiguration?.VerifyClientCertIssuerDN ?? false).ToString(),
            });
        }

        // SSL Policy
        var policy = _data.SslPolicy;
        if (policy is not null)
        {
            SslCertificates.Insert(0, new Dictionary<string, string>
            {
                ["Name"] = "SSL Policy",
                ["Policy Type"] = policy.PolicyType?.ToString() ?? "",
                ["Min Protocol"] = policy.MinProtocolVersion?.ToString() ?? "",
            });
        }
    }

    private void PopulateListeners()
    {
        HttpListeners.Clear();
        if (_data is null) return;
        foreach (var l in _data.HttpListeners)
        {
            HttpListeners.Add(new Dictionary<string, string>
            {
                ["Name"] = l.Name ?? "",
                ["Protocol"] = l.Protocol?.ToString() ?? "",
                ["Host Name"] = l.HostName ?? "",
                ["Require SNI"] = (l.RequireServerNameIndication ?? false).ToString(),
            });
        }
    }

    private void PopulateRoutingRules()
    {
        RoutingRules.Clear();
        if (_data is null) return;
        foreach (var r in _data.RequestRoutingRules)
        {
            RoutingRules.Add(new Dictionary<string, string>
            {
                ["Name"] = r.Name ?? "",
                ["Rule Type"] = r.RuleType?.ToString() ?? "",
                ["Priority"] = r.Priority?.ToString() ?? "",
                ["Listener"] = r.HttpListenerId?.Name ?? "",
                ["Backend Pool"] = r.BackendAddressPoolId?.Name ?? "",
                ["Backend Settings"] = r.BackendHttpSettingsId?.Name ?? "",
            });
        }
    }

    private void PopulateRewriteSets()
    {
        RewriteSets.Clear();
        if (_data is null) return;
        foreach (var rs in _data.RewriteRuleSets)
        {
            RewriteSets.Add(new Dictionary<string, string>
            {
                ["Name"] = rs.Name ?? "",
                ["Rule Count"] = (rs.RewriteRules?.Count ?? 0).ToString(),
            });
        }
    }

    private void PopulateHealthProbes()
    {
        HealthProbes.Clear();
        if (_data is null) return;
        foreach (var p in _data.Probes)
        {
            HealthProbes.Add(new Dictionary<string, string>
            {
                ["Name"] = p.Name ?? "",
                ["Protocol"] = p.Protocol?.ToString() ?? "",
                ["Host"] = p.Host ?? "",
                ["Path"] = p.Path ?? "",
                ["Interval"] = $"{p.IntervalInSeconds ?? 0}s",
                ["Timeout"] = $"{p.TimeoutInSeconds ?? 0}s",
                ["Unhealthy Threshold"] = (p.UnhealthyThreshold ?? 0).ToString(),
            });
        }
    }

    private void PopulateJwtConfigs()
    {
        JwtConfigs.Clear();
        if (_data is null) return;
        foreach (var jwt in _data.EntraJwtValidationConfigs)
        {
            JwtConfigs.Add(new Dictionary<string, string>
            {
                ["Name"] = jwt.Name ?? "",
            });
        }
    }

    /// <summary>
    /// Pushes the modified _data to Azure and reloads.
    /// </summary>
    public async Task<bool> SaveChangesAsync(string actionDescription, CancellationToken cancellationToken = default)
    {
        if (_data is null || string.IsNullOrEmpty(ResourceId))
        {
            return false;
        }

        IsSaving = true;
        ErrorMessage = null;
        SaveMessage = null;

        try
        {
            await gatewayService.UpdateAsync(ResourceId, _data, cancellationToken);
            SaveMessage = actionDescription;

            // Reload to get fresh state
            await LoadAsync(ResourceId, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public bool DeleteBackendPool(string name)
    {
        if (_data is null) return false;
        var item = _data.BackendAddressPools.FirstOrDefault(p => p.Name == name);
        return item is not null && _data.BackendAddressPools.Remove(item);
    }

    public bool DeleteBackendSetting(string name)
    {
        if (_data is null) return false;
        var item = _data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == name);
        return item is not null && _data.BackendHttpSettingsCollection.Remove(item);
    }

    public bool DeleteListener(string name)
    {
        if (_data is null) return false;
        var item = _data.HttpListeners.FirstOrDefault(l => l.Name == name);
        return item is not null && _data.HttpListeners.Remove(item);
    }

    public bool DeleteRoutingRule(string name)
    {
        if (_data is null) return false;
        var item = _data.RequestRoutingRules.FirstOrDefault(r => r.Name == name);
        return item is not null && _data.RequestRoutingRules.Remove(item);
    }

    public bool DeleteHealthProbe(string name)
    {
        if (_data is null) return false;
        var item = _data.Probes.FirstOrDefault(p => p.Name == name);
        return item is not null && _data.Probes.Remove(item);
    }

    public bool DeleteRewriteSet(string name)
    {
        if (_data is null) return false;
        var item = _data.RewriteRuleSets.FirstOrDefault(r => r.Name == name);
        return item is not null && _data.RewriteRuleSets.Remove(item);
    }

    public bool DeleteFrontendPort(string name)
    {
        if (_data is null) return false;
        var item = _data.FrontendPorts.FirstOrDefault(p => p.Name == name);
        return item is not null && _data.FrontendPorts.Remove(item);
    }

    public bool DeleteSslCertificate(string name)
    {
        if (_data is null) return false;
        var item = _data.SslCertificates.FirstOrDefault(c => c.Name == name);
        return item is not null && _data.SslCertificates.Remove(item);
    }

    public bool DeletePrivateLink(string name)
    {
        if (_data is null) return false;
        var item = _data.PrivateLinkConfigurations.FirstOrDefault(p => p.Name == name);
        return item is not null && _data.PrivateLinkConfigurations.Remove(item);
    }

    /// <summary>
    /// Maps section to the delete method for that section's items.
    /// </summary>
    public bool DeleteItem(AppGwSection section, string name)
    {
        return section switch
        {
            AppGwSection.BackendPools => DeleteBackendPool(name),
            AppGwSection.BackendSettings => DeleteBackendSetting(name),
            AppGwSection.Listeners => DeleteListener(name),
            AppGwSection.RoutingRules => DeleteRoutingRule(name),
            AppGwSection.HealthProbes => DeleteHealthProbe(name),
            AppGwSection.RewriteSets => DeleteRewriteSet(name),
            AppGwSection.SslSettings => DeleteSslCertificate(name),
            AppGwSection.PrivateLink => DeletePrivateLink(name),
            _ => false,
        };
    }
}
