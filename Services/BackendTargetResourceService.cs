using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;

namespace AzureDesktop.Services;

public class BackendTargetResource
{
    public string DisplayName { get; set; } = "";
    public string ResolvedValue { get; set; } = "";
    public string TargetType { get; set; } = "";

    public BackendTargetResource() { }
    public BackendTargetResource(string displayName, string resolvedValue, string targetType)
    {
        DisplayName = displayName;
        ResolvedValue = resolvedValue;
        TargetType = targetType;
    }
}

public sealed class BackendTargetResourceService(IAzureAuthService authService)
{
    private readonly Dictionary<string, List<BackendTargetResource>> _cache = [];

    public void ClearCache() => _cache.Clear();

    public async Task<List<BackendTargetResource>> GetResourcesAsync(
        string targetType,
        string gatewayResourceId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{targetType}:{gatewayResourceId}";
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var gwId = new Azure.Core.ResourceIdentifier(gatewayResourceId);
        var subscriptionId = gwId.SubscriptionId!;
        var resourceGroupName = gwId.ResourceGroupName!;

        var results = targetType switch
        {
            "VM" => await GetVmResourcesAsync(subscriptionId, resourceGroupName, cancellationToken),
            "VMSS" => await GetVmssResourcesAsync(subscriptionId, resourceGroupName, cancellationToken),
            "App Service" => await GetAppServiceResourcesAsync(subscriptionId, resourceGroupName, cancellationToken),
            _ => []
        };

        _cache[cacheKey] = results;
        return results;
    }

    private async Task<List<BackendTargetResource>> GetVmResourcesAsync(
        string subscriptionId, string resourceGroupName, CancellationToken cancellationToken)
    {
        var results = new List<BackendTargetResource>();
        var client = authService.Client;
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
        var rg = client.GetResourceGroupResource(rgId);

        await foreach (var vm in rg.GetVirtualMachines().GetAllAsync(cancellationToken: cancellationToken))
        {
            if (vm.Data.NetworkProfile?.NetworkInterfaces is null) continue;

            foreach (var nicRef in vm.Data.NetworkProfile.NetworkInterfaces)
            {
                if (nicRef.Id is null) continue;
                var nicResource = client.GetNetworkInterfaceResource(nicRef.Id);
                var nic = await nicResource.GetAsync(cancellationToken: cancellationToken);

                foreach (var ipConfig in nic.Value.Data.IPConfigurations)
                {
                    var privateIp = ipConfig.PrivateIPAddress;
                    if (string.IsNullOrEmpty(privateIp)) continue;

                    var displayName = nic.Value.Data.IPConfigurations.Count > 1
                        ? $"{vm.Data.Name} ({nic.Value.Data.Name}/{ipConfig.Name} - {privateIp})"
                        : $"{vm.Data.Name} ({privateIp})";

                    results.Add(new BackendTargetResource(displayName, privateIp, "VM"));
                }
            }
        }

        return results;
    }

    private async Task<List<BackendTargetResource>> GetVmssResourcesAsync(
        string subscriptionId, string resourceGroupName, CancellationToken cancellationToken)
    {
        var results = new List<BackendTargetResource>();
        var client = authService.Client;
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
        var rg = client.GetResourceGroupResource(rgId);

        await foreach (var vmss in rg.GetVirtualMachineScaleSets().GetAllAsync(cancellationToken: cancellationToken))
        {
            // List VMSS instances and their NICs
            await foreach (var instance in vmss.GetVirtualMachineScaleSetVms().GetAllAsync(cancellationToken: cancellationToken))
            {
                if (instance.Data.NetworkProfile?.NetworkInterfaces is null) continue;

                foreach (var nicRef in instance.Data.NetworkProfile.NetworkInterfaces)
                {
                    if (nicRef.Id is null) continue;

                    try
                    {
                        var nicResource = client.GetNetworkInterfaceResource(nicRef.Id);
                        var nic = await nicResource.GetAsync(cancellationToken: cancellationToken);
                        foreach (var ipConfig in nic.Value.Data.IPConfigurations)
                        {
                            var privateIp = ipConfig.PrivateIPAddress;
                            if (string.IsNullOrEmpty(privateIp)) continue;
                            results.Add(new BackendTargetResource(
                                $"{vmss.Data.Name}/{instance.Data.Name} ({privateIp})",
                                privateIp, "VMSS"));
                        }
                    }
                    catch
                    {
                        // VMSS instance NICs may not be directly accessible
                    }
                }
            }
        }

        return results;
    }

    private async Task<List<BackendTargetResource>> GetAppServiceResourcesAsync(
        string subscriptionId, string resourceGroupName, CancellationToken cancellationToken)
    {
        var results = new List<BackendTargetResource>();
        var client = authService.Client;
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
        var rg = client.GetResourceGroupResource(rgId);

        await foreach (var site in rg.GetWebSites().GetAllAsync(cancellationToken: cancellationToken))
        {
            var defaultHostname = site.Data.DefaultHostName;
            if (string.IsNullOrEmpty(defaultHostname)) continue;

            results.Add(new BackendTargetResource(
                $"{site.Data.Name} ({defaultHostname})",
                defaultHostname, "App Service"));
        }

        return results;
    }
}
