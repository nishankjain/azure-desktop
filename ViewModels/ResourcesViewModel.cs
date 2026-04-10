using System.Collections.ObjectModel;
using Azure.ResourceManager;
using CommunityToolkit.Mvvm.ComponentModel;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

using AzureDesktop.Helpers;

public partial class ResourceItem(string name, string type, string location, string resourceId)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public string Location { get; } = location;
    public string ResourceId { get; } = resourceId;
    public string DisplayType { get; } = HumanizeResourceType(type);
    public string IconPath { get; } = ResourceIconResolver.GetIconPath(type);

    private static string HumanizeResourceType(string type)
    {
        // Remove namespace (e.g. "Microsoft.Network/applicationGateways" -> "applicationGateways")
        var slashIndex = type.LastIndexOf('/');
        var shortName = slashIndex >= 0 ? type[(slashIndex + 1)..] : type;

        // Split camelCase into words and capitalize
        var chars = new System.Text.StringBuilder(shortName.Length + 8);
        for (var i = 0; i < shortName.Length; i++)
        {
            if (i > 0 && char.IsUpper(shortName[i]) && !char.IsUpper(shortName[i - 1]))
            {
                chars.Append(' ');
            }

            chars.Append(i == 0 ? char.ToUpper(shortName[i]) : shortName[i]);
        }

        return chars.ToString();
    }
}

public sealed class ResourceTypeGroup(string typeName, string displayName, IReadOnlyList<ResourceItem> resources)
{
    public string TypeName { get; } = typeName;
    public string DisplayName { get; } = displayName;
    public int Count { get; } = resources.Count;
    public IReadOnlyList<ResourceItem> Resources { get; } = resources;
}

public partial class ResourcesViewModel(IAzureAuthService authService) : ObservableObject
{
    private readonly List<ResourceItem> _allResources = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? ResourceGroupName { get; set; }

    [ObservableProperty]
    public partial string? SubscriptionName { get; set; }

    [ObservableProperty]
    public partial string? SubscriptionId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupedResources))]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupedResources))]
    [NotifyPropertyChangedFor(nameof(SortIndicator))]
    public partial string SortField { get; set; } = "Name";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupedResources))]
    [NotifyPropertyChangedFor(nameof(SortIndicator))]
    public partial bool SortAscending { get; set; } = true;

    private readonly Dictionary<string, string> _typeDisplayToRaw = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<string> TypeFilters { get; } = [];

    public ObservableCollection<string> LocationFilters { get; } = [];

    public HashSet<string> SelectedTypes { get; } = [];

    public HashSet<string> SelectedLocations { get; } = [];

    public string SortIndicator => $"{SortField} {(SortAscending ? "↑" : "↓")}";

    public IReadOnlyList<ResourceTypeGroup> GroupedResources => ApplyFilterSortAndGroup();

    public async Task LoadForResourceGroupAsync(
        string subscriptionId, string subscriptionName, string resourceGroupName,
        CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;
        _allResources.Clear();
        TypeFilters.Clear();
        LocationFilters.Clear();
        SelectedTypes.Clear();
        SelectedLocations.Clear();
        SearchText = null;
        ResourceGroupName = resourceGroupName;
        SubscriptionName = subscriptionName;
        SubscriptionId = subscriptionId;

        try
        {
            var client = new ArmClient(authService.Credential);
            var subscription = client.GetSubscriptionResource(
                new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            var rg = (await subscription.GetResourceGroups()
                .GetAsync(resourceGroupName, cancellationToken)).Value;

            var types = new HashSet<string>();
            var locations = new HashSet<string>();

            await foreach (var resource in rg.GetGenericResourcesAsync(cancellationToken: cancellationToken))
            {
                var item = new ResourceItem(
                    resource.Data.Name,
                    resource.Data.ResourceType.ToString(),
                    resource.Data.Location.DisplayName ?? "",
                    resource.Data.Id?.ToString() ?? "");
                _allResources.Add(item);
                types.Add(item.Type);
                locations.Add(item.Location);
            }

            foreach (var t in types.OrderBy(t => t))
            {
                TypeFilters.Add(new ResourceItem(string.Empty, t, string.Empty, string.Empty).DisplayType);
            }

            // Build mapping for filter lookup
            _typeDisplayToRaw.Clear();
            foreach (var t in types)
            {
                var display = new ResourceItem(string.Empty, t, string.Empty, string.Empty).DisplayType;
                _typeDisplayToRaw[display] = t;
            }

            foreach (var l in locations.OrderBy(l => l))
            {
                LocationFilters.Add(l);
            }

            if (_allResources.Count == 0)
            {
                ErrorMessage = "No resources found in this resource group.";
            }

            OnPropertyChanged(nameof(GroupedResources));
        }
        catch (OperationCanceledException)
        {
            // User navigated away
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load resources: {ex.Message}";
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
        OnPropertyChanged(nameof(GroupedResources));
    }

    public void ClearFilters()
    {
        SearchText = null;
        SelectedTypes.Clear();
        SelectedLocations.Clear();
        OnPropertyChanged(nameof(GroupedResources));
    }

    private List<ResourceTypeGroup> ApplyFilterSortAndGroup()
    {
        IEnumerable<ResourceItem> result = _allResources;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            result = result.Where(r => r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedTypes.Count > 0)
        {
            var rawTypes = SelectedTypes
                .Select(dt => _typeDisplayToRaw.GetValueOrDefault(dt, dt))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            result = result.Where(r => rawTypes.Contains(r.Type));
        }

        if (SelectedLocations.Count > 0)
        {
            result = result.Where(r => SelectedLocations.Contains(r.Location));
        }

        result = SortField switch
        {
            "Type" => SortAscending ? result.OrderBy(r => r.Type).ThenBy(r => r.Name) : result.OrderByDescending(r => r.Type).ThenBy(r => r.Name),
            "Location" => SortAscending ? result.OrderBy(r => r.Location).ThenBy(r => r.Name) : result.OrderByDescending(r => r.Location).ThenBy(r => r.Name),
            _ => SortAscending ? result.OrderBy(r => r.Name) : result.OrderByDescending(r => r.Name),
        };

        var groups = result
            .GroupBy(r => r.Type)
            .Select(g => new ResourceTypeGroup(g.Key, g.First().DisplayType, g.ToList()));

        groups = SortField == "Type"
            ? (SortAscending ? groups.OrderBy(g => g.DisplayName) : groups.OrderByDescending(g => g.DisplayName))
            : groups.OrderBy(g => g.DisplayName);

        return groups.ToList();
    }
}
