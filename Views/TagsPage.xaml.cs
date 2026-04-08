using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class TagsPage : Page
{
    public TagManagerViewModel ViewModel { get; }
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private object? _parentNavParam;

    public TagsPage()
    {
        ViewModel = App.GetService<TagManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (string resourceId, List<string> breadcrumbs, object navParam))
        {
            _parentNavParam = navParam;
            BreadcrumbItems.Clear();
            foreach (var b in breadcrumbs)
            {
                BreadcrumbItems.Add(b);
            }

            BreadcrumbItems.Add("Tags");
            Breadcrumb.ItemsSource = BreadcrumbItems;

            await ViewModel.LoadTagsAsync(resourceId);
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        NavigateToParent();
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

    private void EditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.BeginEdit();
    }

    private void CancelEditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.CancelEdit();
    }

    private async void SaveEditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
            await ViewModel.SaveEditCommand.ExecuteAsync(tag);
    }

    private void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.BeginDelete();
    }

    private void CancelDeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag }) tag.CancelDelete();
    }

    private async void ConfirmDeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
            await ViewModel.ConfirmDeleteCommand.ExecuteAsync(tag);
    }
}
