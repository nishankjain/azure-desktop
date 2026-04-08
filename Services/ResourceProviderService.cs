using System.Runtime.CompilerServices;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace AzureDesktop.Services;

public sealed record ResourceTypeInfo(
    string ResourceType,
    string DefaultApiVersion,
    IReadOnlyList<string> ApiVersions,
    IReadOnlyList<string> Locations,
    string Capabilities);

public sealed record ResourceProviderInfo(
    string Namespace,
    string RegistrationState,
    string RegistrationPolicy,
    IReadOnlyList<ResourceTypeInfo> ResourceTypes);

public interface IResourceProviderService
{
    IAsyncEnumerable<ResourceProviderInfo> GetResourceProvidersAsync(
        string subscriptionId, CancellationToken cancellationToken = default);

    Task RegisterAsync(string subscriptionId, string providerNamespace,
        CancellationToken cancellationToken = default);

    Task UnregisterAsync(string subscriptionId, string providerNamespace,
        CancellationToken cancellationToken = default);
}

public sealed class ResourceProviderService(IAzureAuthService authService) : IResourceProviderService
{
    public async IAsyncEnumerable<ResourceProviderInfo> GetResourceProvidersAsync(
        string subscriptionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var subscription = client.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        await foreach (var provider in subscription.GetResourceProviders()
            .GetAllAsync(cancellationToken: cancellationToken))
        {
            var resourceTypes = provider.Data.ResourceTypes
                .Select(rt => new ResourceTypeInfo(
                    rt.ResourceType ?? string.Empty,
                    rt.DefaultApiVersion ?? string.Empty,
                    rt.ApiVersions?.ToList() ?? [],
                    rt.Locations?.Select(l => l.ToString()).ToList() ?? [],
                    rt.Capabilities ?? string.Empty))
                .ToList();

            yield return new ResourceProviderInfo(
                provider.Data.Namespace ?? string.Empty,
                provider.Data.RegistrationState ?? "Unknown",
                provider.Data.RegistrationPolicy ?? "Unknown",
                resourceTypes);
        }
    }

    public async Task RegisterAsync(string subscriptionId, string providerNamespace,
        CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var subscription = client.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var provider = (await subscription.GetResourceProviders()
            .GetAsync(providerNamespace, cancellationToken: cancellationToken)).Value;

        await provider.RegisterAsync(cancellationToken: cancellationToken);
    }

    public async Task UnregisterAsync(string subscriptionId, string providerNamespace,
        CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var subscription = client.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var provider = (await subscription.GetResourceProviders()
            .GetAsync(providerNamespace, cancellationToken: cancellationToken)).Value;

        await provider.UnregisterAsync(cancellationToken);
    }
}
