# Verification matrix

| Concern | Automated evidence | Next production extension |
|---|---|---|
| Closed candle / no look-ahead | `Future_close_is_rejected_without_transition` | Prefix replay stability property test |
| Macro authority gate | `Swing_requires_matching_macro_context` and `Macro_aligned_sequence_reaches_entry_ready_with_trace` | Timeframe processor contract test |
| Idempotency | `Duplicate_is_idempotently_rejected` | Persist cursor across restart |
| Out-of-order handling | `Out_of_order_event_is_rejected_before_it_is_recorded_as_processed` | Explicit reorder-buffer adapter |
| Evidence expiration | `Expired_swing_context_resets_roadmap_before_pullback`, `Expired_macro_context_resets_roadmap_before_swing_expiry`, `Expired_pullback_returns_roadmap_to_monitor_pullback` and `Expired_execution_confirmation_returns_roadmap_to_wait_confirmation` | Boundary/property tests for TTL combinations |
| Invalidated setup | `Invalidated_setup_clears_active_evidence_without_emitting_exit_for_preparation` | State-history/audit projection |
| Position lifecycle | `Position_closed_moves_entry_ready_to_cooldown` | Risk port and in-position transition |
| Phase traceability | `Macro_aligned_sequence_reaches_entry_ready_with_trace` | Snapshot transition table |
| Batch/incremental parity | `Batch_and_incremental_replay_produce_the_same_trace` | Golden dataset comparison |
| Risk separation | No order contract in decision | Implement and property-test risk port |


