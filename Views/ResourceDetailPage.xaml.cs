using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceDetailPage : Page
{
    private CancellationTokenSource? _cts;
    public ResourceDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private NavigationContext? _navCtx;

    public ResourceDetailPage()
    {
        ViewModel = App.GetService<ResourceDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx && ctx.Resource is not null)
        {
            _navCtx = ctx;

            ResourceIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(ctx.Resource.IconPath));

            _ = ViewModel.LoadAsync(ctx.Resource, _cts.Token);
        }
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
