# Testing

Day 1 database tests use SQLite in-memory connections with foreign-key enforcement and EF migrations. They cover schema creation, seed data, unique device names, session/pause relationships, and payment foreign keys.

Day 2 tests cover the device action guard, Admin/Manager authorization boundary, device and room administration, and dashboard data projections.

Day 3 tests cover start/pause/resume/end transitions, pause persistence, frozen rates, billing rounding and minimum duration, cash payment rules, duplicate-start protection, authorization, transaction rollback behavior, and reconstruction of an active session from persisted SQLite timestamps after a new DbContext is created.
