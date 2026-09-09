namespace DecisionPlatform.Domain;

public sealed class RoadmapOrchestrator
{
    private readonly RoadmapTimingPolicy _timing;

    public RoadmapOrchestrator(RoadmapTimingPolicy? timing = null) => _timing = timing ?? RoadmapTimingPolicy.Default;

    public ProcessResult Process(RoadmapState state, MarketEvent marketEvent)
    {
        if (marketEvent.ClosedAt > marketEvent.ObservedAt)
            return Reject(state, marketEvent, "LOOKAHEAD_REJECTED");
        if (state.LastObservedAt is { } last && marketEvent.ObservedAt < last)
            return Reject(state, marketEvent, "OUT_OF_ORDER_REJECTED");
        if (!state.ProcessedEventIds.Add(marketEvent.EventId))
            return Reject(state, marketEvent, "DUPLICATE_EVENT_REJECTED");

        state.LastObservedAt = marketEvent.ObservedAt;
        var expiryReason = ReconcileExpiredEvidence(state, marketEvent);
        var before = state.Phase;
        var resolution = Resolve(state, marketEvent);
        AddEvidence(state, resolution.Evidence);
        state.Phase = resolution.Next;

        if (state.Phase != before)
            state.Timeline.Add(new(before, state.Phase, marketEvent.EventId, marketEvent.ObservedAt, resolution.Reason));

        var active = state.ActiveEvidence.ToArray();
        var reason = expiryReason is not null && resolution.Reason == "NO_TRANSITION" ? expiryReason : resolution.Reason;
        var decision = new RoadmapDecision(
            $"d-{marketEvent.EventId}",
            state.Symbol,
            state.Phase,
            resolution.Intent,
            state.Bias,
            Math.Min(1, active.Sum(x => x.Strength) / 3),
            active.Select(x => x.EvidenceId).ToArray(),
            [reason],
            marketEvent.ObservedAt);
        return new(true, state, decision);
    }

    private (RoadmapPhase Next, Evidence? Evidence, Intent Intent, string Reason) Resolve(RoadmapState state, MarketEvent marketEvent)
    {
        if (marketEvent.Kind == EventKind.SetupInvalidated)
        {
            var hadPosition = state.Phase == RoadmapPhase.InPosition;
            state.ActiveEvidence.Clear();
            state.MacroBias = null;
            state.Bias = null;
            return (RoadmapPhase.WaitContext, null, hadPosition ? Intent.Exit : Intent.Wait, "SETUP_INVALIDATED");
        }

        if (TryMacroBias(marketEvent.Kind, out var macroBias))
        {
            var hasConflictingSetup = state.Phase != RoadmapPhase.WaitContext && state.Bias != macroBias;
            if (hasConflictingSetup)
            {
                state.ActiveEvidence.Clear();
                state.Bias = null;
            }

            state.MacroBias = macroBias;
            return (hasConflictingSetup ? RoadmapPhase.WaitContext : state.Phase,
                EvidenceFor(marketEvent, EvidenceKind.MacroAligned, macroBias, .6, _timing.MacroEvidenceTtl),
                Intent.Wait,
                hasConflictingSetup ? "MACRO_CONTEXT_CHANGED" : "MACRO_CONTEXT_CONFIRMED");
        }

        if (state.Phase == RoadmapPhase.WaitContext && TrySwingBias(marketEvent.Kind, out var swingBias))
        {
            if (state.MacroBias is null)
                return (state.Phase, null, Intent.Wait, "MACRO_CONTEXT_REQUIRED");
            if (state.MacroBias != swingBias)
                return (state.Phase, null, Intent.Wait, "MACRO_CONTEXT_MISMATCH");

            state.Bias = swingBias;
            return (RoadmapPhase.MonitorPullback,
                EvidenceFor(marketEvent, EvidenceKind.SwingContinuation, swingBias, .9, _timing.SwingEvidenceTtl),
                Intent.Prepare,
                "SWING_CONTEXT_CONFIRMED");
        }

        if (state.Phase == RoadmapPhase.MonitorPullback && marketEvent.Kind == EventKind.PullbackAtPoi && marketEvent.Side == state.Bias)
            return (RoadmapPhase.WaitConfirmation,
                EvidenceFor(marketEvent, EvidenceKind.PullbackReady, state.Bias, .8, _timing.PullbackEvidenceTtl),
                Intent.Prepare,
                "PULLBACK_AT_POI");

        if (state.Phase == RoadmapPhase.WaitConfirmation && IsAlignedInternalBos(state.Bias, marketEvent.Kind))
            return (RoadmapPhase.EntryReady,
                EvidenceFor(marketEvent, EvidenceKind.ExecutionConfirmed, state.Bias, 1, _timing.ExecutionEvidenceTtl),
                Intent.Enter,
                "EXECUTION_CONFIRMED");

        if (state.Phase == RoadmapPhase.EntryReady && marketEvent.Kind == EventKind.PositionClosed)
            return (RoadmapPhase.Cooldown, null, Intent.Exit, "POSITION_CLOSED");

        return (state.Phase, null, Intent.Wait, "NO_TRANSITION");
    }

