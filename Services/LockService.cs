using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace AzureDesktop.Services;

public sealed record LockInfo(
    string Name,
    string Level,
    string Notes,
    string Scope,
    string LockResourceId);

public interface ILockService
{
    IAsyncEnumerable<LockInfo> GetLocksAsync(string resourceId, CancellationToken cancellationToken = default);
    Task CreateLockAsync(string resourceId, string name, ManagementLockLevel level, string notes, CancellationToken cancellationToken = default);
    Task DeleteLockAsync(string lockResourceId, CancellationToken cancellationToken = default);
}

public sealed class LockService(IAzureAuthService authService) : ILockService
{
    public async IAsyncEnumerable<LockInfo> GetLocksAsync(
        string resourceId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new ArmClient(authService.Credential);
        var resource = client.GetGenericResource(new ResourceIdentifier(resourceId));
        var lockCollection = resource.GetManagementLocks();

        await foreach (var lockResource in lockCollection.GetAllAsync(cancellationToken: cancellationToken))
        {
            // Extract the scope from the lock's resource ID
            // Lock ID format: {scope}/providers/Microsoft.Authorization/locks/{lockName}
            var lockId = lockResource.Data.Id.ToString();
            var scopeEnd = lockId.IndexOf("/providers/Microsoft.Authorization/locks/", StringComparison.OrdinalIgnoreCase);
            var scope = scopeEnd > 0 ? lockId[..scopeEnd] : resourceId;

            yield return new LockInfo(
                lockResource.Data.Name,
                lockResource.Data.Level.ToString(),
                lockResource.Data.Notes ?? "",
                scope,
                lockId);
        }
    }

    public async Task CreateLockAsync(
        string resourceId, string name, ManagementLockLevel level, string notes,
        CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var resource = client.GetGenericResource(new ResourceIdentifier(resourceId));
        var lockCollection = resource.GetManagementLocks();

        var data = new ManagementLockData(level)
        {
            Notes = notes
        };

        await lockCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, name, data, cancellationToken);
    }

    public async Task DeleteLockAsync(string lockResourceId, CancellationToken cancellationToken)
    {
        var client = new ArmClient(authService.Credential);
        var lockResource = client.GetManagementLockResource(new ResourceIdentifier(lockResourceId));
        await lockResource.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken);
    }
}
