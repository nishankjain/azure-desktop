using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class FeatureDetailPage : Page
{
    public FeatureDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private SubscriptionItem? _subItem;

    public FeatureDetailPage()
    {
        ViewModel = App.GetService<FeatureDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (FeatureEntry entry, SubscriptionItem sub))
        {
            _subItem = sub;
            ViewModel.Load(entry, sub.Id);

            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(sub.Name);
            BreadcrumbItems.Add("Preview Features");
            BreadcrumbItems.Add(entry.FeatureName);
            Breadcrumb.ItemsSource = BreadcrumbItems;
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        switch (args.Index)
        {
            case 0:
                Frame.BackStack.Clear();
                Frame.Navigate(typeof(SubscriptionsPage));
                break;
            case 1 when _subItem is not null:
                Frame.Navigate(typeof(SubscriptionDetailPage), _subItem);
                break;
            case 2 when _subItem is not null:
                Frame.Navigate(typeof(FeaturesPage), _subItem);
                break;
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
