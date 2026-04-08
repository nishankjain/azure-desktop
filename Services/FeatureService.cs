using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace AzureDesktop.Services;

public sealed record FeatureInfo(
    string ProviderNamespace,
    string FeatureName,
    string State,
    string ResourceType,
    string ResourceId);

public interface IFeatureService
{
    IAsyncEnumerable<string> GetResourceProviderNamespacesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FeatureInfo> GetFeaturesForProviderAsync(string subscriptionId, string providerNamespace, CancellationToken cancellationToken = default);
    Task<FeatureInfo?> LookupFeatureAsync(string subscriptionId, string providerNamespace, string featureName, CancellationToken cancellationToken = default);
    Task RegisterAsync(string subscriptionId, string providerNamespace, string featureName, CancellationToken cancellationToken = default);
    Task UnregisterAsync(string subscriptionId, string providerNamespace, string featureName, CancellationToken cancellationToken = default);
}

public sealed class FeatureService(IAzureAuthService authService) : IFeatureService
{
    public async IAsyncEnumerable<string> GetResourceProviderNamespacesAsync(
        string subscriptionId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var subscription = client.GetSubscriptionResource(
            new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        await foreach (var provider in subscription.GetResourceProviders()
            .GetAllAsync(cancellationToken: cancellationToken))
        {
            if (provider.Data.Namespace is not null)
            {
                yield return provider.Data.Namespace;
            }
        }
    }

    public async IAsyncEnumerable<FeatureInfo> GetFeaturesForProviderAsync(
        string subscriptionId, string providerNamespace,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var subscription = client.GetSubscriptionResource(
            new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        ResourceProviderResource provider;
        try
        {
            provider = (await subscription.GetResourceProviders()
                .GetAsync(providerNamespace, cancellationToken: cancellationToken)).Value;
        }
        catch
        {
            yield break;
        }

        await foreach (var feature in provider.GetFeatures().GetAllAsync(cancellationToken: cancellationToken))
        {
            var (ns, name) = ParseFeatureName(feature.Data.Name, providerNamespace);
            yield return new FeatureInfo(ns, name,
                feature.Data.FeatureState ?? "Unknown",
                feature.Data.ResourceType.ToString(),
                feature.Data.Id?.ToString() ?? "");
        }
    }

    public async Task<FeatureInfo?> LookupFeatureAsync(
        string subscriptionId, string providerNamespace, string featureName,
        CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var featureId = FeatureResource.CreateResourceIdentifier(subscriptionId, providerNamespace, featureName);
        var featureResource = client.GetFeatureResource(featureId);

        try
        {
            var response = await featureResource.GetAsync(cancellationToken);
            var data = response.Value.Data;
            var (ns, name) = ParseFeatureName(data.Name, providerNamespace);
            return new FeatureInfo(ns, name,
                data.FeatureState ?? "Unknown",
                data.ResourceType.ToString(),
                data.Id?.ToString() ?? "");
        }
        catch
        {
            return null;
        }
    }

    public async Task RegisterAsync(
        string subscriptionId, string providerNamespace, string featureName,
        CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var featureId = FeatureResource.CreateResourceIdentifier(subscriptionId, providerNamespace, featureName);
        var featureResource = client.GetFeatureResource(featureId);
        await featureResource.RegisterAsync(cancellationToken);
    }

    public async Task UnregisterAsync(
        string subscriptionId, string providerNamespace, string featureName,
        CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var featureId = FeatureResource.CreateResourceIdentifier(subscriptionId, providerNamespace, featureName);
        var featureResource = client.GetFeatureResource(featureId);
        await featureResource.UnregisterAsync(cancellationToken);
    }

    private static (string Namespace, string Name) ParseFeatureName(string? fullName, string fallbackNamespace)
    {
        if (fullName is null)
        {
            return (fallbackNamespace, "");
        }

        var parts = fullName.Split('/', 2);
        return parts.Length > 1 ? (parts[0], parts[1]) : (fallbackNamespace, fullName);
    }
}
