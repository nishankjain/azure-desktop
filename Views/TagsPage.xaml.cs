using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class TagsPage : Page
{
    private CancellationTokenSource? _cts;
    public TagManagerViewModel ViewModel { get; }

    public TagsPage()
    {
        ViewModel = App.GetService<TagManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx)
        {
            var resourceId = GetResourceId(ctx);
            await ViewModel.LoadTagsAsync(resourceId, _cts.Token);
        }
    }

    private static string GetResourceId(NavigationContext ctx)
    {
        if (ctx.Resource is not null)
            return ctx.Resource.ResourceId;
        if (ctx.ResourceGroupName is not null)
            return $"/subscriptions/{ctx.SubscriptionId}/resourceGroups/{ctx.ResourceGroupName}";
        return $"/subscriptions/{ctx.SubscriptionId}";
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

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