    private static void AddEvidence(RoadmapState state, Evidence? evidence)
    {
        if (evidence is null)
            return;

        if (evidence.Kind == EvidenceKind.MacroAligned)
            state.ActiveEvidence.RemoveAll(x => x.Kind == EvidenceKind.MacroAligned);
        if (state.ActiveEvidence.All(x => x.EventId != evidence.EventId))
            state.ActiveEvidence.Add(evidence);
    }

    private static string? ReconcileExpiredEvidence(RoadmapState state, MarketEvent marketEvent)
    {
        var expired = state.ActiveEvidence.Where(x => x.ExpiresAt <= marketEvent.ObservedAt).ToArray();
        if (expired.Length == 0)
            return null;

        state.ActiveEvidence.RemoveAll(x => x.ExpiresAt <= marketEvent.ObservedAt);
        var before = state.Phase;
        string? reason = null;

        if (!HasEvidence(state, EvidenceKind.MacroAligned))
        {
            state.ActiveEvidence.Clear();
            state.MacroBias = null;
            state.Bias = null;
            state.Phase = RoadmapPhase.WaitContext;
            reason = "MACRO_CONTEXT_EXPIRED";
        }
        else if (state.Phase >= RoadmapPhase.MonitorPullback && !HasEvidence(state, EvidenceKind.SwingContinuation))
        {
            state.ActiveEvidence.RemoveAll(x => x.Kind != EvidenceKind.MacroAligned);
            state.Bias = null;
            state.Phase = RoadmapPhase.WaitContext;
            reason = "SWING_CONTEXT_EXPIRED";
        }
        else if (state.Phase >= RoadmapPhase.WaitConfirmation && !HasEvidence(state, EvidenceKind.PullbackReady))
        {
            state.ActiveEvidence.RemoveAll(x => x.Kind == EvidenceKind.ExecutionConfirmed);
            state.Phase = RoadmapPhase.MonitorPullback;
            reason = "PULLBACK_EXPIRED";
        }
        else if (state.Phase >= RoadmapPhase.EntryReady && !HasEvidence(state, EvidenceKind.ExecutionConfirmed))
        {
            state.Phase = RoadmapPhase.WaitConfirmation;
            reason = "EXECUTION_CONFIRMATION_EXPIRED";
        }

        if (reason is not null && state.Phase != before)
            state.Timeline.Add(new(before, state.Phase, marketEvent.EventId, marketEvent.ObservedAt, reason));

        return reason;
    }

    private static bool HasEvidence(RoadmapState state, EvidenceKind kind) => state.ActiveEvidence.Any(x => x.Kind == kind);

    private static bool TryMacroBias(EventKind kind, out Side side)
    {
        side = kind == EventKind.MacroBiasUp ? Side.Long : Side.Short;
        return kind is EventKind.MacroBiasUp or EventKind.MacroBiasDown;
    }

    private static bool TrySwingBias(EventKind kind, out Side side)
    {
        side = kind == EventKind.SwingBosUp ? Side.Long : Side.Short;
        return kind is EventKind.SwingBosUp or EventKind.SwingBosDown;
    }

    private static bool IsAlignedInternalBos(Side? bias, EventKind kind) =>
        (bias == Side.Long && kind == EventKind.InternalBosUp) || (bias == Side.Short && kind == EventKind.InternalBosDown);

    private static Evidence EvidenceFor(MarketEvent marketEvent, EvidenceKind kind, Side? side, double strength, TimeSpan ttl) =>
        new($"ev-{marketEvent.EventId}", marketEvent.EventId, kind, side, strength, marketEvent.ObservedAt, marketEvent.ObservedAt + ttl);

    private static ProcessResult Reject(RoadmapState state, MarketEvent marketEvent, string reason) => new(false, state,
        new($"d-{marketEvent.EventId}", state.Symbol, state.Phase, Intent.Reject, state.Bias, 0, [], [reason], marketEvent.ObservedAt));
}
