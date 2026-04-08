using Azure.Core;
using Azure.Identity;

namespace AzureDesktop.Services;

public sealed class AzureAuthService : IAzureAuthService
{
    private InteractiveBrowserCredential? _credential;

    public TokenCredential Credential =>
        _credential ?? throw new InvalidOperationException("Not authenticated. Call SignInAsync first.");

    public bool IsAuthenticated => _credential is not null;

    public async Task SignInAsync(CancellationToken cancellationToken = default)
    {
        _credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
        {
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions()
        });

        // Force a token acquisition to validate the sign-in
        await _credential.GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]),
            cancellationToken);
    }

    public void SignOut()
    {
        _credential = null;
    }
}
