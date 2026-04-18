using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwSslSettingsPage : Page
{
    private const AppGwSection Section = AppGwSection.SslSettings;
    private CancellationTokenSource? _cts;
    private NavigationContext? _navCtx;
    public AppGwViewModel ViewModel { get; }

    public AppGwSslSettingsPage()
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
        // SSL Certificates table
        CertificatesTable.ItemsSource = ViewModel.SslCertificates;
        CertificatesTable.Columns = "Name";
        CertificatesTable.ShowCheckboxes = true;
        CertificatesTable.IsNavigable = false;
        CertificatesTable.EmptyMessage = "No SSL certificates or policies.";
        CertificatesTable.ShowAddButton = AppGwViewModel.GetEditableFields(Section).Count > 0;

        CertificatesTable.ItemClick -= OnCertItemClick;
        CertificatesTable.DeleteClick -= OnCertDeleteClick;
        CertificatesTable.AddClick -= OnCertAddClick;
        CertificatesTable.ItemClick += OnCertItemClick;
        CertificatesTable.DeleteClick += OnCertDeleteClick;
        CertificatesTable.AddClick += OnCertAddClick;

        CertificatesTable.Refresh();

        // SSL Profiles table (conditional)
        if (ViewModel.SslProfiles.Count > 0)
        {
            ProfilesTable.Visibility = Visibility.Visible;
            ProfilesHeader.Visibility = Visibility.Visible;
            ProfilesTable.ItemsSource = ViewModel.SslProfiles;
            ProfilesTable.Columns = "Name, Client Auth";
            ProfilesTable.ShowCheckboxes = true;
            ProfilesTable.IsNavigable = false;
            ProfilesTable.EmptyMessage = "";
            ProfilesTable.ShowAddButton = false;

            ProfilesTable.DeleteClick -= OnProfileDeleteClick;
            ProfilesTable.DeleteClick += OnProfileDeleteClick;

            ProfilesTable.Refresh();
        }
        else
        {
            ProfilesTable.Visibility = Visibility.Collapsed;
            ProfilesHeader.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCertItemClick(object? sender, string name) { }

    private async void OnCertDeleteClick(object? sender, List<string> names)
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

    private async void OnCertAddClick(object? sender, EventArgs e)
    {
        await AppGwDialogHelper.ShowAddDialogAsync(XamlRoot, ViewModel, Section, Render);
    }

    private async void OnProfileDeleteClick(object? sender, List<string> names)
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
