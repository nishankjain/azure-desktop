namespace AzureDesktop.ViewModels;

/// <summary>
/// Implement on ViewModels that have a page-level loading state.
/// MainWindow binds the central ProgressRing to this.
/// </summary>
public interface ILoadable
{
    bool IsLoading { get; }
}
