using System.Collections.ObjectModel;
using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class LocksPage : Page
{
    public LockManagerViewModel ViewModel { get; }

    private object? _parentNavParam;
    private BreadcrumbHelper? _breadcrumbHelper;

    public LocksPage()
    {
        ViewModel = App.GetService<LockManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (string resourceId, List<string> breadcrumbs, object navParam))
        {
            _parentNavParam = navParam;

            _breadcrumbHelper = new BreadcrumbHelper(Breadcrumb, EllipsisButton);
            foreach (var b in breadcrumbs)
            {
                _breadcrumbHelper.Add(b, NavigateToParent);
            }

            _breadcrumbHelper.Add("Locks", () => { });
            _breadcrumbHelper.Apply();

            await ViewModel.LoadLocksAsync(resourceId);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _breadcrumbHelper?.HandleClick(args.Index);
    }

    private void NavigateToParent()
    {
        if (_parentNavParam is NavigationContext ctx)
        {
            if (ctx.Resource is not null)
                Frame.Navigate(typeof(ResourceDetailPage), ctx);
            else if (ctx.ResourceGroupName is not null)
                Frame.Navigate(typeof(ResourceGroupDetailPage), ctx);
            else
                Frame.Navigate(typeof(SubscriptionDetailPage), ctx.Subscription);
        }
        else if (_parentNavParam is SubscriptionItem sub)
        {
            Frame.Navigate(typeof(SubscriptionDetailPage), sub);
        }
        else if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void EditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.BeginEdit();
    }

    private void CancelEditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.CancelEdit();
    }

    private async void SaveEditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
            await ViewModel.SaveEditCommand.ExecuteAsync(entry);
    }

    private void DeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.BeginDelete();
    }

    private void CancelDeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry }) entry.CancelDelete();
    }

    private async void ConfirmDeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
            await ViewModel.ConfirmDeleteCommand.ExecuteAsync(entry);
    }
}
