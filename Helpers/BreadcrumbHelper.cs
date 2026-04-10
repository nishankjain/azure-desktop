using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Helpers;

/// <summary>
/// Manages a breadcrumb trail that shows at most 3 visible items,
/// collapsing earlier items into an ellipsis flyout.
/// </summary>
public sealed class BreadcrumbHelper
{
    private readonly List<(string Label, Action Navigate)> _fullChain = [];
    private readonly ObservableCollection<string> _visibleItems = [];
    private readonly Button _ellipsisButton;
    private readonly BreadcrumbBar _breadcrumb;

    public ObservableCollection<string> VisibleItems => _visibleItems;

    public BreadcrumbHelper(BreadcrumbBar breadcrumb, Button ellipsisButton)
    {
        _breadcrumb = breadcrumb;
        _ellipsisButton = ellipsisButton;
        _breadcrumb.ItemsSource = _visibleItems;
    }

    public void Clear()
    {
        _fullChain.Clear();
        _visibleItems.Clear();
    }

    public void Add(string label, Action navigate)
    {
        _fullChain.Add((label, navigate));
    }

    /// <summary>
    /// Call after all items are added. Shows the last 3 items in the BreadcrumbBar
    /// and collapses earlier items into the ellipsis button.
    /// </summary>
    public void Apply()
    {
        _visibleItems.Clear();

        var maxVisible = 3;
        var collapsedCount = Math.Max(0, _fullChain.Count - maxVisible);

        if (collapsedCount > 0)
        {
            _ellipsisButton.Visibility = Visibility.Visible;

            var flyout = new MenuFlyout();
            for (var i = 0; i < collapsedCount; i++)
            {
                var index = i;
                var item = new MenuFlyoutItem { Text = _fullChain[i].Label };
                item.Click += (_, _) => _fullChain[index].Navigate();
                flyout.Items.Add(item);
            }

            _ellipsisButton.Flyout = flyout;

            // Add items after the collapsed ones (these are visible)
            for (var i = collapsedCount; i < _fullChain.Count; i++)
            {
                _visibleItems.Add(_fullChain[i].Label);
            }
        }
        else
        {
            _ellipsisButton.Visibility = Visibility.Collapsed;

            foreach (var (label, _) in _fullChain)
            {
                _visibleItems.Add(label);
            }
        }
    }

    /// <summary>
    /// Handles BreadcrumbBar.ItemClicked. The index is relative to the visible items.
    /// </summary>
    public void HandleClick(int visibleIndex)
    {
        var collapsedCount = Math.Max(0, _fullChain.Count - 3);
        var actualIndex = collapsedCount + visibleIndex;

        if (actualIndex >= 0 && actualIndex < _fullChain.Count - 1) // Don't navigate on last (current) item
        {
            _fullChain[actualIndex].Navigate();
        }
    }
}
