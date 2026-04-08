using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class FeatureEntry : ObservableObject
{
    public required string ProviderNamespace { get; init; }
    public required string FeatureName { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }

    [ObservableProperty]
    public partial string State { get; set; } = string.Empty;

    public string FullName => $"{ProviderNamespace}/{FeatureName}";
    public bool IsRegistered => State == "Registered";
}

public partial class FeaturesViewModel(IFeatureService featureService) : ObservableObject
{
    private readonly List<string> _allProviders = [];
    private readonly List<FeatureEntry> _allFeatures = [];
    private string? _subscriptionId;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingFeatures { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? SelectedProvider { get; set; }

    [ObservableProperty]
    public partial string ProviderFilter { get; set; } = string.Empty;

    partial void OnProviderFilterChanged(string value) => RefreshFilteredProviders();

    [ObservableProperty]
    public partial string FeatureFilter { get; set; } = string.Empty;

    partial void OnFeatureFilterChanged(string value) => RefreshFilteredFeatures();

    [ObservableProperty]
    public partial FeatureEntry? SearchResult { get; set; }

    [ObservableProperty]
    public partial string? SearchError { get; set; }

    public ObservableCollection<string> FilteredProviders { get; } = [];
    public ObservableCollection<FeatureEntry> FilteredFeatures { get; } = [];

    public bool HasLoadedFeatures => _allFeatures.Count > 0 || SelectedProvider is not null;

    private void RefreshFilteredProviders()
    {
        FilteredProviders.Clear();
        var filtered = string.IsNullOrWhiteSpace(ProviderFilter)
            ? _allProviders
            : _allProviders.Where(p => p.Contains(ProviderFilter, StringComparison.OrdinalIgnoreCase));
        foreach (var p in filtered)
        {
            FilteredProviders.Add(p);
        }
    }

    private void RefreshFilteredFeatures()
    {
        FilteredFeatures.Clear();
        var filtered = string.IsNullOrWhiteSpace(FeatureFilter)
            ? _allFeatures
            : _allFeatures.Where(f => f.FeatureName.Contains(FeatureFilter, StringComparison.OrdinalIgnoreCase));
        foreach (var f in filtered)
        {
            FilteredFeatures.Add(f);
        }
    }

    public async Task LoadProvidersAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        if (_allProviders.Count > 0)
        {
            return; // Already cached
        }

        _subscriptionId = subscriptionId;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await foreach (var ns in featureService.GetResourceProviderNamespacesAsync(subscriptionId, cancellationToken))
            {
                _allProviders.Add(ns);
            }

            _allProviders.Sort();
            RefreshFilteredProviders();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load providers: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SelectProviderAsync(string providerNamespace)
    {
        if (_subscriptionId is null) return;

        SelectedProvider = providerNamespace;
        IsLoadingFeatures = true;
        ErrorMessage = null;
        SearchResult = null;
        SearchError = null;
        _allFeatures.Clear();
        FilteredFeatures.Clear();
        FeatureFilter = string.Empty;

        try
        {
            await foreach (var f in featureService.GetFeaturesForProviderAsync(_subscriptionId, providerNamespace))
            {
                var entry = new FeatureEntry
                {
                    ProviderNamespace = f.ProviderNamespace,
                    FeatureName = f.FeatureName,
                    State = f.State,
                    ResourceType = f.ResourceType,
                    ResourceId = f.ResourceId,
                };
                _allFeatures.Add(entry);
                FilteredFeatures.Add(entry);
            }

            if (_allFeatures.Count == 0)
            {
                ErrorMessage = $"No preview features found for {providerNamespace}.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load features: {ex.Message}";
        }
        finally
        {
            IsLoadingFeatures = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        if (_subscriptionId is null || string.IsNullOrWhiteSpace(ProviderFilter))
        {
            return;
        }

        // If features are loaded, the feature filter already works locally — nothing to do
        if (_allFeatures.Count > 0 && !string.IsNullOrWhiteSpace(FeatureFilter))
        {
            return;
        }

        // If no feature name specified, select the provider
        if (string.IsNullOrWhiteSpace(FeatureFilter))
        {
            // Check if it's an exact provider match
            var exactProvider = _allProviders.FirstOrDefault(
                p => p.Equals(ProviderFilter.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exactProvider is not null)
            {
                await SelectProviderAsync(exactProvider);
            }
            return;
        }

        // Features not loaded + feature name specified = targeted API lookup
        IsSearching = true;
        SearchError = null;
        SearchResult = null;

        try
        {
            var result = await featureService.LookupFeatureAsync(
                _subscriptionId, ProviderFilter.Trim(), FeatureFilter.Trim(), cancellationToken);

            if (result is not null)
            {
                SearchResult = new FeatureEntry
                {
                    ProviderNamespace = result.ProviderNamespace,
                    FeatureName = result.FeatureName,
                    State = result.State,
                    ResourceType = result.ResourceType,
                    ResourceId = result.ResourceId,
                };
            }
            else
            {
                SearchError = $"Feature '{ProviderFilter.Trim()}/{FeatureFilter.Trim()}' not found.";
            }
        }
        catch (Exception ex)
        {
            SearchError = $"Lookup failed: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }
}
