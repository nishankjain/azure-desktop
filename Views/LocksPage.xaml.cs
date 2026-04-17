using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class LocksPage : Page
{
    public LockManagerViewModel ViewModel { get; }

    public LocksPage()
    {
        ViewModel = App.GetService<LockManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is NavigationContext ctx)
        {
            var resourceId = GetResourceId(ctx);
            await ViewModel.LoadLocksAsync(resourceId);
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
