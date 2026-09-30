# GHOST release guide

## Requirements

- Windows 10 or later, 64-bit (`win-x64`).
- Write access to `C:\ProgramData\GHOST`.
- A self-contained publish created by the command below does not require a separate .NET runtime.

## Build and package

Run the following from the repository root:

```powershell
dotnet restore GHOST.Presentation/GHOST.Presentation.csproj -r win-x64 --configfile NuGet.Config
dotnet publish GHOST.Presentation/GHOST.Presentation.csproj -c Release --self-contained true --no-restore
```

Package the contents of `GHOST.Presentation\bin\Release\net10.0-windows\win-x64\publish`. No installer project is included: use an installer or deployment system that installs the complete publish directory and allows the application to create its ProgramData directories. Do not install the database into the publish directory.

## First run and administration

On first launch, GHOST creates and migrates the SQLite database, seeds the ten devices (PS1–PS5, VIP 1–3, Ultra VIP 1–2), and displays the administrator setup dialog. Create the administrator account there. Passwords are stored as PBKDF2-SHA512 hashes with a random salt; never copy passwords into configuration files or logs.

## Data, logs, and backups

| Item | Location |
| --- | --- |
| Production database | `C:\ProgramData\GHOST\Data\ghost.db` |
| Application logs | `C:\ProgramData\GHOST\Logs\ghost-YYYYMMDD.log` |
| Recommended backups | a protected location outside the application and publish directories, for example `C:\ProgramData\GHOST\Backups` |

Use the backup service to create and validate a backup before every restore. Restore only while the application is closed and no process has an open database connection. The restore service first creates a `pre-restore` safety backup; an invalid backup is rejected and a failed replacement is rolled back from that safety copy.

## Updating

1. Close GHOST and confirm no GHOST process remains.
2. Create and validate a backup of `C:\ProgramData\GHOST\Data\ghost.db`.
3. Replace the application publish directory, preserving `C:\ProgramData\GHOST`.
4. Start GHOST and confirm migrations complete and the dashboard loads. Do not represent the current build as having a sign-in workflow; that workflow has not been implemented in the WPF client.

## Troubleshooting

- If startup reports a database error, inspect the log path above and validate the database or restore a known-good backup.
- If the first-run dialog does not appear, an administrator account is already present in the production database.
- If the app cannot create the database or logs, grant the installed user write access to `C:\ProgramData\GHOST`.

## Production checklist

- [ ] Publish the Release `win-x64` self-contained output.
- [ ] If self-contained publishing cannot restore the Windows runtime pack, install the matching .NET 10 Windows Desktop Runtime before using the framework-dependent fallback output.
- [ ] Install the whole publish directory outside `Program Files` write locations that contain data.
- [ ] Confirm database and logs are created under `C:\ProgramData\GHOST`.
- [ ] Complete administrator setup with a unique strong password.
- [ ] Verify all ten seeded devices are present.
- [ ] Configure a protected backup destination and verify a test restore before go-live.
- [ ] Run the full test suite and record its result for the release.

## Known limitations

- This build is **not approved for production release**. The WPF client currently exposes a read-only device dashboard; session controls are deliberately disabled and no sign-in workflow is present.
- Session cash payments are not recorded in `CashTransactions` or assigned to an open shift, so shift reconciliation omits them.
- Backup and restore contracts do not receive an actor identity and therefore cannot enforce service-layer authorization or audit the operation.
- There is no installer project, code signing configuration, or automatic backup scheduler in this repository; deployment packaging, signing, and backup scheduling remain operational responsibilities.
- A fully isolated first-run UI test needs a disposable Windows user/profile or VM because the production database location is intentionally fixed.
