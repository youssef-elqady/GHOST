# Architecture

GHOST uses a layered .NET 10 architecture: Presentation (WPF) depends on Application and Infrastructure; Infrastructure implements persistence and depends on Application and Domain; Application depends on Domain. Domain has no infrastructure or UI dependencies.

`AppDbContext` is the sole EF Core context. Business-sensitive first-run administration is exposed through an application interface and implemented in Infrastructure. The UI never accesses SQLite directly.
