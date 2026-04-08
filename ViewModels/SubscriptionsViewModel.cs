using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class SubscriptionItem(string id, string name, string state, string tenantId)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string State { get; } = state;
    public string TenantId { get; } = tenantId;
    public string AuthorizationSource { get; init; } = string.Empty;
    public string SpendingLimit { get; init; } = string.Empty;
    public string QuotaId { get; init; } = string.Empty;
    public string LocationPlacementId { get; init; } = string.Empty;
}

public partial class SubscriptionsViewModel(ISubscriptionService subscriptionService) : ObservableObject
{
    private readonly List<SubscriptionItem> _allSubscriptions = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSubscriptions))]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSubscriptions))]
    public partial string SortField { get; set; } = "Name";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSubscriptions))]
    public partial bool SortAscending { get; set; } = true;

    public IReadOnlyList<SubscriptionItem> FilteredSubscriptions => ApplySearchAndSort();

    [RelayCommand]
    private async Task LoadSubscriptionsAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;
        _allSubscriptions.Clear();

        try
        {
            await foreach (var sub in subscriptionService.GetSubscriptionsAsync(cancellationToken))
            {
                _allSubscriptions.Add(new SubscriptionItem(
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

            if (_allSubscriptions.Count == 0)
            {
                ErrorMessage = "No subscriptions found. Make sure your account has access to at least one Azure subscription.";
            }

            OnPropertyChanged(nameof(FilteredSubscriptions));
        }
        catch (OperationCanceledException)
        {
            // User navigated away
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load subscriptions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ToggleSort(string field)
    {
        if (SortField == field)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortField = field;
            SortAscending = true;
        }
    }

    private List<SubscriptionItem> ApplySearchAndSort()
    {
        IEnumerable<SubscriptionItem> result = _allSubscriptions;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            result = result.Where(s =>
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        result = SortField switch
        {
            "Id" => SortAscending ? result.OrderBy(s => s.Id) : result.OrderByDescending(s => s.Id),
            _ => SortAscending ? result.OrderBy(s => s.Name) : result.OrderByDescending(s => s.Name),
        };

        return result.ToList();
    }
}
