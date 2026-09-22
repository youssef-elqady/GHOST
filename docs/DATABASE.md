# Database

The production SQLite database is `C:\ProgramData\GHOST\Data\ghost.db`. Its directory is created before connection. SQLite foreign keys are enabled in the connection string.

The `InitialCreate` migration configures required foreign keys, unique device, role, room, and user names, and an indexed customer phone number. `DatabaseInitializer` applies migrations and idempotently seeds the ten required devices and Admin, Manager, and Cashier roles.

The `AddSessionIntegrityAndDeviceRates` migration adds each device's current hourly rate and a session `IsActive` marker. A filtered unique index on active device sessions prevents duplicate active sessions at the SQLite boundary; a unique payment-session index prevents duplicate final settlement. Session records preserve their resolved rate and finalized total independently of future device-rate changes.

`AddCustomerRewardsAndAudit` adds Discounts, LoyaltyTransactions, Gifts, GiftCards, Offers, and AuditLogs. It enforces unique gift-card codes, one discount record per session, and indexes customer/period gift history and audit lookup fields.

`AddInventoryOrdersAndShifts` adds categories, products, inventory transactions, orders/items, shifts, and cash transactions. Product names and category names are unique; a filtered unique open-shift index enforces the single-open-shift rule.

Production backups use SQLite's online backup API and are saved as timestamped files outside the database directory. Each backup passes `PRAGMA integrity_check` and confirms core schema tables. Restore validates the selected backup, retains a separate pre-restore safety copy, stages the replacement, and verifies the restored file before completion.
