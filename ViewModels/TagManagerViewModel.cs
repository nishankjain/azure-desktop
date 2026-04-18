using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class TagEntry(string key, string value) : ObservableObject
{
    [ObservableProperty]
    public partial string Key { get; set; } = key;

    [ObservableProperty]
    public partial string Value { get; set; } = value;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial string EditValue { get; set; } = value;

    public void BeginEdit()
    {
        EditValue = Value;
        IsEditing = true;
        IsConfirmingDelete = false;
    }

    public void CancelEdit()
    {
        IsEditing = false;
    }

    public void BeginDelete()
    {
        IsConfirmingDelete = true;
        IsEditing = false;
    }

    public void CancelDelete()
    {
        IsConfirmingDelete = false;
    }
}

public partial class TagManagerViewModel(ITagService tagService, OperationManager operationManager) : ObservableObject, ILoadable
{
    private string? _resourceId;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? SuccessMessage { get; set; }

    [ObservableProperty]
    public partial string NewTagKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTagValue { get; set; } = string.Empty;

    public ObservableCollection<TagEntry> Tags { get; } = [];

    public async Task LoadTagsAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        _resourceId = resourceId;
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        Tags.Clear();

        try
        {
            var tags = await tagService.GetTagsAsync(resourceId, cancellationToken);
            foreach (var (key, value) in tags.OrderBy(t => t.Key))
            {
                Tags.Add(new TagEntry(key, value));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load tags: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddTagAsync(CancellationToken cancellationToken)
    {
        if (_resourceId is null || string.IsNullOrWhiteSpace(NewTagKey))
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        var op = operationManager.Begin("Update Tags", "Updating tags on", "Updated tags on", NewTagKey.Trim(), resourceId: _resourceId ?? string.Empty);

        try
        {
            await tagService.AddOrUpdateTagAsync(_resourceId, NewTagKey.Trim(), NewTagValue.Trim(), cancellationToken);

            var existing = Tags.FirstOrDefault(t => t.Key == NewTagKey.Trim());
            if (existing is not null)
            {
                existing.Value = NewTagValue.Trim();
            }
            else
            {
                Tags.Add(new TagEntry(NewTagKey.Trim(), NewTagValue.Trim()));
            }

            SuccessMessage = $"Tag '{NewTagKey.Trim()}' saved.";
            op.Complete();
            NewTagKey = string.Empty;
            NewTagValue = string.Empty;
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            ErrorMessage = $"Failed to save tag: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task SaveEditAsync(TagEntry tag, CancellationToken cancellationToken)
    {
        if (_resourceId is null)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        var op = operationManager.Begin("Update Tags", "Updating tags on", "Updated tags on", tag.Key, resourceId: _resourceId ?? string.Empty);

        try
        {
            await tagService.AddOrUpdateTagAsync(_resourceId, tag.Key, tag.EditValue.Trim(), cancellationToken);
            tag.Value = tag.EditValue.Trim();
            tag.IsEditing = false;
            SuccessMessage = $"Tag '{tag.Key}' updated.";
            op.Complete();
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            ErrorMessage = $"Failed to update tag: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync(TagEntry tag, CancellationToken cancellationToken)
    {
        if (_resourceId is null)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        var op = operationManager.Begin("Delete Tag", "Deleting tag", "Deleted tag", tag.Key, resourceId: _resourceId ?? string.Empty);

        try
        {
            await tagService.RemoveTagAsync(_resourceId, tag.Key, cancellationToken);
            Tags.Remove(tag);
            SuccessMessage = $"Tag '{tag.Key}' removed.";
            op.Complete();
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            ErrorMessage = $"Failed to remove tag: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
