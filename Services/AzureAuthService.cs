using Azure.Core;
using Azure.Identity;

namespace AzureDesktop.Services;

public sealed class AzureAuthService : IAzureAuthService
{
    private static readonly string[] ManagementScopes = ["https://management.azure.com/.default"];
    private static readonly string AuthRecordPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AzureDesktop",
        "auth_record.json");

    private InteractiveBrowserCredential? _credential;

    public TokenCredential Credential =>
        _credential ?? throw new InvalidOperationException("Not authenticated. Call SignInAsync first.");

    public bool IsAuthenticated => _credential is not null;

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AuthRecordPath))
        {
            return false;
        }

        try
        {
            await using var stream = File.OpenRead(AuthRecordPath);
            var record = await AuthenticationRecord.DeserializeAsync(stream, cancellationToken);

            var credential = CreateCredential(record);

            // Silent token acquisition using the cached refresh token
            await credential.GetTokenAsync(
                new TokenRequestContext(ManagementScopes),
                cancellationToken);

            _credential = credential;
            return true;
        }
        catch
        {
            // Cache expired, corrupted, or refresh token revoked
            return false;
        }
    }

    public async Task SignInAsync(CancellationToken cancellationToken = default)
    {
        var credential = CreateCredential();

        // Interactive sign-in and capture the authentication record
        var record = await credential.AuthenticateAsync(
            new TokenRequestContext(ManagementScopes),
            cancellationToken);

        // Persist the record so future launches can restore silently
        Directory.CreateDirectory(Path.GetDirectoryName(AuthRecordPath)!);
        await using var stream = File.Create(AuthRecordPath);
        await record.SerializeAsync(stream, cancellationToken);

        _credential = credential;
    }

    public void SignOut()
    {
        _credential = null;

        try
        {
            File.Delete(AuthRecordPath);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static InteractiveBrowserCredential CreateCredential(AuthenticationRecord? record = null)
    {
        var options = new InteractiveBrowserCredentialOptions
        {
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
        };

        if (record is not null)
        {
            options.AuthenticationRecord = record;
        }

        return new InteractiveBrowserCredential(options);
    }
}
