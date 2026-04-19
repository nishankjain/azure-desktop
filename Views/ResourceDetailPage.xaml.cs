using System.Collections.ObjectModel;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class ResourceDetailPage : NavigablePage
{
    public override string PageLabel => NavCtx?.Resource?.SingularType ?? "Resource";
    protected override bool IsOverviewPage => true;
    public override string? ActiveNavTag
    {
        get
        {
            if (NavCtx?.Resource?.Type.Equals("Microsoft.Network/applicationGateways", StringComparison.OrdinalIgnoreCase) == true)
                return "AppGwOverview";
            return "ResourceDetail";
        }
    }
    public override NavItemDefinition[] GetNavItems()
    {
        if (NavCtx?.Resource?.Type.Equals("Microsoft.Network/applicationGateways", StringComparison.OrdinalIgnoreCase) == true)
            return AppGwNavItems.Get();
        return ResourceNavItems.Get(NavCtx?.Resource?.Type ?? "");
    }

    public ResourceDetailViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    public ResourceDetailPage()
    {
        ViewModel = App.GetService<ResourceDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnContextReady(NavigationContext? ctx)
    {
        if (ctx?.Resource is not null)
        {
            ResourceIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri(ctx.Resource.IconPath));
            _ = ViewModel.LoadAsync(ctx.Resource, Cts!.Token);
        }
    }
}
