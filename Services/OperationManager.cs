using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzureDesktop.Services;

public enum OperationStatus
{
    InProgress,
    Succeeded,
    Failed,
}

public sealed partial class OperationEntry : ObservableObject
{
    public required string OperationName { get; init; }
    public required string InProgressText { get; init; }
    public required string CompletedText { get; init; }
    public required string ResourceName { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusIsInProgress))]
    [NotifyPropertyChangedFor(nameof(StatusIsSucceeded))]
    [NotifyPropertyChangedFor(nameof(StatusIsFailed))]
    [NotifyPropertyChangedFor(nameof(InProgressVisibility))]
    [NotifyPropertyChangedFor(nameof(SucceededVisibility))]
    [NotifyPropertyChangedFor(nameof(FailedVisibility))]
    public partial OperationStatus Status { get; set; } = OperationStatus.InProgress;

    public bool StatusIsInProgress => Status == OperationStatus.InProgress;
    public bool StatusIsSucceeded => Status == OperationStatus.Succeeded;
    public bool StatusIsFailed => Status == OperationStatus.Failed;

    public Microsoft.UI.Xaml.Visibility SucceededVisibility => StatusIsSucceeded ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility FailedVisibility => StatusIsFailed ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility InProgressVisibility => StatusIsInProgress ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    [ObservableProperty]
    public partial DateTimeOffset? CompletedAt { get; set; }

    [ObservableProperty]
    public partial string? TrackingId { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsRead { get; set; }

    public string Duration => CompletedAt.HasValue
        ? $"{(CompletedAt.Value - StartedAt).TotalSeconds:F1}s"
        : $"{(DateTimeOffset.Now - StartedAt).TotalSeconds:F0}s…";

    public string StatusIcon => Status switch
    {
        OperationStatus.InProgress => "\uF16A", // ProgressRing glyph placeholder
        OperationStatus.Succeeded => "\uE73E", // Checkmark
        OperationStatus.Failed => "\uEA39", // Error
        _ => "\uE946",
    };

    public string StartedAtFormatted => StartedAt.LocalDateTime.ToString("h:mm:ss tt");
    public string CompletedAtFormatted => CompletedAt?.LocalDateTime.ToString("h:mm:ss tt") ?? "—";

    public void Complete(string? trackingId = null)
    {
        TrackingId = trackingId;
        CompletedAt = DateTimeOffset.Now;
        Status = OperationStatus.Succeeded;
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(CompletedAtFormatted));
    }

    public void Fail(string errorMessage, string? trackingId = null)
    {
        ErrorMessage = errorMessage;
        TrackingId = trackingId;
        CompletedAt = DateTimeOffset.Now;
        Status = OperationStatus.Failed;
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(CompletedAtFormatted));
    }
}

public sealed class OperationManager
{
    private const int MaxHistory = 10;

    public ObservableCollection<OperationEntry> Operations { get; } = [];

    public int ActiveCount => Operations.Count(o => o.Status == OperationStatus.InProgress);

    public int UnreadCount => Operations.Count(o => !o.IsRead);

    public OperationEntry? LatestActive => Operations.FirstOrDefault(o => o.Status == OperationStatus.InProgress);

    public void MarkAllRead()
    {
        foreach (var op in Operations)
        {
            op.IsRead = true;
        }
    }

    public event Action? OperationUpdated;

    public OperationEntry Begin(string operationName, string inProgressText, string completedText, string resourceName, string resourceType = "", string resourceId = "")
    {
        var entry = new OperationEntry
        {
            OperationName = operationName,
            InProgressText = inProgressText,
            CompletedText = completedText,
            ResourceName = resourceName,
            ResourceType = resourceType,
            ResourceId = resourceId,
        };

        entry.PropertyChanged += (_, _) => OperationUpdated?.Invoke();

        Operations.Insert(0, entry);

        // Trim to max history
        while (Operations.Count > MaxHistory)
        {
            Operations.RemoveAt(Operations.Count - 1);
        }

        return entry;
    }
}
