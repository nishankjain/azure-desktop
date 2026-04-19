using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;

namespace AzureDesktop.Views;

public sealed partial class AppGwRoutingRulesPage : AppGwPageBase
{
    public override string PageLabel => "Routing Rules";
    public override string? ActiveNavTag => "AppGwRoutingRules";

    public AppGwRoutingRulesPage()
    {
        InitializeComponent();
    }

    protected override void OnDataLoaded()
    {
        SectionTable.ItemsSource = ViewModel.RoutingRules;
        SectionTable.Columns = "Name, Rule Type, Priority, Listener, Backend Pool, Backend Settings";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = true;
        SectionTable.EmptyMessage = "No routing rules configured.";
        SectionTable.ShowAddButton = AppGwViewModel.GetRoutingRuleFields().Count > 0;

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
        Frame.Navigate(typeof(AppGwRoutingRuleDetailPage),
            NavCtx with { Section = AppGwSection.RoutingRules, DetailItemName = name });
    }

    private async void OnDeleteClick(object? sender, List<string> names)
    {
        var deleted = false;
        foreach (var name in names)
        {
            if (ViewModel.DeleteRoutingRule(name)) deleted = true;
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
            AppGwViewModel.GetRoutingRuleFields(),
            ViewModel.AddRoutingRule,
            async desc => { await ViewModel.SaveChangesAsync(desc); },
            OnDataLoaded);
    }
}
