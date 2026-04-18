using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceGroupDetailPage : Page
{
    private CancellationTokenSource? _cts;
    public ResourceGroupDetailViewModel ViewModel { get; }

    public ResourceGroupDetailPage()
    {
        ViewModel = App.GetService<ResourceGroupDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx && ctx.ResourceGroupName is not null)
        {
            var rgItem = new ResourceGroupItem(ctx.ResourceGroupName, ctx.ResourceGroupLocation ?? "");
            ViewModel.Load(ctx.SubscriptionId, rgItem);
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
