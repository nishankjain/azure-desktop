using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwListenersPage : AppGwPageBase
{
    public override string PageLabel => "Listeners";
    public override string? ActiveNavTag => "AppGwListeners";

    public AppGwListenersPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        SectionTable.ItemsSource = ViewModel.HttpListeners;
        SectionTable.Columns = "Name, Protocol, Host Name, Frontend Port, Require SNI";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = false;
        SectionTable.EmptyMessage = "No listeners configured.";
        SectionTable.ShowAddButton = AppGwViewModel.GetListenerFields().Count > 0;

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
            if (ViewModel.DeleteListener(name)) deleted = true;
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
            AppGwViewModel.GetListenerFields(),
            ViewModel.AddListener,
            async desc => { await ViewModel.SaveChangesAsync(desc); },
            OnDataLoaded);
    }
}
