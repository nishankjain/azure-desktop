using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class FeatureDetailPage : NavigablePage
{
    public override string PageLabel => "Feature";
    public override string? ActiveNavTag => "PreviewFeatures";
    public override NavItemDefinition[] GetNavItems() => SubscriptionNavItems.Get();

    public FeatureDetailViewModel ViewModel { get; }
    private SubscriptionItem? _subItem;

    public FeatureDetailPage()
    {
        ViewModel = App.GetService<FeatureDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnContextReady(NavigationContext? ctx)
    {
        if (ctx?.Feature is not null)
        {
            _subItem = ctx.Subscription;
            ViewModel.Load(ctx.Feature, ctx.SubscriptionId);
        }
    }

    public override BreadcrumbEntry[] GetBreadcrumbs()
    {
        var baseCrumbs = base.GetBreadcrumbs();
        var list = baseCrumbs.ToList();
        list.Insert(list.Count - 1, new("Preview Features", typeof(FeaturesPage), NavCtx with { Feature = null }));
        return list.ToArray();
    }

    private async void ToggleRegistration_Click(object sender, RoutedEventArgs e)
    {
        if (_subItem is not null)
        {
            await ViewModel.ToggleRegistrationAsync(_subItem.Id);
        }
    }
}
