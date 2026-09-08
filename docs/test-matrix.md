# Verification matrix

| Concern | Automated evidence | Next production extension |
|---|---|---|
| Closed candle / no look-ahead | `Future_close_is_rejected_without_transition` | Prefix replay stability property test |
| Idempotency | `Duplicate_is_idempotently_rejected` | Persist cursor across restart |
| Phase traceability | `Closed_sequence_reaches_entry_ready_with_trace` | Snapshot transition table |
| Batch/incremental parity | Same ordered `Process` core | Golden dataset comparison |
| Out-of-order handling | Domain reason code | Add explicit reorder-buffer adapter |
| Expiration | TTL removal in orchestrator | Virtual-clock boundary tests |
| Risk separation | No order contract in decision | Implement and property-test risk port |
