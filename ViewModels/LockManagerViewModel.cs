using System.Collections.ObjectModel;
using Azure.ResourceManager.Resources.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureDesktop.Services;

namespace AzureDesktop.ViewModels;

public partial class LockEntry : ObservableObject
{
    public required string Name { get; init; }
    public required string Scope { get; init; }
    public required string LockResourceId { get; init; }
    public required bool IsInherited { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelIcon))]
    public partial string Level { get; set; } = string.Empty;

    // Shield for CanNotDelete, Eye for ReadOnly
    public string LevelIcon => Level == "ReadOnly" ? "\uE7B3" : "\uE72E";

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial string EditNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int EditLevelIndex { get; set; }

    public void BeginEdit()
    {
        EditNotes = Notes;
        EditLevelIndex = Level == "ReadOnly" ? 1 : 0;
        IsEditing = true;
        IsConfirmingDelete = false;
    }

    public void CancelEdit() => IsEditing = false;

    public void BeginDelete()
    {
        IsConfirmingDelete = true;
        IsEditing = false;
    }

    public void CancelDelete() => IsConfirmingDelete = false;
}

public partial class LockManagerViewModel(ILockService lockService, OperationManager operationManager) : ObservableObject
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
    public partial string NewLockName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewLockNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedLockLevelIndex { get; set; }

    public ObservableCollection<LockEntry> Locks { get; } = [];

    public async Task LoadLocksAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        _resourceId = resourceId;
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        Locks.Clear();

        try
        {
            await foreach (var lockInfo in lockService.GetLocksAsync(resourceId, cancellationToken))
            {
                var isInherited = !lockInfo.Scope.StartsWith(
                    resourceId, StringComparison.OrdinalIgnoreCase);

                Locks.Add(new LockEntry
                {
                    Name = lockInfo.Name,
                    Level = lockInfo.Level,
                    Notes = lockInfo.Notes,
                    Scope = lockInfo.Scope,
                    LockResourceId = lockInfo.LockResourceId,
                    IsInherited = isInherited,
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load locks: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddLockAsync(CancellationToken cancellationToken)
    {
        if (_resourceId is null || string.IsNullOrWhiteSpace(NewLockName))
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        var op = operationManager.Begin("Create Lock", "Creating", "Created", NewLockName.Trim(), resourceId: _resourceId ?? string.Empty);

        try
        {
            var level = SelectedLockLevelIndex == 1
                ? ManagementLockLevel.ReadOnly
                : ManagementLockLevel.CanNotDelete;

            await lockService.CreateLockAsync(
                _resourceId, NewLockName.Trim(), level, NewLockNotes.Trim(), cancellationToken);

            Locks.Add(new LockEntry
            {
                Name = NewLockName.Trim(),
                Level = level.ToString(),
                Notes = NewLockNotes.Trim(),
                Scope = _resourceId,
                LockResourceId = $"{_resourceId}/providers/Microsoft.Authorization/locks/{NewLockName.Trim()}",
                IsInherited = false,
            });

            SuccessMessage = $"Lock '{NewLockName.Trim()}' created.";
            op.Complete();
            NewLockName = string.Empty;
            NewLockNotes = string.Empty;
            SelectedLockLevelIndex = 0;
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            ErrorMessage = $"Failed to create lock: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task SaveEditAsync(LockEntry entry, CancellationToken cancellationToken)
    {
        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        var op = operationManager.Begin("Update Lock", "Updating", "Updated", entry.Name, resourceId: _resourceId ?? string.Empty);

        try
        {
            var level = entry.EditLevelIndex == 1
                ? ManagementLockLevel.ReadOnly
                : ManagementLockLevel.CanNotDelete;

            // CreateOrUpdate with the same name overwrites the lock
            await lockService.CreateLockAsync(
                entry.Scope, entry.Name, level, entry.EditNotes.Trim(), cancellationToken);

            entry.Level = level.ToString();
            entry.Notes = entry.EditNotes.Trim();
            entry.IsEditing = false;
            SuccessMessage = $"Lock '{entry.Name}' updated.";
            op.Complete();
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            ErrorMessage = $"Failed to update lock: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync(LockEntry entry, CancellationToken cancellationToken)
    {
        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        var op = operationManager.Begin("Delete Lock", "Deleting", "Deleted", entry.Name, resourceId: _resourceId ?? string.Empty);

        try
        {
            await lockService.DeleteLockAsync(entry.LockResourceId, cancellationToken);
            Locks.Remove(entry);
            SuccessMessage = $"Lock '{entry.Name}' deleted.";
            op.Complete();
        }
        catch (Exception ex)
        {
            op.Fail(ex.Message);
            ErrorMessage = $"Failed to delete lock: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
