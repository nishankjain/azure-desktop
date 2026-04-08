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

    public TagsPage()
    {
        ViewModel = App.GetService<TagManagerViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (string resourceId, List<string> breadcrumbs, object _))
        {
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
        if (Frame.CanGoBack) Frame.GoBack();
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
