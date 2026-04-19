using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Views;

public sealed partial class AppGwSslSettingsPage : AppGwPageBase
{
    public override string PageLabel => "SSL Settings";
    public override string? ActiveNavTag => "AppGwSsl";

    public AppGwSslSettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        // SSL Certificates table
        CertificatesTable.ItemsSource = ViewModel.SslCertificates;
        CertificatesTable.Columns = "Name";
        CertificatesTable.ShowCheckboxes = true;
        CertificatesTable.IsNavigable = false;
        CertificatesTable.EmptyMessage = "No SSL certificates or policies.";
        CertificatesTable.ShowAddButton = AppGwViewModel.GetSslCertificateFields().Count > 0;

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
            if (ViewModel.DeleteSslCertificate(name)) deleted = true;
        }
        if (deleted)
        {
            var desc = names.Count == 1 ? $"Deleted '{names[0]}'." : $"Deleted {names.Count} items.";
            try { await ViewModel.SaveChangesAsync(desc); } catch { }
            OnDataLoaded();
        }
    }

    private async void OnCertAddClick(object? sender, EventArgs e)
    {
        await AppGwDialogHelper.ShowAddDialogAsync(
            XamlRoot,
            AppGwViewModel.GetSslCertificateFields(),
            _ => false,
            async desc => { await ViewModel.SaveChangesAsync(desc); },
            OnDataLoaded);
    }

    private async void OnProfileDeleteClick(object? sender, List<string> names)
    {
        var deleted = false;
        foreach (var name in names)
        {
            if (ViewModel.DeleteSslCertificate(name)) deleted = true;
        }
        if (deleted)
        {
            var desc = names.Count == 1 ? $"Deleted '{names[0]}'." : $"Deleted {names.Count} items.";
            try { await ViewModel.SaveChangesAsync(desc); } catch { }
            OnDataLoaded();
        }
    }
}
