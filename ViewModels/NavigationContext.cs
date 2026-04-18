using AzureDesktop.Views;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.ViewModels;

/// <summary>
/// Unified navigation context that describes the full drill-down hierarchy.
/// The breadcrumb chain is derived from which fields are populated.
/// </summary>
public sealed record NavigationContext(
    SubscriptionItem Subscription,
    string? ResourceGroupName = null,
    string? ResourceGroupLocation = null,
    ResourceItem? Resource = null,
    AppGwSection? Section = null,
    string? DetailItemName = null,
    FeatureEntry? Feature = null,
    ResourceProviderEntry? ResourceProvider = null,
    string? PageLabel = null)
{
    public string SubscriptionId => Subscription.Id;
    public string SubscriptionName => Subscription.Name;

    /// <summary>
    /// Builds the breadcrumb chain from the populated fields.
    /// Returns (label, pageType, contextForNavigation) triples from root to current.
    /// </summary>
    public List<(string Label, Type PageType, NavigationContext Context)> BuildBreadcrumbChain()
    {
        var chain = new List<(string, Type, NavigationContext)>();
        var root = this with { ResourceGroupName = null, ResourceGroupLocation = null, Resource = null,
            Section = null, DetailItemName = null, Feature = null, ResourceProvider = null, PageLabel = null };

        chain.Add(("Subscriptions", typeof(SubscriptionsPage), root));
        chain.Add(("Subscription", typeof(SubscriptionDetailPage), root));

        // Feature path
        if (Feature is not null)
        {
            chain.Add(("Preview Features", typeof(FeaturesPage), root));
            chain.Add(("Feature", typeof(FeatureDetailPage), this));
            return chain;
        }

        if (PageLabel == "Preview Features")
        {
            chain.Add(("Preview Features", typeof(FeaturesPage), this));
            return chain;
        }

        // Resource Provider path
        if (ResourceProvider is not null)
        {
            chain.Add(("Resource Providers", typeof(ResourceProvidersPage), root));
            chain.Add(("Resource Provider", typeof(ResourceProviderDetailPage), this));
            return chain;
        }

        if (PageLabel == "Resource Providers")
        {
            chain.Add(("Resource Providers", typeof(ResourceProvidersPage), this));
            return chain;
        }

        // Resource Group path
        if (ResourceGroupName is null)
        {
            if (PageLabel is not null)
            {
                chain.Add((PageLabel, typeof(Page), this));
            }

            return chain;
        }

        var rgCtx = this with { Resource = null, Section = null, DetailItemName = null, PageLabel = null };
        chain.Add(("Resource Group", typeof(ResourceGroupDetailPage), rgCtx));

        if (Resource is null)
        {
            if (PageLabel is not null)
            {
                chain.Add((PageLabel, typeof(Page), this));
            }

            return chain;
        }

        var resCtx = this with { Section = null, DetailItemName = null, PageLabel = null };
        chain.Add((Resource.SingularType, typeof(ResourceDetailPage), resCtx));

        // Section level
        if (Section is not null)
        {
            var sectionCtx = this with { DetailItemName = null, PageLabel = null };
            var sectionPageType = SectionToPageType(Section.Value);
            chain.Add((SectionToTitle(Section.Value), sectionPageType, sectionCtx));

            if (DetailItemName is not null)
            {
                chain.Add((DetailItemName, typeof(Page), this));
            }
        }

        if (PageLabel is not null && Section is null)
        {
            chain.Add((PageLabel, typeof(Page), this));
        }

        return chain;
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

    private static Type SectionToPageType(AppGwSection section) => section switch
    {
        AppGwSection.Overview => typeof(AppGwOverviewPage),
        AppGwSection.BackendPools => typeof(AppGwBackendPoolsPage),
        AppGwSection.BackendSettings => typeof(AppGwBackendSettingsPage),
        AppGwSection.FrontendIP => typeof(AppGwFrontendIPPage),
        AppGwSection.PrivateLink => typeof(AppGwPrivateLinkPage),
        AppGwSection.SslSettings => typeof(AppGwSslSettingsPage),
        AppGwSection.Listeners => typeof(AppGwListenersPage),
        AppGwSection.RoutingRules => typeof(AppGwRoutingRulesPage),
        AppGwSection.RewriteSets => typeof(AppGwRewriteSetsPage),
        AppGwSection.HealthProbes => typeof(AppGwHealthProbesPage),
        AppGwSection.Configuration => typeof(AppGwConfigurationPage),
        AppGwSection.Waf => typeof(AppGwWafPage),
        AppGwSection.JwtValidation => typeof(AppGwJwtValidationPage),
        _ => typeof(AppGwOverviewPage),
    };
}
