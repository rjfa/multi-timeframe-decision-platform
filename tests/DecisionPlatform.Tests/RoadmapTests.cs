using DecisionPlatform.Domain;
using Xunit;

namespace DecisionPlatform.Tests;

public sealed class RoadmapTests
{
    private readonly RoadmapOrchestrator _sut = new();
    private static readonly DateTimeOffset T = new(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Macro_aligned_sequence_reaches_entry_ready_with_trace()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 5));
        _sut.Process(state, E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 25));
        var result = _sut.Process(state, E("c1", EventKind.InternalBosUp, Timeframe.M5, Side.Long, 40));

        Assert.Equal(RoadmapPhase.EntryReady, result.State.Phase);
        Assert.Equal(Intent.Enter, result.Decision.Intent);
        Assert.Equal(3, result.State.Timeline.Count);
    }

    [Fact]
    public void Swing_requires_matching_macro_context()
    {
        var state = new RoadmapState("SYNTH");
        var result = _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 0));

        Assert.True(result.Accepted);
        Assert.Equal(RoadmapPhase.WaitContext, result.State.Phase);
        Assert.Contains("MACRO_CONTEXT_REQUIRED", result.Decision.ReasonCodes);
    }

    [Fact]
    public void Expired_swing_context_resets_roadmap_before_pullback()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 5));
        var result = _sut.Process(state, E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 486));

        Assert.Equal(RoadmapPhase.WaitContext, result.State.Phase);
        Assert.Equal(Intent.Wait, result.Decision.Intent);
        Assert.Contains("SWING_CONTEXT_EXPIRED", result.Decision.ReasonCodes);
        Assert.DoesNotContain(result.State.ActiveEvidence, x => x.Kind == EvidenceKind.SwingContinuation);
        Assert.Equal("SWING_CONTEXT_EXPIRED", result.State.Timeline[^1].ReasonCode);
    }

    [Fact]
    public void Expired_macro_context_resets_roadmap_before_swing_expiry()
    {
        var sut = new RoadmapOrchestrator(new RoadmapTimingPolicy(TimeSpan.FromMinutes(10), TimeSpan.FromHours(1), TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(20)));
        var state = new RoadmapState("SYNTH");
        sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 1));
        var result = sut.Process(state, E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 11));

        Assert.Equal(RoadmapPhase.WaitContext, result.State.Phase);
        Assert.Contains("MACRO_CONTEXT_EXPIRED", result.Decision.ReasonCodes);
        Assert.Empty(result.State.ActiveEvidence);
    }

    [Fact]
    public void Expired_pullback_returns_roadmap_to_monitor_pullback()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 1));
        _sut.Process(state, E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 2));
        var result = _sut.Process(state, E("c1", EventKind.InternalBosUp, Timeframe.M5, Side.Long, 48));

        Assert.Equal(RoadmapPhase.MonitorPullback, result.State.Phase);
        Assert.Contains("PULLBACK_EXPIRED", result.Decision.ReasonCodes);
        Assert.DoesNotContain(result.State.ActiveEvidence, x => x.Kind == EvidenceKind.PullbackReady);
        Assert.Contains(result.State.ActiveEvidence, x => x.Kind == EvidenceKind.SwingContinuation);
    }

    [Fact]
    public void Expired_execution_confirmation_returns_roadmap_to_wait_confirmation()
    {
        var state = EntryReadyState();
        var result = _sut.Process(state, E("x1", EventKind.PositionClosed, Timeframe.M5, Side.Long, 61));

        Assert.Equal(RoadmapPhase.WaitConfirmation, result.State.Phase);
        Assert.Equal(Intent.Wait, result.Decision.Intent);
        Assert.Contains("EXECUTION_CONFIRMATION_EXPIRED", result.Decision.ReasonCodes);
        Assert.DoesNotContain(result.State.ActiveEvidence, x => x.Kind == EvidenceKind.ExecutionConfirmed);
    }
    [Fact]
    public void Out_of_order_event_is_rejected_before_it_is_recorded_as_processed()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 10));
        var late = E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 5);
        var result = _sut.Process(state, late);

        Assert.False(result.Accepted);
        Assert.Contains("OUT_OF_ORDER_REJECTED", result.Decision.ReasonCodes);
        Assert.DoesNotContain(late.EventId, state.ProcessedEventIds);
    }

    [Fact]
    public void Invalidated_setup_clears_active_evidence_without_emitting_exit_for_preparation()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 5));
        var result = _sut.Process(state, E("i1", EventKind.SetupInvalidated, Timeframe.M5, Side.Short, 10));

        Assert.Equal(RoadmapPhase.WaitContext, result.State.Phase);
        Assert.Equal(Intent.Wait, result.Decision.Intent);
        Assert.Empty(result.State.ActiveEvidence);
        Assert.Contains("SETUP_INVALIDATED", result.Decision.ReasonCodes);
    }

    [Fact]
    public void Position_closed_moves_entry_ready_to_cooldown()
    {
        var state = EntryReadyState();
        var result = _sut.Process(state, E("x1", EventKind.PositionClosed, Timeframe.M5, Side.Long, 45));

        Assert.Equal(RoadmapPhase.Cooldown, result.State.Phase);
        Assert.Equal(Intent.Exit, result.Decision.Intent);
        Assert.Contains("POSITION_CLOSED", result.Decision.ReasonCodes);
    }

    [Fact]
    public void Duplicate_is_idempotently_rejected()
    {
        var state = new RoadmapState("SYNTH");
        var macro = E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0);
        _sut.Process(state, macro);
        var duplicate = _sut.Process(state, macro);

        Assert.False(duplicate.Accepted);
        Assert.Contains("DUPLICATE_EVENT_REJECTED", duplicate.Decision.ReasonCodes);
    }

    [Fact]
    public void Future_close_is_rejected_without_transition()
    {
        var state = new RoadmapState("SYNTH");
        var marketEvent = new MarketEvent("m1", "SYNTH", Timeframe.D1, T.AddMinutes(1), T, EventKind.MacroBiasUp, Side.Long, 100, "test", "1");
        var result = _sut.Process(state, marketEvent);

        Assert.False(result.Accepted);
        Assert.Equal(RoadmapPhase.WaitContext, state.Phase);
    }

    [Fact]
    public void Batch_and_incremental_replay_produce_the_same_trace()
    {
        var events = new[]
        {
            E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0),
            E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 5),
            E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 25),
            E("c1", EventKind.InternalBosUp, Timeframe.M5, Side.Long, 40),
            E("x1", EventKind.PositionClosed, Timeframe.M5, Side.Long, 45)
        };

        var batch = Replay(events);
        var incremental = events.Select(e => e).Aggregate(new List<ProcessResult>(), (results, marketEvent) =>
        {
            var state = results.Count == 0 ? new RoadmapState("SYNTH") : results[^1].State;
            results.Add(_sut.Process(state, marketEvent));
            return results;
        });

        Assert.Equal(batch.Select(Trace), incremental.Select(Trace));
    }

    private List<ProcessResult> Replay(IEnumerable<MarketEvent> events)
    {
        var state = new RoadmapState("SYNTH");
        return events.Select(marketEvent => _sut.Process(state, marketEvent)).ToList();
    }

    private static string Trace(ProcessResult result) => string.Join("|", new[]
    {
        result.Accepted.ToString(),
        result.State.Phase.ToString(),
        result.Decision.Intent.ToString(),
        string.Join(",", result.Decision.ReasonCodes),
        string.Join(",", result.Decision.EvidenceIds),
        string.Join(",", result.State.Timeline.Select(t => $"{t.From}>{t.To}:{t.ReasonCode}"))
    });
    private RoadmapState EntryReadyState()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("m1", EventKind.MacroBiasUp, Timeframe.D1, Side.Long, 0));
        _sut.Process(state, E("s1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 5));
        _sut.Process(state, E("p1", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 25));
        _sut.Process(state, E("c1", EventKind.InternalBosUp, Timeframe.M5, Side.Long, 40));
        return state;
    }

    private static MarketEvent E(string id, EventKind kind, Timeframe timeframe, Side side, int minute) =>
        new(id, "SYNTH", timeframe, T.AddMinutes(minute), T.AddMinutes(minute).AddSeconds(1), kind, side, 100, "test", "1");
}


