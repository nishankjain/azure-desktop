using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

namespace AzureDesktop.Services;

public interface IApplicationGatewayService
{
    Task<ApplicationGatewayResource> CreateAsync(
        string subscriptionId,
        string resourceGroupName,
        string gatewayName,
        string location,
        ApplicationGatewayData data,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ApplicationGatewayResource> GetAllAsync(
        string subscriptionId,
        string resourceGroupName,
        CancellationToken cancellationToken = default);

    Task<ApplicationGatewayResource> UpdateAsync(
        string resourceId,
        ApplicationGatewayData data,
        CancellationToken cancellationToken = default);
}
