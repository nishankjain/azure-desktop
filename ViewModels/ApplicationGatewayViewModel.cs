using System.Collections.ObjectModel;
using Azure.Core;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class ApplicationGatewayItem(string name, string location, string state, string resourceGroup)
{
    public string Name { get; } = name;
    public string Location { get; } = location;
    public string State { get; } = state;
    public string ResourceGroup { get; } = resourceGroup;
}

public partial class ApplicationGatewayViewModel : ObservableObject
{
    private readonly IApplicationGatewayService _gatewayService;
    private readonly ISubscriptionService _subscriptionService;

    public ApplicationGatewayViewModel(
        IApplicationGatewayService gatewayService,
        ISubscriptionService subscriptionService)
    {
        _gatewayService = gatewayService;
        _subscriptionService = subscriptionService;
        GatewayName = "";
        ResourceGroupName = "";
        Location = "eastus";
    }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsCreating { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    // Create form fields
    [ObservableProperty]
    public partial string GatewayName { get; set; }

    [ObservableProperty]
    public partial string ResourceGroupName { get; set; }

    [ObservableProperty]
    public partial string Location { get; set; }

    [ObservableProperty]
    public partial SubscriptionItem? SelectedSubscription { get; set; }

    public ObservableCollection<SubscriptionItem> Subscriptions { get; } = [];
    public ObservableCollection<ApplicationGatewayItem> Gateways { get; } = [];

    [RelayCommand]
    private async Task LoadSubscriptionsAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        Subscriptions.Clear();

        try
        {
            await foreach (var sub in _subscriptionService.GetSubscriptionsAsync(cancellationToken))
            {
                Subscriptions.Add(new SubscriptionItem(
                    sub.Data.SubscriptionId ?? "",
                    sub.Data.DisplayName ?? "(unnamed)",
                    sub.Data.State?.ToString() ?? "Unknown",
                    sub.Data.TenantId?.ToString() ?? ""));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load subscriptions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateGatewayAsync(CancellationToken cancellationToken)
    {
        if (SelectedSubscription is null || string.IsNullOrWhiteSpace(GatewayName) || string.IsNullOrWhiteSpace(ResourceGroupName))
        {
            ErrorMessage = "Please fill in all fields: subscription, resource group, and gateway name.";
            return;
        }

        IsCreating = true;
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            var gatewayData = new ApplicationGatewayData
            {
                Location = new AzureLocation(Location),
                Sku = new ApplicationGatewaySku
                {
                    Name = ApplicationGatewaySkuName.StandardV2,
                    Tier = ApplicationGatewayTier.StandardV2,
                    Capacity = 2
                }
            };

            // Add a default frontend IP configuration
            gatewayData.GatewayIPConfigurations.Add(new ApplicationGatewayIPConfiguration
            {
                Name = "appGatewayIpConfig"
            });

            var result = await _gatewayService.CreateAsync(
                SelectedSubscription.Id,
                ResourceGroupName,
                GatewayName,
                Location,
                gatewayData,
                cancellationToken);

            StatusMessage = $"Application Gateway '{result.Data.Name}' created successfully in {result.Data.Location}.";

            // Refresh the list
            await LoadGatewaysAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create gateway: {ex.Message}";
        }
        finally
        {
            IsCreating = false;
        }
    }

    [RelayCommand]
    private async Task LoadGatewaysAsync(CancellationToken cancellationToken)
    {
        if (SelectedSubscription is null || string.IsNullOrWhiteSpace(ResourceGroupName))
            return;

        IsLoading = true;
        Gateways.Clear();

        try
        {
            await foreach (var gw in _gatewayService.GetAllAsync(
                SelectedSubscription.Id, ResourceGroupName, cancellationToken))
            {
                Gateways.Add(new ApplicationGatewayItem(
                    gw.Data.Name,
                    gw.Data.Location?.ToString() ?? "",
                    gw.Data.OperationalState?.ToString() ?? "Unknown",
                    ResourceGroupName));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load gateways: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
