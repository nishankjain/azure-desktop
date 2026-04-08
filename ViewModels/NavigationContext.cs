namespace AzureDesktop.ViewModels;

/// <summary>
/// Navigation context passed through the drill-down hierarchy.
/// </summary>
public sealed record NavigationContext(
    SubscriptionItem Subscription,
    string? ResourceGroupName = null,
    string? ResourceGroupLocation = null,
    ResourceItem? Resource = null)
{
    public string SubscriptionId => Subscription.Id;
    public string SubscriptionName => Subscription.Name;
}
