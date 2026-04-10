using System.Runtime.CompilerServices;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace AzureDesktop.Services;

public sealed class SubscriptionService(IAzureAuthService authService) : ISubscriptionService
{
    public async IAsyncEnumerable<SubscriptionResource> GetSubscriptionsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = authService.Client;

        await foreach (var subscription in client.GetSubscriptions().GetAllAsync(cancellationToken))
        {
            yield return subscription;
        }
    }
}
