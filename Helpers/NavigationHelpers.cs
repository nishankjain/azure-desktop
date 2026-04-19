using AzureDesktop.Views;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Helpers;

/// <summary>
/// Shared navigation item definitions for Application Gateway pages.
/// </summary>
public static class AppGwNavItems
{
    public static NavItemDefinition[] Get() =>
    [
        new("Overview", "AppGwOverview", "applicationGateways.svg", typeof(AppGwOverviewPage)),
        new("Backend Pools", "AppGwBackendPools", "backendPools.svg", typeof(AppGwBackendPoolsPage)),
        new("Backend Settings", "AppGwBackendSettings", "backendSettings.svg", typeof(AppGwBackendSettingsPage)),
        new("Frontend IP", "AppGwFrontendIP", "frontendIP.svg", typeof(AppGwFrontendIPPage)),
        new("Private Link", "AppGwPrivateLink", "privateLink.svg", typeof(AppGwPrivateLinkPage)),
        new("SSL Settings", "AppGwSsl", "sslSettings.svg", typeof(AppGwSslSettingsPage)),
        new("Listeners", "AppGwListeners", "listeners.svg", typeof(AppGwListenersPage)),
        new("Routing Rules", "AppGwRoutingRules", "routingRules.svg", typeof(AppGwRoutingRulesPage)),
        new("Rewrite Sets", "AppGwRewriteSets", "rewriteSets.svg", typeof(AppGwRewriteSetsPage)),
        new("Health Probes", "AppGwHealthProbes", "healthProbes.svg", typeof(AppGwHealthProbesPage)),
        new("Configuration", "AppGwConfig", "configuration.svg", typeof(AppGwConfigurationPage)),
        new("WAF", "AppGwWaf", "waf.svg", typeof(AppGwWafPage)),
        new("JWT Validation", "AppGwJwt", "jwtValidation.svg", typeof(AppGwJwtValidationPage)),
        new("Tags", "Tags", "tags.svg", typeof(TagsPage)),
        new("Locks", "Locks", "locks.svg", typeof(LocksPage)),
    ];
}

/// <summary>
/// Shared navigation item definitions for subscription-level pages.
/// </summary>
public static class SubscriptionNavItems
{
    public static NavItemDefinition[] Get() =>
    [
        new("Overview", "SubscriptionDetail", "subscriptions.svg", typeof(SubscriptionDetailPage)),
        new("Resource Groups", "ResourceGroups", "resourceGroups.svg", typeof(ResourceGroupsPage)),
        new("Tags", "Tags", "tags.svg", typeof(TagsPage)),
        new("Locks", "Locks", "locks.svg", typeof(LocksPage)),
        new("Preview Features", "PreviewFeatures", "previewFeatures.svg", typeof(FeaturesPage)),
    ];
}

/// <summary>
/// Shared navigation item definitions for resource group-level pages.
/// </summary>
public static class ResourceGroupNavItems
{
    public static NavItemDefinition[] Get() =>
    [
        new("Overview", "RGDetail", "resourceGroups.svg", typeof(ResourceGroupDetailPage)),
        new("Resources", "Resources", "default.svg", typeof(ResourcesPage)),
        new("Tags", "Tags", "tags.svg", typeof(TagsPage)),
        new("Locks", "Locks", "locks.svg", typeof(LocksPage)),
    ];
}

/// <summary>
/// Shared navigation item definitions for generic resource-level pages.
/// </summary>
public static class ResourceNavItems
{
    public static NavItemDefinition[] Get(string resourceType)
    {
        var iconFile = ResourceIconResolver.GetIconFileName(resourceType);
        return
        [
            new("Overview", "ResourceDetail", iconFile, typeof(ResourceDetailPage)),
            new("Tags", "Tags", "tags.svg", typeof(TagsPage)),
            new("Locks", "Locks", "locks.svg", typeof(LocksPage)),
        ];
    }
}

/// <summary>
/// Maps Azure resource types to human-readable labels for breadcrumbs.
/// </summary>
public static class ResourceTypeLabels
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.Resources/subscriptions"] = "Subscription",
        ["Microsoft.Resources/subscriptions/resourceGroups"] = "Resource Group",
        ["Microsoft.Network/applicationGateways"] = "Application Gateway",
        ["Microsoft.Network/applicationGateways/backendAddressPools"] = "Backend Pool",
        ["Microsoft.Network/applicationGateways/backendHttpSettingsCollection"] = "Backend Settings",
        ["Microsoft.Network/applicationGateways/frontendIPConfigurations"] = "Frontend IP",
        ["Microsoft.Network/applicationGateways/frontendPorts"] = "Frontend Port",
        ["Microsoft.Network/applicationGateways/httpListeners"] = "Listener",
        ["Microsoft.Network/applicationGateways/requestRoutingRules"] = "Routing Rule",
        ["Microsoft.Network/applicationGateways/rewriteRuleSets"] = "Rewrite Set",
        ["Microsoft.Network/applicationGateways/probes"] = "Health Probe",
        ["Microsoft.Network/applicationGateways/sslCertificates"] = "SSL Certificate",
        ["Microsoft.Network/applicationGateways/privateLinkConfigurations"] = "Private Link",
        ["Microsoft.Network/applicationGateways/urlPathMaps"] = "URL Path Map",
    };

    public static string GetLabel(string resourceType)
    {
        if (Labels.TryGetValue(resourceType, out var label))
            return label;

        // Fallback: extract last segment and humanize
        var lastSlash = resourceType.LastIndexOf('/');
        return lastSlash >= 0 ? resourceType[(lastSlash + 1)..] : resourceType;
    }
}
