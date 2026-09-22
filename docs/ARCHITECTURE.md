# Architecture

GHOST uses a layered .NET 10 architecture: Presentation (WPF) depends on Application and Infrastructure; Infrastructure implements persistence and depends on Application and Domain; Application depends on Domain. Domain has no infrastructure or UI dependencies.

`AppDbContext` is the sole EF Core context. Business-sensitive first-run administration is exposed through an application interface and implemented in Infrastructure. The UI never accesses SQLite directly.

Day 2 uses `IDeviceDashboardService` for read-only dashboard projections and `IDeviceAdministrationService` for mutations. Device and room mutations require an active Admin or Manager role in Infrastructure; the unauthenticated WPF foundation keeps administration controls disabled until login is introduced.

Day 3 adds `ISessionService`, implemented transactionally in Infrastructure. It owns device/session state transitions, pause persistence, final billing, cash settlement, and authorization for Cashier, Manager, and Admin roles. `IBillingCalculator` is deterministic and independent of UI clocks; persisted session timestamps, frozen `RatePerHour`, and persisted pauses remain the source of truth after restart.

Day 4 adds `IDay4Service` for customer profiles/search, discount approval, loyalty ledger entries, gifts, gift cards, offers, and immutable audit events. Authorization is evaluated in the service layer; ViewModels do not access EF Core directly.

Day 5 adds `IDay5Service`, which performs product, inventory, order, cash, and shift mutations in EF Core transactions. POS completion snapshots item prices and writes stock, order, inventory, cash, and audit records atomically.

Day 6 adds read-only `IReportingService` projections for database-backed management metrics and `IBackupService` for validated SQLite online backups and staged restores. Reports use async projections and do not expose persistence to WPF ViewModels.
