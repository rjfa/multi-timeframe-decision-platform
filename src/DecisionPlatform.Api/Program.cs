using System.Text.Json.Serialization;
using DecisionPlatform.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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
    var steps = events.Select(e => new { Event = new { e.EventId, e.Timeframe, e.Side, e.ClosedAt, e.ObservedAt, e.Source, e.SourceVersion }, Result = orchestrator.Process(state, e) }).Select(x => new { x.Result.Accepted, x.Result.Decision, Phase = x.Result.State.Phase, Evidence = x.Result.State.ActiveEvidence.ToArray(), Timeline = x.Result.State.Timeline.ToArray(), x.Event }).ToArray();
    return Results.Ok(steps);
});
app.Run();

public sealed class ScenarioStore
{
    private readonly Dictionary<string, MarketEvent[]> _scenarios = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bullish-continuation"] = Build(Side.Long),
        ["bearish-continuation"] = Build(Side.Short),
        ["invalidated-setup"] = [.. Build(Side.Long).Take(3), Event("e4", Timeframe.M5, EventKind.SetupInvalidated, Side.Short, 45)]
    };

    public string[] Names => _scenarios.Keys.Order().ToArray();
    public bool TryGet(string name, out MarketEvent[] events) => _scenarios.TryGetValue(name, out events!);

    private static MarketEvent[] Build(Side side) =>
    [
        Event("e1", Timeframe.D1, side == Side.Long ? EventKind.MacroBiasUp : EventKind.MacroBiasDown, side, 0),
        Event("e2", Timeframe.H1, side == Side.Long ? EventKind.SwingBosUp : EventKind.SwingBosDown, side, 5),
        Event("e3", Timeframe.M5, EventKind.PullbackAtPoi, side, 25),
        Event("e4", Timeframe.M5, side == Side.Long ? EventKind.InternalBosUp : EventKind.InternalBosDown, side, 40)
    ];

    private static MarketEvent Event(string id, Timeframe timeframe, EventKind kind, Side side, int minutes)
    {
        var timestamp = new DateTimeOffset(2026, 8, 18, 10, 0, 0, TimeSpan.Zero).AddMinutes(minutes);
        return new(id, "SYNTHUSDT", timeframe, timestamp, timestamp.AddSeconds(1), kind, side, 100m, "synthetic-v1", "1.0");
    }
}


