# Testing

Day 1 database tests use SQLite in-memory connections with foreign-key enforcement and EF migrations. They cover schema creation, seed data, unique device names, session/pause relationships, and payment foreign keys.

Day 2 tests cover the device action guard, Admin/Manager authorization boundary, device and room administration, and dashboard data projections.

Day 3 tests cover start/pause/resume/end transitions, pause persistence, frozen rates, billing rounding and minimum duration, cash payment rules, duplicate-start protection, authorization, transaction rollback behavior, and reconstruction of an active session from persisted SQLite timestamps after a new DbContext is created.

Day 4 tests cover customer phone search and uniqueness, blocking, discount approval authorization, loyalty ledger constraints, period-limited gifts, gift-card redemption, admin-only offers, and audit records.

Day 5 tests cover stock mutation tracing, negative-stock prevention, POS atomicity, historical price snapshots, product availability, cash transactions, opening/closing shifts, and discrepancy reasons.
