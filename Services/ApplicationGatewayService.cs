using System.Runtime.CompilerServices;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;

namespace AzureDesktop.Services;

public sealed class ApplicationGatewayService(IAzureAuthService authService) : IApplicationGatewayService
{
    public async Task<ApplicationGatewayResource> CreateAsync(
        string subscriptionId,
        string resourceGroupName,
        string gatewayName,
        string location,
        ApplicationGatewayData data,
        CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var resourceGroupId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
        var resourceGroup = client.GetResourceGroupResource(resourceGroupId);
        var collection = resourceGroup.GetApplicationGateways();

        var operation = await collection.CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            gatewayName,
            data,
            cancellationToken);

        return operation.Value;
    }

    public async IAsyncEnumerable<ApplicationGatewayResource> GetAllAsync(
        string subscriptionId,
        string resourceGroupName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var resourceGroupId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
        var resourceGroup = client.GetResourceGroupResource(resourceGroupId);
        var collection = resourceGroup.GetApplicationGateways();

        await foreach (var gateway in collection.GetAllAsync(cancellationToken))
        {
            yield return gateway;
        }
    }

    public async Task<ApplicationGatewayResource> UpdateAsync(
        string resourceId,
        ApplicationGatewayData data,
        CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var id = new Azure.Core.ResourceIdentifier(resourceId);
        var rgId = ResourceGroupResource.CreateResourceIdentifier(id.SubscriptionId!, id.ResourceGroupName!);
        var rg = client.GetResourceGroupResource(rgId);
        var collection = rg.GetApplicationGateways();
        var operation = await collection.CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            data.Name,
            data,
            cancellationToken);
        return operation.Value;
    }
}
