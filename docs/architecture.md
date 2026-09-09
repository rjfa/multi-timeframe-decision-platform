# Architecture

## Invariants and assumptions

- Timeframes: Macro D1, Swing H1 and Execution M5.
- A macro D1 bias is an explicit gate: a swing BOS can only establish strategic context when its side matches active macro evidence.
- Only closed observations are accepted: `ClosedAt <= ObservedAt`.
- Events are processed monotonically by `ObservedAt`; an out-of-order event is rejected before it is recorded as processed.
- Immutable synthetic semantic events are the public input boundary.
- Evidence expires under the typed `RoadmapTimingPolicy`: macro 24h, swing 8h, pullback 45m and execution confirmation 20m.
- Expiration reconciles the roadmap to the highest phase whose required evidence remains active. It cannot leave a phase advanced by expired evidence.
- Rules are versioned; persistence and exchange mechanisms are outside the domain.

## Component boundaries

`DataSource → TimeFrameProcessor → EventNormalizer → EvidenceAccumulator → RoadmapOrchestrator → DecisionFilter → RiskValidator → ExecutionAdapter`

The sample implements the contracts and orchestrator. Data, risk and execution remain documented extension ports. No downstream component may reach backward into dataframes, exchange adapters or global state.

## Phase model

| Phase | Trigger | Next | Required active evidence / recovery |
|---|---|---|---|
| WaitContext | Macro-aligned Swing BOS | MonitorPullback | Macro D1 + swing H1; missing macro/swing resets here |
| MonitorPullback | Pullback at POI | WaitConfirmation | Macro + swing + pullback; expired pullback returns here |
| WaitConfirmation | Aligned internal BOS | EntryReady | Macro + swing + pullback + execution confirmation |
| EntryReady | Position closed | Cooldown | Expired execution confirmation returns to WaitConfirmation |
| Any setup | SetupInvalidated | WaitContext | Clears active evidence; produces `Exit` only from `InPosition` |

A macro context change that conflicts with an active setup clears that setup and returns to `WaitContext`.

## Entry, risk and order

The orchestrator produces `Intent.Enter`; a future `RiskValidator` must resolve stop hierarchy, minimum distance, reward/risk, size, margin and exposure before an `ExecutionAdapter` receives anything. A rejected risk check never becomes a partial order.

## Operational parity

Backtest, replay, paper and realtime must call `RoadmapOrchestrator.Process`. Only clock, source, persistence and broker adapters vary.
