using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwBackendSettingsPage : Page
{
    private const AppGwSection Section = AppGwSection.BackendSettings;
    private CancellationTokenSource? _cts;
    private NavigationContext? _navCtx;
    public AppGwViewModel ViewModel { get; }

    public AppGwBackendSettingsPage()
    {
        ViewModel = App.GetService<AppGwViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (e.Parameter is NavigationContext ctx)
        {
            _navCtx = ctx;
            if (ctx.Resource is not null)
            {
                await ViewModel.LoadAsync(ctx.Resource.ResourceId, _cts.Token);
            }
            Render();
        }
    }

    private void Render()
    {
        SectionTable.ItemsSource = ViewModel.BackendSettings;
        SectionTable.Columns = "Name, Protocol, Port, Cookie Affinity, Request Timeout, Host Name";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = false;
        SectionTable.EmptyMessage = "No backend settings configured.";
        SectionTable.ShowAddButton = AppGwViewModel.GetEditableFields(Section).Count > 0;

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
            if (ViewModel.DeleteItem(Section, name)) deleted = true;
        }
        if (deleted)
        {
            var desc = names.Count == 1 ? $"Deleted '{names[0]}'." : $"Deleted {names.Count} items.";
            try { await ViewModel.SaveChangesAsync(desc); } catch { }
            Render();
        }
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        await AppGwDialogHelper.ShowAddDialogAsync(XamlRoot, ViewModel, Section, Render);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
