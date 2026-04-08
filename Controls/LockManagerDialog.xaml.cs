using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Controls;

public sealed partial class LockManagerDialog : ContentDialog
{
    public LockManagerViewModel ViewModel { get; }

    public LockManagerDialog()
    {
        ViewModel = App.GetService<LockManagerViewModel>();
        InitializeComponent();
    }

    public static async Task ShowAsync(string resourceId, XamlRoot xamlRoot)
    {
        var dialog = new LockManagerDialog
        {
            XamlRoot = xamlRoot
        };
        await dialog.ViewModel.LoadLocksAsync(resourceId);
        await dialog.ShowAsync();
    }

    private void DeleteLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
        {
            entry.BeginDelete();
        }
    }

    private void CancelDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
        {
            entry.CancelDelete();
        }
    }

    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
        {
            await ViewModel.ConfirmDeleteCommand.ExecuteAsync(entry);
        }
    }

    private void EditLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
        {
            entry.BeginEdit();
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
        {
            entry.CancelEdit();
        }
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockEntry entry })
        {
            await ViewModel.SaveEditCommand.ExecuteAsync(entry);
        }
    }
}
