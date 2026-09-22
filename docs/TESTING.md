# Testing

Day 1 database tests use SQLite in-memory connections with foreign-key enforcement and EF migrations. They cover schema creation, seed data, unique device names, session/pause relationships, and payment foreign keys.

Day 2 tests cover the device action guard, Admin/Manager authorization boundary, device and room administration, and dashboard data projections.
