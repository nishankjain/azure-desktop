using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class FeatureDetailPage : Page
{
    public FeatureDetailViewModel ViewModel { get; }
    private SubscriptionItem? _subItem;

    public FeatureDetailPage()
    {
        ViewModel = App.GetService<FeatureDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx && ctx.Feature is not null)
        {
            _subItem = ctx.Subscription;
            ViewModel.Load(ctx.Feature, ctx.SubscriptionId);
        }
    }

    private async void ToggleRegistration_Click(object sender, RoutedEventArgs e)
    {
        if (_subItem is not null)
        {
            await ViewModel.ToggleRegistrationAsync(_subItem.Id);
        }
    }
}
