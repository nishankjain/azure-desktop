using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceProvidersPage : Page
{
    private CancellationTokenSource? _cts;
    public ResourceProvidersViewModel ViewModel { get; }
    private SubscriptionItem? _subItem;

    public ResourceProvidersPage()
    {
        ViewModel = App.GetService<ResourceProvidersViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx)
        {
            _subItem = ctx.Subscription;

            await ViewModel.LoadProvidersAsync(ctx.SubscriptionId, _cts.Token);
        }
    }

    private void Provider_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ResourceProviderEntry entry && _subItem is not null)
        {
            Frame.Navigate(typeof(ResourceProviderDetailPage), new NavigationContext(_subItem, ResourceProvider: entry));
        }
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ResourceProviderEntry entry })
        {
            await ViewModel.RegisterProviderAsync(entry);
        }
    }

    private async void Unregister_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ResourceProviderEntry entry })
        {
            await ViewModel.UnregisterProviderAsync(entry);
        }
    }

    private void SortToggle_Click(object sender, RoutedEventArgs e)
    {
        SortLabel.Text = ViewModel.SortAscending ? "A-Z" : "Z-A";
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
