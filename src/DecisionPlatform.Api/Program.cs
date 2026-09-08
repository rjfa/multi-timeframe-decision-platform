using DecisionPlatform.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<RoadmapOrchestrator>();
builder.Services.AddSingleton<ScenarioStore>();
builder.Services.AddCors(x => x.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/scenarios", (ScenarioStore store) => store.Names);
app.MapPost("/api/replay/{name}", (string name, ScenarioStore store, RoadmapOrchestrator orchestrator) =>
{
    if (!store.TryGet(name, out var events)) return Results.NotFound();
    var state = new RoadmapState("SYNTHUSDT");
    var steps = events.Select(e => orchestrator.Process(state, e)).Select(r => new { r.Accepted, r.Decision, Phase = r.State.Phase, Evidence = r.State.ActiveEvidence.ToArray(), Timeline = r.State.Timeline.ToArray() }).ToArray();
    return Results.Ok(steps);
});
app.Run();

public sealed class ScenarioStore
{
    private readonly Dictionary<string, MarketEvent[]> _scenarios = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bullish-continuation"] = Build(Side.Long),
        ["bearish-continuation"] = Build(Side.Short),
        ["invalidated-setup"] = [.. Build(Side.Long).Take(2), Event("e3", Timeframe.M5, EventKind.SetupInvalidated, Side.Short, 45)]
    };
    public string[] Names => _scenarios.Keys.Order().ToArray();
    public bool TryGet(string name, out MarketEvent[] events) => _scenarios.TryGetValue(name, out events!);
    private static MarketEvent[] Build(Side side) =>
    [
        Event("e1", Timeframe.H1, side == Side.Long ? EventKind.SwingBosUp : EventKind.SwingBosDown, side, 0),
        Event("e2", Timeframe.M5, EventKind.PullbackAtPoi, side, 25),
        Event("e3", Timeframe.M5, side == Side.Long ? EventKind.InternalBosUp : EventKind.InternalBosDown, side, 40)
    ];
    private static MarketEvent Event(string id, Timeframe tf, EventKind kind, Side side, int minutes)
    {
        var t = new DateTimeOffset(2026, 8, 18, 10, 0, 0, TimeSpan.Zero).AddMinutes(minutes);
        return new(id, "SYNTHUSDT", tf, t, t.AddSeconds(1), kind, side, 100m, "synthetic-v1", "1.0");
    }
}
