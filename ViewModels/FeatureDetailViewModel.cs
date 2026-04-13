using CommunityToolkit.Mvvm.ComponentModel;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class FeatureDetailViewModel(IFeatureService featureService, OperationManager operationManager) : ObservableObject
{
    [ObservableProperty]
    public partial string FeatureName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProviderNamespace { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial string State { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResourceType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResourceId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SuccessMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial bool IsToggling { get; set; }

    public string ToggleButtonText => State is "Registered" ? "Unregister" : "Register";

    public bool CanToggle => !IsToggling;

    public Uri? DocsUri => BuildDocsUri();

    public void Load(FeatureEntry entry, string subscriptionId)
    {
        FeatureName = entry.FeatureName;
        ProviderNamespace = entry.ProviderNamespace;
        State = entry.State;
        ResourceType = entry.ResourceType;
        ResourceId = entry.ResourceId;
        FullName = entry.FullName;
        SuccessMessage = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(DocsUri));
    }

    public async Task ToggleRegistrationAsync(string subscriptionId)
    {
        IsToggling = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            if (State == "Registered")
            {
                var op = operationManager.Begin("Unregister Feature", "Unregistering", "Unregistered", FeatureName, "Feature", ResourceId);
                await featureService.UnregisterAsync(subscriptionId, ProviderNamespace, FeatureName);
                State = "NotRegistered";
                SuccessMessage = $"Feature '{FeatureName}' unregistered.";
                op.Complete();
            }
            else
            {
                var op = operationManager.Begin("Register Feature", "Registering", "Registered", FeatureName, "Feature", ResourceId);
                await featureService.RegisterAsync(subscriptionId, ProviderNamespace, FeatureName);
                State = "Registered";
                SuccessMessage = $"Feature '{FeatureName}' registered.";
                op.Complete();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to toggle feature: {ex.Message}";
        }
        finally
        {
            IsToggling = false;
        }
    }

    private Uri? BuildDocsUri()
    {
        if (string.IsNullOrEmpty(ProviderNamespace)) return null;
        var provider = ProviderNamespace.Replace("Microsoft.", "").ToLowerInvariant();
        return new Uri($"https://learn.microsoft.com/en-us/azure/{provider}/");
    }
}
