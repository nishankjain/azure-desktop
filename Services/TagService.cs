using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace AzureDesktop.Services;

public interface ITagService
{
    Task<IDictionary<string, string>> GetTagsAsync(string resourceId, CancellationToken cancellationToken = default);
    Task AddOrUpdateTagAsync(string resourceId, string key, string value, CancellationToken cancellationToken = default);
    Task RemoveTagAsync(string resourceId, string key, CancellationToken cancellationToken = default);
}

public sealed class TagService(IAzureAuthService authService) : ITagService
{
    public async Task<IDictionary<string, string>> GetTagsAsync(string resourceId, CancellationToken cancellationToken)
    {
        var client = authService.Client;
        var tagResource = client.GetTagResource(TagResource.CreateResourceIdentifier(new ResourceIdentifier(resourceId)));
        var response = await tagResource.GetAsync(cancellationToken);
        return response.Value.Data.TagValues;
    }

    public async Task AddOrUpdateTagAsync(string resourceId, string key, string value, CancellationToken cancellationToken)
    {
        var client = authService.Client;
        var tagResource = client.GetTagResource(TagResource.CreateResourceIdentifier(new ResourceIdentifier(resourceId)));
        var patch = new TagResourcePatch
        {
            PatchMode = TagPatchMode.Merge,
        };
        patch.TagValues[key] = value;
        await tagResource.UpdateAsync(Azure.WaitUntil.Completed, patch, cancellationToken);
    }

    public async Task RemoveTagAsync(string resourceId, string key, CancellationToken cancellationToken)
    {
        var client = authService.Client;
        var tagResource = client.GetTagResource(TagResource.CreateResourceIdentifier(new ResourceIdentifier(resourceId)));
        var patch = new TagResourcePatch
        {
            PatchMode = TagPatchMode.Delete,
        };
        patch.TagValues[key] = "";
        await tagResource.UpdateAsync(Azure.WaitUntil.Completed, patch, cancellationToken);
    }
}
