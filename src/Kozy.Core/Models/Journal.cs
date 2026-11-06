namespace Kozy.Core.Models;

public sealed record JournalAction
{
    public required int Index { get; init; }
    public required string Type { get; init; }
    public object? Captured { get; init; }
    public object? Applied { get; init; }
    public bool Verified { get; init; }
}

public sealed record JournalTweak
{
    public required string Id { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<JournalAction> Actions { get; init; }
}

public sealed record RestorePointInfo
{
    public bool Created { get; init; }
    public int SequenceNumber { get; init; }
}

public sealed record JournalSession
{
    public required string SessionId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required string AppVersion { get; init; }
    public required int OsBuild { get; init; }
    public RestorePointInfo? RestorePoint { get; init; }
    public required IReadOnlyList<JournalTweak> Tweaks { get; init; }
}