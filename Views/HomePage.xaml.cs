using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class HomePage : Page
{
    private CancellationTokenSource? _cts;
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await ViewModel.TryRestoreAsync();
    }

    private void Subscriptions_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SubscriptionsPage));
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
