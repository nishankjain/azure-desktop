using CommunityToolkit.Mvvm.ComponentModel;

namespace AzureDesktop.ViewModels;

public partial class SubscriptionDetailViewModel : ObservableObject
{
    [ObservableProperty]
    public partial SubscriptionItem? Subscription { get; set; }
}
