using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwRewriteSetsPage : AppGwPageBase
{
    public override string PageLabel => "Rewrite Sets";
    public override string? ActiveNavTag => "AppGwRewriteSets";

    public AppGwRewriteSetsPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        SectionTable.ItemsSource = ViewModel.RewriteSets;
        SectionTable.Columns = "Name, Rule Count";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = false;
        SectionTable.EmptyMessage = "No rewrite sets configured.";
        SectionTable.ShowAddButton = AppGwViewModel.GetRewriteSetFields().Count > 0;

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
            if (ViewModel.DeleteRewriteSet(name)) deleted = true;
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
        await AppGwDialogHelper.ShowAddDialogAsync(
            XamlRoot,
            AppGwViewModel.GetRewriteSetFields(),
            _ => false,
            async desc => { await ViewModel.SaveChangesAsync(desc); },
            OnDataLoaded);
    }
}
