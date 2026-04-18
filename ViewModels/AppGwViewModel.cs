using System.Collections.ObjectModel;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
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

public partial class AppGwViewModel(IAzureAuthService authService, IApplicationGatewayService gatewayService, OperationManager operationManager) : ObservableObject, ILoadable
{
    private ApplicationGatewayData? _data;

    public ApplicationGatewayData? Data => _data;

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
            var client = authService.Client;
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

    private static string? SubResourceName(Azure.Core.ResourceIdentifier? id)
    {
        if (id is null) return null;
        var name = id.Name;
        if (name is null) return null;
        var idx = name.LastIndexOf('/');
        return idx >= 0 ? name[(idx + 1)..] : name;
    }

    private void PopulateBackendPools()
    {
        BackendPools.Clear();
        if (_data is null) return;
        foreach (var pool in _data.BackendAddressPools)
        {
            // Count direct routing rule references
            var directRules = _data.RequestRoutingRules
                .Count(r => SubResourceName(r.BackendAddressPoolId) == pool.Name);

            // Count URL path map references
            var pathMapRules = _data.UrlPathMaps
                .Count(m => SubResourceName(m.DefaultBackendAddressPoolId) == pool.Name
                    || (m.PathRules?.Any(pr => SubResourceName(pr.BackendAddressPoolId) == pool.Name) ?? false));

            BackendPools.Add(new Dictionary<string, string>
            {
                ["Name"] = pool.Name ?? "",
                ["Targets"] = (pool.BackendAddresses?.Count ?? 0).ToString(),
                ["Associated Rules"] = (directRules + pathMapRules).ToString(),
            });
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
                ["Frontend IP"] = l.FrontendIPConfigurationId?.Name ?? "",
                ["Frontend Port"] = l.FrontendPortId?.Name ?? "",
                ["SSL Certificate"] = l.SslCertificateId?.Name ?? "",
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
                ["Listener"] = SubResourceName(r.HttpListenerId) ?? "",
                ["Backend Pool"] = SubResourceName(r.BackendAddressPoolId) ?? "",
                ["Backend Settings"] = SubResourceName(r.BackendHttpSettingsId) ?? "",
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
        var op = operationManager.Begin(actionDescription, "Saving", "Saved", Name, "Application Gateway", ResourceId);

        try
        {
            await gatewayService.UpdateAsync(ResourceId, _data, cancellationToken);
            SaveMessage = actionDescription;
            op.Complete();

            // Reload to get fresh state
            await LoadAsync(ResourceId, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
            op.Fail(ex.Message);
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

    /// <summary>Returns editable field definitions for a section (field name, placeholder).</summary>
    public static List<(string Field, string Placeholder)> GetEditableFields(AppGwSection section) => section switch
    {
        AppGwSection.BackendPools => [("Name", "Pool name"), ("Addresses", "Comma-separated IPs/FQDNs")],
        AppGwSection.BackendSettings => [("Name", "Settings name"), ("Protocol", "Http/Https"), ("Port", "80"), ("Cookie Affinity", "Enabled/Disabled"), ("Request Timeout", "30"), ("Host Name", "hostname")],
        AppGwSection.Listeners => [("Name", "Listener name"), ("Protocol", "Http/Https"), ("Host Name", "*.example.com"), ("Require SNI", "True/False"), ("Frontend IP", "Frontend IP config name"), ("Frontend Port", "Frontend port name"), ("SSL Certificate", "SSL cert name (for HTTPS)")],
        AppGwSection.RoutingRules => [("Name", "Rule name"), ("Rule Type", "Basic/PathBasedRouting"), ("Priority", "100"), ("Listener", "Listener name"), ("Backend Pool", "Backend pool name"), ("Backend Settings", "Backend settings name")],
        AppGwSection.HealthProbes => [("Name", "Probe name"), ("Protocol", "Http/Https"), ("Host", "hostname"), ("Path", "/health"), ("Interval", "30"), ("Timeout", "30"), ("Unhealthy Threshold", "3"), ("Pick Host From Backend", "True/False")],
        AppGwSection.FrontendIP => [("Name", "Port name"), ("Port", "80")],
        AppGwSection.SslSettings => [("Name", "Certificate name")],
        AppGwSection.RewriteSets => [("Name", "Rewrite set name")],
        _ => [],
    };

    public bool EditItem(AppGwSection section, string originalName, Dictionary<string, string> values)
    {
        if (_data is null) return false;

        switch (section)
        {
            case AppGwSection.BackendPools:
                var pool = _data.BackendAddressPools.FirstOrDefault(p => p.Name == originalName);
                if (pool is null) return false;
                pool.BackendAddresses.Clear();
                foreach (var addr in (values.GetValueOrDefault("Addresses") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var ba = new ApplicationGatewayBackendAddress();
                    if (System.Net.IPAddress.TryParse(addr, out _))
                        ba.IPAddress = addr;
                    else
                        ba.Fqdn = addr;
                    pool.BackendAddresses.Add(ba);
                }
                return true;

            case AppGwSection.BackendSettings:
                var setting = _data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == originalName);
                if (setting is null) return false;
                if (values.TryGetValue("Protocol", out var proto))
                    setting.Protocol = proto.Equals("Https", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayProtocol.Https : ApplicationGatewayProtocol.Http;
                if (values.TryGetValue("Port", out var portStr) && int.TryParse(portStr, out var port))
                    setting.Port = port;
                if (values.TryGetValue("Cookie Affinity", out var cookie))
                    setting.CookieBasedAffinity = cookie.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayCookieBasedAffinity.Enabled : ApplicationGatewayCookieBasedAffinity.Disabled;
                if (values.TryGetValue("Request Timeout", out var timeoutStr) && int.TryParse(timeoutStr.TrimEnd('s'), out var timeout))
                    setting.RequestTimeoutInSeconds = timeout;
                if (values.TryGetValue("Host Name", out var host))
                    setting.HostName = host;
                return true;

            case AppGwSection.Listeners:
                var listener = _data.HttpListeners.FirstOrDefault(l => l.Name == originalName);
                if (listener is null) return false;
                if (values.TryGetValue("Protocol", out var lProto))
                    listener.Protocol = lProto.Equals("Https", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayProtocol.Https : ApplicationGatewayProtocol.Http;
                if (values.TryGetValue("Host Name", out var lHost))
                    listener.HostName = lHost;
                if (values.TryGetValue("Require SNI", out var lSni))
                    listener.RequireServerNameIndication = lSni.Equals("True", StringComparison.OrdinalIgnoreCase);
                if (values.TryGetValue("Frontend IP", out var lFip) && !string.IsNullOrWhiteSpace(lFip))
                {
                    var fip = _data.FrontendIPConfigurations.FirstOrDefault(f => f.Name == lFip);
                    if (fip is not null) listener.FrontendIPConfigurationId = fip.Id;
                }
                if (values.TryGetValue("Frontend Port", out var lFp) && !string.IsNullOrWhiteSpace(lFp))
                {
                    var fp = _data.FrontendPorts.FirstOrDefault(f => f.Name == lFp);
                    if (fp is not null) listener.FrontendPortId = fp.Id;
                }
                if (values.TryGetValue("SSL Certificate", out var lSsl) && !string.IsNullOrWhiteSpace(lSsl))
                {
                    var cert = _data.SslCertificates.FirstOrDefault(c => c.Name == lSsl);
                    if (cert is not null) listener.SslCertificateId = cert.Id;
                }
                return true;

            case AppGwSection.HealthProbes:
                var probe = _data.Probes.FirstOrDefault(p => p.Name == originalName);
                if (probe is null) return false;
                if (values.TryGetValue("Protocol", out var pProto))
                    probe.Protocol = pProto.Equals("Https", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayProtocol.Https : ApplicationGatewayProtocol.Http;
                if (values.TryGetValue("Host", out var pHost))
                    probe.Host = pHost;
                if (values.TryGetValue("Path", out var pPath))
                    probe.Path = pPath;
                if (values.TryGetValue("Interval", out var intStr) && int.TryParse(intStr.TrimEnd('s'), out var interval))
                    probe.IntervalInSeconds = interval;
                if (values.TryGetValue("Timeout", out var toStr) && int.TryParse(toStr.TrimEnd('s'), out var to))
                    probe.TimeoutInSeconds = to;
                if (values.TryGetValue("Unhealthy Threshold", out var thStr) && int.TryParse(thStr, out var threshold))
                    probe.UnhealthyThreshold = threshold;
                if (values.TryGetValue("Pick Host From Backend", out var pickHost))
                    probe.PickHostNameFromBackendHttpSettings = pickHost.Equals("True", StringComparison.OrdinalIgnoreCase);
                return true;

            case AppGwSection.RoutingRules:
                var rule = _data.RequestRoutingRules.FirstOrDefault(r => r.Name == originalName);
                if (rule is null) return false;
                if (values.TryGetValue("Rule Type", out var rType))
                    rule.RuleType = rType.Equals("PathBasedRouting", StringComparison.OrdinalIgnoreCase)
                        ? ApplicationGatewayRequestRoutingRuleType.PathBasedRouting
                        : ApplicationGatewayRequestRoutingRuleType.Basic;
                if (values.TryGetValue("Priority", out var rPri) && int.TryParse(rPri, out var priVal))
                    rule.Priority = priVal;
                if (values.TryGetValue("Listener", out var rListener) && !string.IsNullOrWhiteSpace(rListener))
                {
                    var hl = _data.HttpListeners.FirstOrDefault(l => l.Name == rListener);
                    if (hl is not null) rule.HttpListenerId = hl.Id;
                }
                if (values.TryGetValue("Backend Pool", out var rPool) && !string.IsNullOrWhiteSpace(rPool))
                {
                    var bp = _data.BackendAddressPools.FirstOrDefault(p => p.Name == rPool);
                    if (bp is not null) rule.BackendAddressPoolId = bp.Id;
                }
                if (values.TryGetValue("Backend Settings", out var rSettings) && !string.IsNullOrWhiteSpace(rSettings))
                {
                    var bs = _data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == rSettings);
                    if (bs is not null) rule.BackendHttpSettingsId = bs.Id;
                }
                return true;

            default:
                return false;
        }
    }

    public bool AddItem(AppGwSection section, Dictionary<string, string> values)
    {
        if (_data is null) return false;
        var name = values.GetValueOrDefault("Name") ?? "";
        if (string.IsNullOrWhiteSpace(name)) return false;

        switch (section)
        {
            case AppGwSection.BackendPools:
                var pool = new ApplicationGatewayBackendAddressPool { Name = name };
                foreach (var addr in (values.GetValueOrDefault("Addresses") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var ba = new ApplicationGatewayBackendAddress();
                    if (System.Net.IPAddress.TryParse(addr, out _)) ba.IPAddress = addr; else ba.Fqdn = addr;
                    pool.BackendAddresses.Add(ba);
                }
                _data.BackendAddressPools.Add(pool);
                return true;

            case AppGwSection.BackendSettings:
                var s = new ApplicationGatewayBackendHttpSettings { Name = name };
                if (values.TryGetValue("Protocol", out var sp)) s.Protocol = sp.Equals("Https", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayProtocol.Https : ApplicationGatewayProtocol.Http;
                if (values.TryGetValue("Port", out var sPort) && int.TryParse(sPort, out var spInt)) s.Port = spInt;
                if (values.TryGetValue("Cookie Affinity", out var sc)) s.CookieBasedAffinity = sc.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayCookieBasedAffinity.Enabled : ApplicationGatewayCookieBasedAffinity.Disabled;
                if (values.TryGetValue("Request Timeout", out var st) && int.TryParse(st.TrimEnd('s'), out var stInt)) s.RequestTimeoutInSeconds = stInt;
                if (values.TryGetValue("Host Name", out var sh)) s.HostName = sh;
                _data.BackendHttpSettingsCollection.Add(s);
                return true;

            case AppGwSection.HealthProbes:
                var p = new ApplicationGatewayProbe { Name = name };
                if (values.TryGetValue("Protocol", out var pp)) p.Protocol = pp.Equals("Https", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayProtocol.Https : ApplicationGatewayProtocol.Http;
                if (values.TryGetValue("Host", out var ph)) p.Host = ph;
                if (values.TryGetValue("Path", out var ppa)) p.Path = ppa;
                if (values.TryGetValue("Interval", out var pi) && int.TryParse(pi.TrimEnd('s'), out var piInt)) p.IntervalInSeconds = piInt;
                if (values.TryGetValue("Timeout", out var pt) && int.TryParse(pt.TrimEnd('s'), out var ptInt)) p.TimeoutInSeconds = ptInt;
                if (values.TryGetValue("Unhealthy Threshold", out var pu) && int.TryParse(pu, out var puInt)) p.UnhealthyThreshold = puInt;
                _data.Probes.Add(p);
                return true;

            case AppGwSection.FrontendIP: // Actually frontend ports via the "Add" in FrontendIP section
                var fp = new ApplicationGatewayFrontendPort { Name = name };
                if (values.TryGetValue("Port", out var fpPort) && int.TryParse(fpPort, out var fpInt)) fp.Port = fpInt;
                _data.FrontendPorts.Add(fp);
                return true;

            case AppGwSection.Listeners:
                var newListener = new ApplicationGatewayHttpListener { Name = name };
                if (values.TryGetValue("Protocol", out var lp2))
                    newListener.Protocol = lp2.Equals("Https", StringComparison.OrdinalIgnoreCase) ? ApplicationGatewayProtocol.Https : ApplicationGatewayProtocol.Http;
                if (values.TryGetValue("Host Name", out var lh2))
                    newListener.HostName = lh2;
                if (values.TryGetValue("Require SNI", out var lSni2))
                    newListener.RequireServerNameIndication = lSni2.Equals("True", StringComparison.OrdinalIgnoreCase);
                if (values.TryGetValue("Frontend IP", out var lFip2) && !string.IsNullOrWhiteSpace(lFip2))
                {
                    var fip = _data.FrontendIPConfigurations.FirstOrDefault(f => f.Name == lFip2);
                    if (fip is not null) newListener.FrontendIPConfigurationId = fip.Id;
                }
                if (values.TryGetValue("Frontend Port", out var lFp2) && !string.IsNullOrWhiteSpace(lFp2))
                {
                    var fp2 = _data.FrontendPorts.FirstOrDefault(f => f.Name == lFp2);
                    if (fp2 is not null) newListener.FrontendPortId = fp2.Id;
                }
                if (values.TryGetValue("SSL Certificate", out var lSsl2) && !string.IsNullOrWhiteSpace(lSsl2))
                {
                    var cert = _data.SslCertificates.FirstOrDefault(c => c.Name == lSsl2);
                    if (cert is not null) newListener.SslCertificateId = cert.Id;
                }
                _data.HttpListeners.Add(newListener);
                return true;

            case AppGwSection.RoutingRules:
                var rr = new ApplicationGatewayRequestRoutingRule { Name = name };
                if (values.TryGetValue("Rule Type", out var rt2))
                    rr.RuleType = rt2.Equals("PathBasedRouting", StringComparison.OrdinalIgnoreCase)
                        ? ApplicationGatewayRequestRoutingRuleType.PathBasedRouting
                        : ApplicationGatewayRequestRoutingRuleType.Basic;
                if (values.TryGetValue("Priority", out var rp2) && int.TryParse(rp2, out var rpInt2))
                    rr.Priority = rpInt2;
                if (values.TryGetValue("Listener", out var rL2) && !string.IsNullOrWhiteSpace(rL2))
                {
                    var hl = _data.HttpListeners.FirstOrDefault(l => l.Name == rL2);
                    if (hl is not null) rr.HttpListenerId = hl.Id;
                }
                if (values.TryGetValue("Backend Pool", out var rBp2) && !string.IsNullOrWhiteSpace(rBp2))
                {
                    var bp = _data.BackendAddressPools.FirstOrDefault(p => p.Name == rBp2);
                    if (bp is not null) rr.BackendAddressPoolId = bp.Id;
                }
                if (values.TryGetValue("Backend Settings", out var rBs2) && !string.IsNullOrWhiteSpace(rBs2))
                {
                    var bs = _data.BackendHttpSettingsCollection.FirstOrDefault(s => s.Name == rBs2);
                    if (bs is not null) rr.BackendHttpSettingsId = bs.Id;
                }
                _data.RequestRoutingRules.Add(rr);
                return true;

            default:
                return false;
        }
    }
}
