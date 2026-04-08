using CommunityToolkit.Mvvm.ComponentModel;

namespace AzureDesktop.ViewModels;

public partial class ResourceGroupDetailViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? Location { get; set; }

    [ObservableProperty]
    public partial string? ProvisioningState { get; set; }

    [ObservableProperty]
    public partial string? ResourceId { get; set; }

    public void Load(string subscriptionId, ResourceGroupItem rgItem)
    {
        Name = rgItem.Name;
        Location = rgItem.Location;
        ResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{rgItem.Name}";
    }
}
