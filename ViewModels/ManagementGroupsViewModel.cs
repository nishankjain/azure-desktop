using System.Collections.ObjectModel;
using Azure;
using Azure.ResourceManager;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class ManagementGroupItem(string id, string displayName)
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
}

public partial class ManagementGroupsViewModel(
    IAzureAuthService authService,
    ISubscriptionService subscriptionService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>
    /// True when the user lacks MG read permissions and we fell back to flat subscription listing.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFlatMode { get; set; }

    public ObservableCollection<ManagementGroupItem> ManagementGroups { get; } = [];

    public ObservableCollection<SubscriptionItem> FlatSubscriptions { get; } = [];

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;
        ManagementGroups.Clear();
        FlatSubscriptions.Clear();
        IsFlatMode = false;

        try
        {
            var client = new ArmClient(authService.Credential);

            await foreach (var mg in client.GetManagementGroups().GetAllAsync(cancellationToken: cancellationToken))
            {
                ManagementGroups.Add(new ManagementGroupItem(
                    mg.Data.Name ?? "",
                    mg.Data.DisplayName ?? mg.Data.Name ?? "(unnamed)"));
            }

            if (ManagementGroups.Count == 0)
            {
                ErrorMessage = "No management groups found.";
            }
        }
        catch (OperationCanceledException)
        {
            // User navigated away
        }
        catch (RequestFailedException ex) when (ex.Status == 403)
        {
            // No MG permissions — fall back to flat subscription list
            await LoadFlatSubscriptionsAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Any other MG error — fall back to flat subscription list
            await LoadFlatSubscriptionsAsync(cancellationToken);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFlatSubscriptionsAsync(CancellationToken cancellationToken)
    {
        IsFlatMode = true;
        ErrorMessage = null;

        try
        {
            await foreach (var sub in subscriptionService.GetSubscriptionsAsync(cancellationToken))
            {
                FlatSubscriptions.Add(new SubscriptionItem(
                    sub.Data.SubscriptionId ?? "",
                    sub.Data.DisplayName ?? "(unnamed)",
                    sub.Data.State?.ToString() ?? "Unknown",
                    sub.Data.TenantId?.ToString() ?? "")
                {
                    AuthorizationSource = sub.Data.AuthorizationSource ?? "",
                    SpendingLimit = sub.Data.SubscriptionPolicies?.SpendingLimit?.ToString() ?? "",
                    QuotaId = sub.Data.SubscriptionPolicies?.QuotaId ?? "",
                    LocationPlacementId = sub.Data.SubscriptionPolicies?.LocationPlacementId ?? "",
                });
            }

            if (FlatSubscriptions.Count == 0)
            {
                ErrorMessage = "No subscriptions found.";
            }
        }
        catch (OperationCanceledException)
        {
            // User navigated away
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load subscriptions: {ex.Message}";
        }
    }
}
