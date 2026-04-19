namespace AzureDesktop.ViewModels;

/// <summary>
/// A single breadcrumb segment in the hierarchy.
/// </summary>
/// <param name="Label">Display text (e.g. "Application Gateway")</param>
/// <param name="PageType">Page to navigate to when clicked, or null for current (last) segment</param>
/// <param name="Context">NavigationContext to pass when navigating, or null for current segment</param>
public sealed record BreadcrumbEntry(string Label, Type? PageType, NavigationContext? Context);

/// <summary>
/// A sidebar navigation item definition.
/// </summary>
/// <param name="Label">Display text</param>
/// <param name="Tag">Unique identifier for selection tracking</param>
/// <param name="IconFile">SVG icon filename from Assets/Icons/</param>
/// <param name="PageType">Page to navigate to when clicked</param>
public sealed record NavItemDefinition(string Label, string Tag, string IconFile, Type PageType);

/// <summary>
/// Interface for pages that participate in hierarchical navigation.
/// MainWindow reads this to render breadcrumbs and sidebar.
/// </summary>
public interface INavigablePage
{
    /// <summary>
    /// Returns the breadcrumb hierarchy from root to current page.
    /// </summary>
    BreadcrumbEntry[] GetBreadcrumbs();

    /// <summary>
    /// Returns the sidebar nav items to display when this page is active.
    /// </summary>
    NavItemDefinition[] GetNavItems();

    /// <summary>
    /// The tag of the currently active nav item, for highlighting.
    /// </summary>
    string? ActiveNavTag { get; }
}
