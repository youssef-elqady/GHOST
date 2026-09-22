# Database

The production SQLite database is `C:\ProgramData\GHOST\Data\ghost.db`. Its directory is created before connection. SQLite foreign keys are enabled in the connection string.

The `InitialCreate` migration configures required foreign keys, unique device, role, room, and user names, and an indexed customer phone number. `DatabaseInitializer` applies migrations and idempotently seeds the ten required devices and Admin, Manager, and Cashier roles.

The `AddSessionIntegrityAndDeviceRates` migration adds each device's current hourly rate and a session `IsActive` marker. A filtered unique index on active device sessions prevents duplicate active sessions at the SQLite boundary; a unique payment-session index prevents duplicate final settlement. Session records preserve their resolved rate and finalized total independently of future device-rate changes.
