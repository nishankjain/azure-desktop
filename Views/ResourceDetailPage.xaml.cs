using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceDetailPage : Page
{
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

        if (e.Parameter is NavigationContext ctx && ctx.Resource is not null)
        {
            _navCtx = ctx;

            ResourceIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(ctx.Resource.IconPath));

            _ = ViewModel.LoadAsync(ctx.Resource);
        }
    }
}
