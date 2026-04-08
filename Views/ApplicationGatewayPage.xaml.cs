using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class ApplicationGatewayPage : Page
{
    public ApplicationGatewayViewModel ViewModel { get; }

    public ApplicationGatewayPage()
    {
        ViewModel = App.GetService<ApplicationGatewayViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Subscriptions.Count == 0)
        {
            await ViewModel.LoadSubscriptionsCommand.ExecuteAsync(null);
        }
    }
}
