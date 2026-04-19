using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwFrontendIPPage : AppGwPageBase
{
    public override string PageLabel => "Frontend IP";
    public override string? ActiveNavTag => "AppGwFrontendIP";

    public AppGwFrontendIPPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        // Frontend IP Configurations table
        FrontendIPTable.ItemsSource = ViewModel.FrontendIPs;
        FrontendIPTable.Columns = "Name, Private IP, Allocation Method, Public IP";
        FrontendIPTable.ShowCheckboxes = true;
        FrontendIPTable.IsNavigable = false;
        FrontendIPTable.EmptyMessage = "No frontend IPs configured.";
        FrontendIPTable.ShowAddButton = AppGwViewModel.GetFrontendPortFields().Count > 0;

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
        // Frontend IPs cannot be deleted — no-op
    }

    private async void OnIPAddClick(object? sender, EventArgs e)
    {
        await AppGwDialogHelper.ShowAddDialogAsync(
            XamlRoot,
            AppGwViewModel.GetFrontendPortFields(),
            ViewModel.AddFrontendPort,
            async desc => { await ViewModel.SaveChangesAsync(desc); },
            OnDataLoaded);
    }

    private async void OnPortDeleteClick(object? sender, List<string> names)
    {
        var deleted = false;
        foreach (var name in names)
        {
            if (ViewModel.DeleteFrontendPort(name)) deleted = true;
        }
        if (deleted)
        {
            var desc = names.Count == 1 ? $"Deleted '{names[0]}'." : $"Deleted {names.Count} items.";
            try { await ViewModel.SaveChangesAsync(desc); } catch { }
            OnDataLoaded();
        }
    }
}
