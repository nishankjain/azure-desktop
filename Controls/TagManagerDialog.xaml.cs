using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Controls;

public sealed partial class TagManagerDialog : ContentDialog
{
    public TagManagerViewModel ViewModel { get; }

    public TagManagerDialog()
    {
        ViewModel = App.GetService<TagManagerViewModel>();
        InitializeComponent();
    }

    public static async Task ShowAsync(string resourceId, XamlRoot xamlRoot)
    {
        var dialog = new TagManagerDialog
        {
            XamlRoot = xamlRoot
        };
        await dialog.ViewModel.LoadTagsAsync(resourceId);
        await dialog.ShowAsync();
    }

    private void EditTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
        {
            tag.BeginEdit();
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
        {
            tag.CancelEdit();
        }
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
        {
            await ViewModel.SaveEditCommand.ExecuteAsync(tag);
        }
    }

    private void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
        {
            tag.BeginDelete();
        }
    }

    private void CancelDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
        {
            tag.CancelDelete();
        }
    }

    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TagEntry tag })
        {
            await ViewModel.ConfirmDeleteCommand.ExecuteAsync(tag);
        }
    }
}
