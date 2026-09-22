# Database

The production SQLite database is `C:\ProgramData\GHOST\Data\ghost.db`. Its directory is created before connection. SQLite foreign keys are enabled in the connection string.

The `InitialCreate` migration configures required foreign keys, unique device, role, room, and user names, and an indexed customer phone number. `DatabaseInitializer` applies migrations and idempotently seeds the ten required devices and Admin, Manager, and Cashier roles.
