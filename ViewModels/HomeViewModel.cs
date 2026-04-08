using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class HomeViewModel(IAzureAuthService authService) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SignInButtonText))]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    public partial bool IsSigningIn { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public string SignInButtonText => IsAuthenticated ? "Sign Out" : "Sign In to Azure";

    public string StatusMessage => IsAuthenticated
        ? "You are signed in. Use the navigation menu to browse your Azure resources."
        : "Sign in with your Azure account to get started.";

    [RelayCommand]
    private async Task ToggleSignInAsync(CancellationToken cancellationToken)
    {
        if (IsAuthenticated)
        {
            authService.SignOut();
            IsAuthenticated = false;
            ErrorMessage = null;
            return;
        }

        IsSigningIn = true;
        ErrorMessage = null;

        try
        {
            await authService.SignInAsync(cancellationToken);
            IsAuthenticated = true;
        }
        catch (OperationCanceledException)
        {
            // User cancelled — no error to show
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Sign-in failed: {ex.Message}";
        }
        finally
        {
            IsSigningIn = false;
        }
    }
}
