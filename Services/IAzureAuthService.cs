using Azure.Core;
using Azure.ResourceManager;

namespace AzureDesktop.Services;

public interface IAzureAuthService
{
    TokenCredential Credential { get; }
    ArmClient Client { get; }
    bool IsAuthenticated { get; }
    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
    Task SignInAsync(CancellationToken cancellationToken = default);
    void SignOut();
}
