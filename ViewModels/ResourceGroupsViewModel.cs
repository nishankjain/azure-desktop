using System.Collections.ObjectModel;
using Azure.ResourceManager;
using CommunityToolkit.Mvvm.ComponentModel;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class ResourceGroupItem(string name, string location)
{
    public string Name { get; } = name;
    public string Location { get; } = location;
}

public partial class ResourceGroupsViewModel(IAzureAuthService authService) : ObservableObject
{
    private readonly List<ResourceGroupItem> _allResourceGroups = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? SubscriptionName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredResourceGroups))]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredResourceGroups))]
    public partial string SortField { get; set; } = "Name";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredResourceGroups))]
    public partial bool SortAscending { get; set; } = true;

    public ObservableCollection<string> LocationFilters { get; } = [];

    public HashSet<string> SelectedLocations { get; } = [];

    public IReadOnlyList<ResourceGroupItem> FilteredResourceGroups => ApplySearchSortAndFilter();

    public async Task LoadForSubscriptionAsync(SubscriptionItem subItem, CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;
        _allResourceGroups.Clear();
        LocationFilters.Clear();
        SelectedLocations.Clear();
        SearchText = null;
        SubscriptionName = subItem.Name;

        try
        {
            var client = authService.Client;
            var subscription = client.GetSubscriptionResource(
                new Azure.Core.ResourceIdentifier($"/subscriptions/{subItem.Id}"));

            var locations = new HashSet<string>();

            await foreach (var rg in subscription.GetResourceGroups().GetAllAsync(cancellationToken: cancellationToken))
            {
                var item = new ResourceGroupItem(
                    rg.Data.Name,
                    rg.Data.Location.DisplayName ?? "");
                _allResourceGroups.Add(item);
                locations.Add(item.Location);
            }

            foreach (var l in locations.OrderBy(l => l))
            {
                LocationFilters.Add(l);
            }

            if (_allResourceGroups.Count == 0)
            {
                ErrorMessage = "No resource groups found in this subscription.";
            }

            OnPropertyChanged(nameof(FilteredResourceGroups));
        }
        catch (OperationCanceledException)
        {
            // User navigated away
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load resource groups: {ex.Message}";
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

    public void OnFilterChanged()
    {
        OnPropertyChanged(nameof(FilteredResourceGroups));
    }

    public void ClearFilters()
    {
        SearchText = null;
        SelectedLocations.Clear();
        OnPropertyChanged(nameof(FilteredResourceGroups));
    }

    private List<ResourceGroupItem> ApplySearchSortAndFilter()
    {
        IEnumerable<ResourceGroupItem> result = _allResourceGroups;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            result = result.Where(r => r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedLocations.Count > 0)
        {
            result = result.Where(r => SelectedLocations.Contains(r.Location));
        }

        result = SortAscending ? result.OrderBy(r => r.Name) : result.OrderByDescending(r => r.Name);

        return result.ToList();
    }
}
