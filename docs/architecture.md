# Architecture

## Invariants and assumptions
- Timeframes: Execution M5, Swing H1, Macro D1.
- Only closed observations are accepted.
- `ObservedAt` never precedes `ClosedAt`; decision time equals or follows observation.
- Synthetic semantic events are the public input boundary.
- Rules are versioned; persistence and exchange mechanisms are outside the domain.

## Component boundaries
`DataSource → TimeFrameProcessor → EventNormalizer → EvidenceAccumulator → RoadmapOrchestrator → DecisionFilter → RiskValidator → ExecutionAdapter`

The sample implements the contracts and orchestrator; data, risk, and execution are documented extension ports. No downstream component may reach backward into dataframes, exchange adapters, or global state.

## Phase model
| Phase | Trigger | Next | Expiration/invalidation |
|---|---|---|---|
| WaitContext | Swing BOS | MonitorPullback | Opposing context |
| MonitorPullback | Pullback at POI | WaitConfirmation | 8h swing evidence |
| WaitConfirmation | Aligned internal BOS | EntryReady | 45m pullback evidence |
| EntryReady | Separate risk acceptance | InPosition | 20m execution evidence |
| Any setup | SetupInvalidated | WaitContext | Clears active evidence |

## Entry, risk, and order
The orchestrator produces `Intent.Enter`; a future `RiskValidator` must resolve stop hierarchy, minimum distance, reward/risk, size, margin, and exposure before an `ExecutionAdapter` receives anything. A rejected risk check never becomes a partial order.

## Operational parity
Backtest, replay, paper, and realtime must call `RoadmapOrchestrator.Process`. Only clock, source, persistence, and broker adapters vary.
