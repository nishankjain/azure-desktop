using CommunityToolkit.Mvvm.ComponentModel;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class ResourceDetailViewModel(IAzureAuthService authService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Type { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResourceId { get; set; } = string.Empty;

    // App Gateway specific
    [ObservableProperty]
    public partial bool IsAppGateway { get; set; }

    [ObservableProperty]
    public partial string SkuName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SkuTier { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SkuCapacity { get; set; }

    [ObservableProperty]
    public partial string OperationalState { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProvisioningState { get; set; } = string.Empty;

    public System.Collections.ObjectModel.ObservableCollection<string> FrontendIPs { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<string> BackendPools { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<string> HttpListeners { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<string> RoutingRules { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<string> HealthProbes { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<string> HttpSettings { get; } = [];

    public async Task LoadAsync(ResourceItem resource, CancellationToken cancellationToken = default)
    {
        Name = resource.Name;
        Type = resource.Type;
        Location = resource.Location;
        ResourceId = resource.ResourceId;
        IsAppGateway = resource.Type.Equals("Microsoft.Network/applicationGateways", StringComparison.OrdinalIgnoreCase);

        if (IsAppGateway)
        {
            // Load AppGW data into the shared AppGwViewModel
            IsLoading = true;
            try
            {
                var appGwVm = App.GetService<AppGwViewModel>();
                await appGwVm.LoadAsync(resource.ResourceId, cancellationToken);

                // Copy basic fields from AppGW VM
                SkuName = appGwVm.SkuName;
                SkuTier = appGwVm.SkuTier;
                SkuCapacity = appGwVm.SkuCapacity;
                OperationalState = appGwVm.OperationalState;
                ProvisioningState = appGwVm.ProvisioningState;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task LoadAppGatewayDetailsAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var client = new ArmClient(authService.Credential);
            var gwResource = client.GetApplicationGatewayResource(
                new Azure.Core.ResourceIdentifier(ResourceId));
            var gw = await gwResource.GetAsync(cancellationToken);
            var data = gw.Value.Data;

            SkuName = data.Sku?.Name?.ToString() ?? "";
            SkuTier = data.Sku?.Tier?.ToString() ?? "";
            SkuCapacity = data.Sku?.Capacity ?? 0;
            OperationalState = data.OperationalState?.ToString() ?? "";
            ProvisioningState = data.ProvisioningState?.ToString() ?? "";

            FrontendIPs.Clear();
            foreach (var fip in data.FrontendIPConfigurations)
            {
                var label = fip.Name ?? "";
                if (fip.PrivateIPAddress is not null)
                {
                    label += $" (Private: {fip.PrivateIPAddress})";
                }

                FrontendIPs.Add(label);
            }

            BackendPools.Clear();
            foreach (var pool in data.BackendAddressPools)
            {
                var count = pool.BackendAddresses?.Count ?? 0;
                BackendPools.Add($"{pool.Name} ({count} addresses)");
            }

            HttpListeners.Clear();
            foreach (var listener in data.HttpListeners)
            {
                var protocol = listener.Protocol?.ToString() ?? "";
                HttpListeners.Add($"{listener.Name} ({protocol})");
            }

            RoutingRules.Clear();
            foreach (var rule in data.RequestRoutingRules)
            {
                RoutingRules.Add($"{rule.Name} ({rule.RuleType?.ToString() ?? ""})");
            }

            HealthProbes.Clear();
            foreach (var probe in data.Probes)
            {
                var protocol = probe.Protocol?.ToString() ?? "";
                HealthProbes.Add($"{probe.Name} ({protocol} \u2192 {probe.Host}{probe.Path})");
            }

            HttpSettings.Clear();
            foreach (var settings in data.BackendHttpSettingsCollection)
            {
                var protocol = settings.Protocol?.ToString() ?? "";
                HttpSettings.Add($"{settings.Name} ({protocol}, port: {settings.Port})");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load details: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
