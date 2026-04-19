using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwBackendPoolsPage : AppGwPageBase
{
    public override string PageLabel => "Backend Pools";
    public override string? ActiveNavTag => "AppGwBackendPools";

    public AppGwBackendPoolsPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        SectionTable.ItemsSource = ViewModel.BackendPools;
        SectionTable.Columns = "Name, Targets, Associated Rules";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = true;
        SectionTable.EmptyMessage = "No backend pools configured.";
        SectionTable.ShowAddButton = AppGwViewModel.GetBackendPoolFields().Count > 0;

        SectionTable.ItemClick -= OnItemClick;
        SectionTable.DeleteClick -= OnDeleteClick;
        SectionTable.AddClick -= OnAddClick;
        SectionTable.ItemClick += OnItemClick;
        SectionTable.DeleteClick += OnDeleteClick;
        SectionTable.AddClick += OnAddClick;

        SectionTable.Refresh();
    }

    private void OnItemClick(object? sender, string name)
    {
        if (NavCtx is null) return;
        Frame.Navigate(typeof(AppGwBackendPoolDetailPage), NavCtx with { DetailItemName = name });
    }

    private async void OnDeleteClick(object? sender, List<string> names)
    {
        var deleted = false;
        foreach (var name in names)
        {
            if (ViewModel.DeleteBackendPool(name)) deleted = true;
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
            AppGwViewModel.GetBackendPoolFields(),
            ViewModel.AddBackendPool,
            async desc => { await ViewModel.SaveChangesAsync(desc); },
            OnDataLoaded);
    }
}
