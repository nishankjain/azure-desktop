using Azure.ResourceManager.Resources;

namespace AzureDesktop.Services;

public interface ISubscriptionService
{
    IAsyncEnumerable<SubscriptionResource> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
}
