using AzureDesktop.Helpers;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class AppGwRoutingRulesPage : Page
{
    private const AppGwSection Section = AppGwSection.RoutingRules;
    private CancellationTokenSource? _cts;
    private NavigationContext? _navCtx;
    public AppGwViewModel ViewModel { get; }

    public AppGwRoutingRulesPage()
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
        SectionTable.ItemsSource = ViewModel.RoutingRules;
        SectionTable.Columns = "Name, Rule Type, Priority, Listener, Backend Pool, Backend Settings";
        SectionTable.ShowCheckboxes = true;
        SectionTable.IsNavigable = true;
        SectionTable.EmptyMessage = "No routing rules configured.";
        SectionTable.ShowAddButton = AppGwViewModel.GetEditableFields(Section).Count > 0;

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
        if (_navCtx is null) return;
        Frame.Navigate(typeof(AppGwRoutingRuleDetailPage),
            _navCtx with { Section = AppGwSection.RoutingRules, DetailItemName = name });
    }

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
