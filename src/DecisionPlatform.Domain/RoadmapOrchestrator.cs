namespace DecisionPlatform.Domain;

public sealed class RoadmapOrchestrator
{
    public ProcessResult Process(RoadmapState state, MarketEvent marketEvent)
    {
        if (marketEvent.ClosedAt > marketEvent.ObservedAt)
            return Reject(state, marketEvent, "LOOKAHEAD_REJECTED");
        if (!state.ProcessedEventIds.Add(marketEvent.EventId))
            return Reject(state, marketEvent, "DUPLICATE_EVENT_REJECTED");
        if (state.LastObservedAt is { } last && marketEvent.ObservedAt < last)
            return Reject(state, marketEvent, "OUT_OF_ORDER_REJECTED");

        state.LastObservedAt = marketEvent.ObservedAt;
        state.ActiveEvidence.RemoveAll(x => x.ExpiresAt <= marketEvent.ObservedAt);

        var before = state.Phase;
        var (next, evidence, intent, reason) = Resolve(state, marketEvent);
        if (evidence is not null && state.ActiveEvidence.All(x => x.EventId != evidence.EventId)) state.ActiveEvidence.Add(evidence);
        state.Phase = next;
        if (next != before) state.Timeline.Add(new(before, next, marketEvent.EventId, marketEvent.ObservedAt, reason));

        var active = state.ActiveEvidence.Where(x => x.InvalidatedBy is null).ToArray();
        var decision = new RoadmapDecision($"d-{marketEvent.EventId}", state.Symbol, next, intent, state.Bias,
            Math.Min(1, active.Sum(x => x.Strength) / 3), active.Select(x => x.EvidenceId).ToArray(), [reason], marketEvent.ObservedAt);
        return new(true, state, decision);
    }

    private static (RoadmapPhase, Evidence?, Intent, string) Resolve(RoadmapState state, MarketEvent e)
    {
        if (e.Kind == EventKind.SetupInvalidated)
        {
            state.ActiveEvidence.Clear(); state.Bias = null;
            return (RoadmapPhase.WaitContext, EvidenceFor(e, EvidenceKind.Invalidated, .0, null), Intent.Exit, "SETUP_INVALIDATED");
        }
        if (state.Phase == RoadmapPhase.WaitContext && e.Kind is EventKind.SwingBosUp or EventKind.SwingBosDown)
        {
            state.Bias = e.Kind == EventKind.SwingBosUp ? Side.Long : Side.Short;
            return (RoadmapPhase.MonitorPullback, EvidenceFor(e, EvidenceKind.SwingContinuation, .9, TimeSpan.FromHours(8)), Intent.Prepare, "SWING_CONTEXT_CONFIRMED");
        }
        if (state.Phase == RoadmapPhase.MonitorPullback && e.Kind == EventKind.PullbackAtPoi && e.Side == state.Bias)
            return (RoadmapPhase.WaitConfirmation, EvidenceFor(e, EvidenceKind.PullbackReady, .8, TimeSpan.FromMinutes(45)), Intent.Prepare, "PULLBACK_AT_POI");
        if (state.Phase == RoadmapPhase.WaitConfirmation && ((state.Bias == Side.Long && e.Kind == EventKind.InternalBosUp) || (state.Bias == Side.Short && e.Kind == EventKind.InternalBosDown)))
            return (RoadmapPhase.EntryReady, EvidenceFor(e, EvidenceKind.ExecutionConfirmed, 1, TimeSpan.FromMinutes(20)), Intent.Enter, "EXECUTION_CONFIRMED");
        if (state.Phase == RoadmapPhase.EntryReady && e.Kind == EventKind.PositionClosed)
            return (RoadmapPhase.Cooldown, null, Intent.Exit, "POSITION_CLOSED");
        return (state.Phase, null, Intent.Wait, "NO_TRANSITION");
    }

    private static Evidence EvidenceFor(MarketEvent e, EvidenceKind kind, double strength, TimeSpan? ttl) =>
        new($"ev-{e.EventId}", e.EventId, kind, e.Side, strength, e.ObservedAt, ttl is null ? null : e.ObservedAt + ttl);

    private static ProcessResult Reject(RoadmapState state, MarketEvent e, string reason) => new(false, state,
        new($"d-{e.EventId}", state.Symbol, state.Phase, Intent.Reject, state.Bias, 0, [], [reason], e.ObservedAt));
}
