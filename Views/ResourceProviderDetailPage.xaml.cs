using System.Collections.ObjectModel;
using AzureDesktop.Services;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzureDesktop.Views;

public sealed partial class ResourceProviderDetailPage : Page
{
    public ObservableCollection<string> BreadcrumbItems { get; } = [];

    private ResourceProviderEntry? _entry;
    private SubscriptionItem? _subItem;
    private IReadOnlyList<ResourceTypeInfo> _allResourceTypes = [];
    private ResourceTypeInfo? _selectedResourceType;

    public string ProviderNamespace => _entry?.Namespace ?? string.Empty;
    public string RegistrationState => _entry?.RegistrationState ?? string.Empty;
    public string RegistrationPolicy => _entry?.RegistrationPolicy ?? string.Empty;
    public bool IsRegistered => _entry?.IsRegistered ?? false;
    public bool IsNotRegistered => !IsRegistered;
    public bool IsUpdating => _entry?.IsUpdating ?? false;
    public int ResourceTypeCount => _allResourceTypes.Count;
    public string? ErrorMessage { get; private set; }

    public string SelectedResourceTypeName => _selectedResourceType?.ResourceType ?? string.Empty;
    public string SelectedDefaultApiVersion => _selectedResourceType?.DefaultApiVersion ?? string.Empty;
    public IReadOnlyList<string> SelectedCapabilities => string.IsNullOrWhiteSpace(_selectedResourceType?.Capabilities)
        ? ["None"]
        : _selectedResourceType.Capabilities.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public ResourceProviderDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (ResourceProviderEntry entry, SubscriptionItem sub))
        {
            _entry = entry;
            _subItem = sub;
            _allResourceTypes = entry.ResourceTypes;

            BreadcrumbItems.Clear();
            BreadcrumbItems.Add("Subscriptions");
            BreadcrumbItems.Add(sub.Name);
            BreadcrumbItems.Add("Resource Providers");
            BreadcrumbItems.Add(entry.Namespace);
            Breadcrumb.ItemsSource = BreadcrumbItems;

            Bindings.Update();
        }
    }

    private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        switch (args.Index)
        {
            case 0:
                Frame.BackStack.Clear();
                Frame.Navigate(typeof(SubscriptionsPage));
                break;
            case 1 when _subItem is not null:
                Frame.Navigate(typeof(SubscriptionDetailPage), _subItem);
                break;
            case 2 when _subItem is not null:
                Frame.Navigate(typeof(ResourceProvidersPage), _subItem);
                break;
        }
    }

    private void ResourceTypeSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            PopulateSuggestions(sender);
        }
    }

    private void ResourceTypeSearch_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox box)
        {
            box.IsSuggestionListOpen = true;
            PopulateSuggestions(box);
        }
    }

    private void PopulateSuggestions(AutoSuggestBox box)
    {
        var query = box.Text;
        var suggestions = string.IsNullOrWhiteSpace(query)
            ? _allResourceTypes.Select(rt => rt.ResourceType).ToList()
            : _allResourceTypes
                .Where(rt => rt.ResourceType.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(rt => rt.ResourceType)
                .ToList();

        box.ItemsSource = suggestions;
    }

    private void ResourceTypeSearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string resourceTypeName)
        {
            _selectedResourceType = _allResourceTypes
                .FirstOrDefault(rt => rt.ResourceType == resourceTypeName);

            if (_selectedResourceType is not null)
            {
                ResourceTypeDetails.Visibility = Visibility.Visible;
                CapabilitiesList.ItemsSource = SelectedCapabilities;
                ApiVersionsList.ItemsSource = _selectedResourceType.ApiVersions;
                LocationsList.ItemsSource = _selectedResourceType.Locations;
                Bindings.Update();
            }
        }
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        if (_entry is null || _subItem is null)
        {
            return;
        }

        var vm = App.GetService<ResourceProvidersViewModel>();
        await vm.RegisterProviderAsync(_entry);
        Bindings.Update();
    }

    private async void Unregister_Click(object sender, RoutedEventArgs e)
    {
        if (_entry is null || _subItem is null)
        {
            return;
        }

        var vm = App.GetService<ResourceProvidersViewModel>();
        await vm.UnregisterProviderAsync(_entry);
        Bindings.Update();
    }
}
