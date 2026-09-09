import { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import { portfolioLinks } from "./portfolio-links";
import "./styles.css";

type TimelineEntry = { from: string; to: string; reasonCode: string; at: string };
type Evidence = { evidenceId: string; kind: string; strength: number; expiresAt: string | null };
type ReplayEvent = { eventId: string; timeFrame: string; side: string | null; closedAt: string; observedAt: string; source: string; sourceVersion: string };
type Step = {
  accepted: boolean;
  phase: string;
  event: ReplayEvent;
  decision: { intent: string; confidence: number; reasonCodes: string[]; decidedAt: string };
  evidence: Evidence[];
  timeline: TimelineEntry[];
};
type Scenario = { id: string; title: string; objective: string; expected: string };
type LoadState = "idle" | "loading" | "ready" | "error";

const scenarioDetails: Record<string, Omit<Scenario, "id">> = {
  "bullish-continuation": { title: "Aligned continuation", objective: "Show D1 macro authority gating H1 context before M5 confirmation.", expected: "The roadmap reaches EntryReady; it does not create an order." },
  "bearish-continuation": { title: "Aligned bearish continuation", objective: "Replay the same authority contract with a short-side context.", expected: "The roadmap reaches EntryReady with a short bias." },
  "invalidated-setup": { title: "Invalidated setup", objective: "Show that invalidation clears evidence instead of silently advancing a setup.", expected: "The roadmap returns to WaitContext without an exit order." },
};

const reasonDescriptions: Record<string, string> = {
  MACRO_CONTEXT_CONFIRMED: "D1 macro context is recorded as a gate for slower strategic context.",
  SWING_CONTEXT_CONFIRMED: "H1 swing context matches the active macro bias.",
  PULLBACK_AT_POI: "The M5 pullback advances the roadmap only inside the approved context.",
  EXECUTION_CONFIRMED: "The final M5 confirmation creates a traceable intent, not an order.",
  SETUP_INVALIDATED: "The setup is cleared and evidence is not allowed to remain active.",
  NO_TRANSITION: "The event was accepted but did not satisfy a transition rule.",
};

const displayTime = (timestamp: string) => new Intl.DateTimeFormat("en", { dateStyle: "medium", timeStyle: "medium", timeZone: "UTC" }).format(new Date(timestamp));

function App() {
  const [scenarioIds, setScenarioIds] = useState<string[]>(Object.keys(scenarioDetails));
  const [selected, setSelected] = useState("bullish-continuation");
  const [steps, setSteps] = useState<Step[]>([]);
  const [index, setIndex] = useState(-1);
  const [loadState, setLoadState] = useState<LoadState>("idle");
  const [error, setError] = useState("");

  const scenarios = useMemo<Scenario[]>(() => scenarioIds.map((id) => ({ id, ...(scenarioDetails[id] ?? { title: id, objective: "Replay a synthetic decision sequence.", expected: "Review its decision trace." }) })), [scenarioIds]);
  const selectedScenario = scenarios.find((scenario) => scenario.id === selected) ?? scenarios[0];
  const step = index >= 0 ? steps[index] : undefined;
  const completed = steps.length > 0 && index === steps.length - 1;

  useEffect(() => {
    fetch("/api/scenarios")
      .then((response) => response.ok ? response.json() as Promise<string[]> : Promise.reject(new Error("Scenario list could not be loaded.")))
      .then((ids) => {
        setScenarioIds(ids);
        if (!ids.includes(selected)) setSelected(ids[0] ?? "");
      })
      .catch(() => setError("Scenario list is unavailable. You can retry once the API is running."));
  }, []);

  async function loadReplay() {
    setLoadState("loading");
    setError("");
    try {
      const response = await fetch(`/api/replay/${selected}`, { method: "POST" });
      if (!response.ok) throw new Error("The selected replay could not be loaded.");
      const replay = await response.json() as Step[];
      if (replay.length === 0) throw new Error("The selected replay contains no events.");
      setSteps(replay);
      setIndex(0);
      setLoadState("ready");
    } catch (cause) {
      setSteps([]);
      setIndex(-1);
      setLoadState("error");
      setError(cause instanceof Error ? cause.message : "The replay could not be loaded.");
    }
  }

  function selectScenario(id: string) {
    setSelected(id);
    setSteps([]);
    setIndex(-1);
    setLoadState("idle");
    setError("");
  }

  return (
    <main>
      <header className="hero">
        <div className="hero-links"><a href={portfolioLinks.caseStudy}>Back to Study Case 01</a><a href="#replay">Run the guided replay</a></div>
        <p className="eyebrow">Interactive architecture demo</p>
        <h1>Multi-Timeframe Decision Platform</h1>
        <p className="lead">A guided replay of how explicit evidence, authority and time prevent a decision system from advancing on stale or future data.</p>
        <div className="guardrails"><span>Synthetic events</span><span>Closed observations only</span><span>No live trading or performance claims</span></div>
      </header>

      <section className="architecture" aria-labelledby="architecture-title">
        <div><p className="eyebrow">What this demonstrates</p><h2 id="architecture-title">Evidence does not become action by accident.</h2></div>
        <ol className="authority-flow"><li><b>D1</b><span>Macro gate</span></li><li><b>H1</b><span>Swing context</span></li><li><b>M5</b><span>Pullback & confirmation</span></li><li><b>Intent</b><span>Risk remains separate</span></li></ol>
      </section>

      <section id="replay" className="replay-shell" aria-labelledby="replay-title">
        <div className="section-heading"><div><p className="eyebrow">Guided replay</p><h2 id="replay-title">Choose an architecture outcome to inspect</h2></div><p>Each event is closed before observation. Move through the trace to see why the roadmap advances, waits or clears itself.</p></div>
        <div className="scenario-grid" role="list">
          {scenarios.map((scenario) => <button key={scenario.id} type="button" role="listitem" className={`scenario-card ${scenario.id === selected ? "selected" : ""}`} onClick={() => selectScenario(scenario.id)} aria-pressed={scenario.id === selected}><strong>{scenario.title}</strong><span>{scenario.objective}</span><small>Expected: {scenario.expected}</small></button>)}
        </div>
        <div className="replay-controls">
          <button className="primary" type="button" onClick={loadReplay} disabled={!selected || loadState === "loading"}>{loadState === "loading" ? "Loading replay…" : steps.length ? "Restart guided replay" : "Run the guided replay"}</button>
          <button type="button" onClick={() => setIndex((current) => Math.max(0, current - 1))} disabled={index <= 0}>Previous event</button>
          <button type="button" onClick={() => setIndex((current) => Math.min(steps.length - 1, current + 1))} disabled={!steps.length || index >= steps.length - 1}>Next event</button>
          <span className="progress">{steps.length ? `Event ${index + 1} of ${steps.length}` : "Replay not started"}</span>
        </div>
        <p className="status" aria-live="polite">{error || (step ? `${step.event.timeFrame} ${step.event.eventId}: ${reasonDescriptions[step.decision.reasonCodes[0]] ?? step.decision.reasonCodes[0]}` : "Select a scenario, then start the guided replay.")}</p>

        {step ? <section className="step-view" aria-label="Current replay event">
          <article className="decision-card"><p className="card-kicker">Current decision</p><h3>{step.phase}</h3><p className={`intent ${step.decision.intent}`}>{step.decision.intent}</p><p>{reasonDescriptions[step.decision.reasonCodes[0]] ?? step.decision.reasonCodes[0]}</p><dl><div><dt>Confidence</dt><dd>{Math.round(step.decision.confidence * 100)}%</dd></div><div><dt>Decision time</dt><dd>{displayTime(step.decision.decidedAt)}</dd></div></dl></article>
          <article><p className="card-kicker">Observed event</p><h3>{step.event.timeFrame} · {step.event.side ?? "No side"}</h3><dl><div><dt>Closed</dt><dd>{displayTime(step.event.closedAt)}</dd></div><div><dt>Observed</dt><dd>{displayTime(step.event.observedAt)}</dd></div><div><dt>Source</dt><dd>{step.event.source} / {step.event.sourceVersion}</dd></div></dl></article>
          <article><p className="card-kicker">Active evidence</p><h3>{step.evidence.length ? `${step.evidence.length} active item${step.evidence.length === 1 ? "" : "s"}` : "No active evidence"}</h3>{step.evidence.map((evidence) => <div className="evidence" key={evidence.evidenceId}><span>{evidence.kind}</span><b>{Math.round(evidence.strength * 100)}%</b></div>)}</article>
          <article className="timeline"><p className="card-kicker">Transition trace</p>{step.timeline.length ? step.timeline.map((transition, transitionIndex) => <div key={`${transition.reasonCode}-${transitionIndex}`}><span>{transition.from}</span><b>→</b><span>{transition.to}</span><small>{transition.reasonCode}</small></div>) : <p>No phase transition yet; the system is collecting or checking context.</p>}</article>
        </section> : <section className="empty"><h3>Start with aligned continuation</h3><p>It makes the authority sequence visible from macro context to a traceable intent.</p></section>}

        {completed && step ? <section className="completion" aria-labelledby="completion-title"><p className="eyebrow">Replay complete</p><h2 id="completion-title">{step.decision.intent === "Enter" ? "A traceable intent was produced — not an order." : "The setup remained governed by explicit invalidation rules."}</h2><p>Use this pattern when decision systems need evidence, authority boundaries and reviewable behavior before actions are allowed downstream.</p><div className="completion-actions"><a className="primary-link" href={portfolioLinks.contact}>Discuss a similar decision system</a><a href={portfolioLinks.caseStudy}>Back to Study Case 01</a></div></section> : null}
      </section>

      <footer>Engineering demonstration using synthetic semantic events. Risk validation and execution remain deliberately separate ports.</footer>
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<App />);
