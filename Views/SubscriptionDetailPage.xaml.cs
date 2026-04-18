using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class SubscriptionDetailPage : Page
{
    private CancellationTokenSource? _cts;
    public SubscriptionDetailViewModel ViewModel { get; }

    public SubscriptionDetailPage()
    {
        ViewModel = App.GetService<SubscriptionDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx)
        {
            ViewModel.Subscription = ctx.Subscription;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
