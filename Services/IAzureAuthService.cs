using Azure.Core;

namespace AzureDesktop.Services;

public interface IAzureAuthService
{
    TokenCredential Credential { get; }
    bool IsAuthenticated { get; }
    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
    Task SignInAsync(CancellationToken cancellationToken = default);
    void SignOut();
}
