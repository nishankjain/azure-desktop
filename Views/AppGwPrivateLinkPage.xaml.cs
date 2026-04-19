using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwPrivateLinkPage : AppGwPageBase
{
    public override string PageLabel => "Private Link";
    public override string? ActiveNavTag => "AppGwPrivateLink";

    public AppGwPrivateLinkPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        SectionTable.ItemsSource = ViewModel.PrivateLinks;
        SectionTable.Columns = "Name, IP Configurations";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = false;
        SectionTable.EmptyMessage = "No private link configurations.";
        SectionTable.ShowAddButton = false;

        SectionTable.ItemClick -= OnItemClick;
        SectionTable.DeleteClick -= OnDeleteClick;
        SectionTable.AddClick -= OnAddClick;
        SectionTable.ItemClick += OnItemClick;
        SectionTable.DeleteClick += OnDeleteClick;
        SectionTable.AddClick += OnAddClick;

        SectionTable.Refresh();
    }

    private void OnItemClick(object? sender, string name) { }

    private async void OnDeleteClick(object? sender, List<string> names)
    {
        var deleted = false;
        foreach (var name in names)
        {
            if (ViewModel.DeletePrivateLink(name)) deleted = true;
        }
        if (deleted)
        {
            var desc = names.Count == 1 ? $"Deleted '{names[0]}'." : $"Deleted {names.Count} items.";
            try { await ViewModel.SaveChangesAsync(desc); } catch { }
            OnDataLoaded();
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        // No add support for private links
    }
}
