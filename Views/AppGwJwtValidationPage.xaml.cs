using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwJwtValidationPage : AppGwPageBase
{
    public override string PageLabel => "JWT Validation";
    public override string? ActiveNavTag => "AppGwJwt";

    public AppGwJwtValidationPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        SectionTable.ItemsSource = ViewModel.JwtConfigs;
        SectionTable.Columns = "Name";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = false;
        SectionTable.EmptyMessage = "No JWT validation configs.";
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
        // JWT validation items cannot be deleted — no-op
    }

    private async void OnAddClick(object? sender, EventArgs e)
    {
        // No add support for JWT validation
    }
}
