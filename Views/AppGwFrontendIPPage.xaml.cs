using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwFrontendIPPage : Page
{
    private const AppGwSection Section = AppGwSection.FrontendIP;
    private CancellationTokenSource? _cts;
    private NavigationContext? _navCtx;
    public AppGwViewModel ViewModel { get; }

    public AppGwFrontendIPPage()
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
        // Frontend IP Configurations table
        FrontendIPTable.ItemsSource = ViewModel.FrontendIPs;
        FrontendIPTable.Columns = "Name, Private IP, Allocation Method, Public IP";
        FrontendIPTable.ShowCheckboxes = true;
        FrontendIPTable.IsNavigable = false;
        FrontendIPTable.EmptyMessage = "No frontend IPs configured.";
        FrontendIPTable.ShowAddButton = AppGwViewModel.GetEditableFields(Section).Count > 0;

        FrontendIPTable.ItemClick -= OnIPItemClick;
        FrontendIPTable.DeleteClick -= OnIPDeleteClick;
        FrontendIPTable.AddClick -= OnIPAddClick;
        FrontendIPTable.ItemClick += OnIPItemClick;
        FrontendIPTable.DeleteClick += OnIPDeleteClick;
        FrontendIPTable.AddClick += OnIPAddClick;

        FrontendIPTable.Refresh();

        // Frontend Ports table
        FrontendPortsTable.ItemsSource = ViewModel.FrontendPorts;
        FrontendPortsTable.Columns = "Name, Port";
        FrontendPortsTable.ShowCheckboxes = true;
        FrontendPortsTable.IsNavigable = false;
        FrontendPortsTable.EmptyMessage = "No frontend ports configured.";
        FrontendPortsTable.ShowAddButton = false;

        FrontendPortsTable.DeleteClick -= OnPortDeleteClick;
        FrontendPortsTable.DeleteClick += OnPortDeleteClick;

        FrontendPortsTable.Refresh();
    }

    private void OnIPItemClick(object? sender, string name) { }

    private async void OnIPDeleteClick(object? sender, List<string> names)
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

    private async void OnIPAddClick(object? sender, EventArgs e)
    {
        await AppGwDialogHelper.ShowAddDialogAsync(XamlRoot, ViewModel, Section, Render);
    }

    private async void OnPortDeleteClick(object? sender, List<string> names)
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

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnNavigatedFrom(e);
    }
}
