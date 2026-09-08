namespace DecisionPlatform.Domain;

public enum Timeframe { M5, H1, D1 }
public enum Side { Long, Short }
public enum EventKind { MacroBiasUp, MacroBiasDown, SwingBosUp, SwingBosDown, PullbackAtPoi, InternalBosUp, InternalBosDown, SetupInvalidated, PositionClosed }
public enum EvidenceKind { MacroAligned, SwingContinuation, PullbackReady, ExecutionConfirmed, Invalidated }
public enum RoadmapPhase { WaitContext, MonitorPullback, WaitConfirmation, EntryReady, InPosition, Cooldown }
public enum Intent { Wait, Prepare, Enter, Exit, Reject }

public sealed record MarketEvent(string EventId, string Symbol, Timeframe Timeframe, DateTimeOffset ClosedAt, DateTimeOffset ObservedAt, EventKind Kind, Side? Side, decimal? Level, string Source, string SourceVersion);
public sealed record Evidence(string EvidenceId, string EventId, EvidenceKind Kind, Side? Side, double Strength, DateTimeOffset ValidFrom, DateTimeOffset? ExpiresAt, string? InvalidatedBy = null);
public sealed record Transition(RoadmapPhase From, RoadmapPhase To, string EventId, DateTimeOffset At, string ReasonCode);
public sealed record RoadmapDecision(string DecisionId, string Symbol, RoadmapPhase Phase, Intent Intent, Side? Side, double Confidence, IReadOnlyList<string> EvidenceIds, IReadOnlyList<string> ReasonCodes, DateTimeOffset DecidedAt);

public sealed class RoadmapState(string symbol)
{
    public string Symbol { get; } = symbol;
    public RoadmapPhase Phase { get; internal set; } = RoadmapPhase.WaitContext;
    public Side? Bias { get; internal set; }
    public HashSet<string> ProcessedEventIds { get; } = [];
    public List<Evidence> ActiveEvidence { get; } = [];
    public List<Transition> Timeline { get; } = [];
    public DateTimeOffset? LastObservedAt { get; internal set; }
    public string RulesVersion { get; } = "2026.1";
}

public sealed record ProcessResult(bool Accepted, RoadmapState State, RoadmapDecision Decision);
