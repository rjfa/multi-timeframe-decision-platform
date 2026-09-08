using DecisionPlatform.Domain;
using Xunit;

namespace DecisionPlatform.Tests;

public sealed class RoadmapTests
{
    private readonly RoadmapOrchestrator _sut = new();
    private static readonly DateTimeOffset T = new(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Closed_sequence_reaches_entry_ready_with_trace()
    {
        var state = new RoadmapState("SYNTH");
        _sut.Process(state, E("1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 0));
        _sut.Process(state, E("2", EventKind.PullbackAtPoi, Timeframe.M5, Side.Long, 25));
        var result = _sut.Process(state, E("3", EventKind.InternalBosUp, Timeframe.M5, Side.Long, 40));
        Assert.Equal(RoadmapPhase.EntryReady, result.State.Phase);
        Assert.Equal(Intent.Enter, result.Decision.Intent);
        Assert.Equal(3, result.State.Timeline.Count);
    }

    [Fact]
    public void Duplicate_is_idempotently_rejected()
    {
        var state = new RoadmapState("SYNTH"); var e = E("1", EventKind.SwingBosUp, Timeframe.H1, Side.Long, 0);
        _sut.Process(state, e); var duplicate = _sut.Process(state, e);
        Assert.False(duplicate.Accepted);
        Assert.Contains("DUPLICATE_EVENT_REJECTED", duplicate.Decision.ReasonCodes);
        Assert.Single(state.Timeline);
    }

    [Fact]
    public void Future_close_is_rejected_without_transition()
    {
        var state = new RoadmapState("SYNTH");
        var e = new MarketEvent("1", "SYNTH", Timeframe.H1, T.AddMinutes(1), T, EventKind.SwingBosUp, Side.Long, 100, "test", "1");
        var result = _sut.Process(state, e);
        Assert.False(result.Accepted); Assert.Equal(RoadmapPhase.WaitContext, state.Phase);
    }

    private static MarketEvent E(string id, EventKind kind, Timeframe tf, Side side, int minute) => new(id, "SYNTH", tf, T.AddMinutes(minute), T.AddMinutes(minute).AddSeconds(1), kind, side, 100, "test", "1");
}
