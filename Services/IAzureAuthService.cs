using Azure.Core;

namespace AzureDesktop.Services;

public interface IAzureAuthService
{
    TokenCredential Credential { get; }
    bool IsAuthenticated { get; }
    Task SignInAsync(CancellationToken cancellationToken = default);
    void SignOut();
}
