using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class ResourceProviderEntry : ObservableObject
{
    public required string Namespace { get; init; }
    public required string RegistrationPolicy { get; init; }
    public required IReadOnlyList<ResourceTypeInfo> ResourceTypes { get; init; }

    [ObservableProperty]
    public partial string RegistrationState { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsUpdating { get; set; }

    public bool IsRegistered => RegistrationState == "Registered";
    public int ResourceTypeCount => ResourceTypes.Count;
}

public partial class ResourceProvidersViewModel(IResourceProviderService resourceProviderService) : ObservableObject
{
    private readonly List<ResourceProviderEntry> _allProviders = [];
    private string? _subscriptionId;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value) => RefreshFilteredProviders();

    [ObservableProperty]
    public partial string StatusFilter { get; set; } = "All";

    partial void OnStatusFilterChanged(string value) => RefreshFilteredProviders();

    [ObservableProperty]
    public partial string PolicyFilter { get; set; } = "All";

    partial void OnPolicyFilterChanged(string value) => RefreshFilteredProviders();

    [ObservableProperty]
    public partial bool SortAscending { get; set; } = true;

    partial void OnSortAscendingChanged(bool value) => RefreshFilteredProviders();

    public ObservableCollection<ResourceProviderEntry> FilteredProviders { get; } = [];

    private void RefreshFilteredProviders()
    {
        FilteredProviders.Clear();

        var filtered = _allProviders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(p =>
                p.Namespace.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (StatusFilter != "All")
        {
            filtered = filtered.Where(p =>
                p.RegistrationState.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (PolicyFilter != "All")
        {
            filtered = filtered.Where(p =>
                p.RegistrationPolicy.Equals(PolicyFilter, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = SortAscending
            ? filtered.OrderBy(p => p.Namespace, StringComparer.OrdinalIgnoreCase)
            : filtered.OrderByDescending(p => p.Namespace, StringComparer.OrdinalIgnoreCase);

        foreach (var p in sorted)
        {
            FilteredProviders.Add(p);
        }
    }

    public async Task LoadProvidersAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        _subscriptionId = subscriptionId;
        _allProviders.Clear();
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await foreach (var provider in resourceProviderService
                .GetResourceProvidersAsync(subscriptionId, cancellationToken))
            {
                _allProviders.Add(new ResourceProviderEntry
                {
                    Namespace = provider.Namespace,
                    RegistrationState = provider.RegistrationState,
                    RegistrationPolicy = provider.RegistrationPolicy,
                    ResourceTypes = provider.ResourceTypes,
                });
            }

            _allProviders.Sort((a, b) => string.Compare(a.Namespace, b.Namespace, StringComparison.OrdinalIgnoreCase));
            RefreshFilteredProviders();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load resource providers: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RegisterProviderAsync(ResourceProviderEntry entry, CancellationToken cancellationToken = default)
    {
        if (_subscriptionId is null)
        {
            return;
        }

        entry.IsUpdating = true;

        try
        {
            await resourceProviderService.RegisterAsync(_subscriptionId, entry.Namespace, cancellationToken);
            entry.RegistrationState = "Registered";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to register {entry.Namespace}: {ex.Message}";
        }
        finally
        {
            entry.IsUpdating = false;
        }
    }

    public async Task UnregisterProviderAsync(ResourceProviderEntry entry, CancellationToken cancellationToken = default)
    {
        if (_subscriptionId is null)
        {
            return;
        }

        entry.IsUpdating = true;

        try
        {
            await resourceProviderService.UnregisterAsync(_subscriptionId, entry.Namespace, cancellationToken);
            entry.RegistrationState = "NotRegistered";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to unregister {entry.Namespace}: {ex.Message}";
        }
        finally
        {
            entry.IsUpdating = false;
        }
    }
}
