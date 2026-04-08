using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }

    private void Subscriptions_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SubscriptionsPage));
    }

    private void ApplicationGateway_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ApplicationGatewayPage));
    }
}
