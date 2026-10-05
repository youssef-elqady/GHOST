using GHOST.Application.Day6;
using GHOST.Application.Authentication;
using GHOST.Domain.Entities;
using GHOST.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Serilog;

namespace GHOST.Infrastructure.Day6;

public sealed class SqliteBackupService(
    AppDbContext dbContext,
    ICurrentUserContext currentUser) : IBackupService
{
    public async Task<string> CreateBackupAsync(
        string db,
        BackupOptions options,
        CancellationToken ct = default)
    {
        var actor = Require("Manager");

        if (!await ValidateAsync(db, ct))
            throw new InvalidOperationException("Source database is invalid.");

        Directory.CreateDirectory(options.DirectoryPath);

        var target = await CreateValidatedBackupFileAsync(
            db,
            options.DirectoryPath,
            ct);

        var archivePath = await ArchiveBackupAsync(
            target,
            options,
            ct);

        Rotate(options);

        await Audit(actor.Id, "BackupCreated", "Backup", target, ct);

        await Audit(actor.Id, "BackupArchived", "Backup", archivePath, ct);

        Log.Information(
            "Database backup created at {BackupPath}; archive at {ArchivePath}",
            target,
            archivePath);

        return target;
    }

    public async Task<bool> ValidateAsync(
        string path,
        CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            await using var connection = new SqliteConnection(
                $"Data Source={path};Mode=ReadOnly;Pooling=False");

            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = "PRAGMA integrity_check;";
            var integrity = (string?)await command.ExecuteScalarAsync(ct);

            if (integrity != "ok")
                return false;

            command.CommandText =
                "SELECT COUNT(*) FROM sqlite_master " +
                "WHERE type='table' AND name IN ('Devices','Sessions','Users');";

            return Convert.ToInt32(
                await command.ExecuteScalarAsync(ct)) == 3;
        }
        catch
        {
            return false;
        }
    }

    public async Task RestoreAsync(
        string db,
        string backup,
        BackupOptions options,
        CancellationToken ct = default)
    {
        var actor = Require("Admin");

        if (!await ValidateAsync(backup, ct))
            throw new InvalidOperationException("Selected backup is invalid.");

        var safety = await CreateBackupAsyncInternal(
            db,
            Path.Combine(options.DirectoryPath, "pre-restore"),
            ct);

        var stage = db + ".restore-stage";

        try
        {
            File.Copy(backup, stage, true);

            if (!await ValidateAsync(stage, ct))
                throw new InvalidOperationException("Staged restore is invalid.");

            File.Copy(stage, db, true);

            if (!await ValidateAsync(db, ct))
                throw new InvalidOperationException(
                    "Restored database verification failed.");

            await Audit(actor.Id, "BackupRestored", "Backup", backup, ct);

            Log.Information(
                "Database restored from {BackupPath}; prior database saved as {SafetyBackup}",
                backup,
                safety);
        }
        catch
        {
            if (File.Exists(safety))
                File.Copy(safety, db, true);

            throw;
        }
        finally
        {
            if (File.Exists(stage))
                File.Delete(stage);
        }
    }

    private async Task<string> CreateBackupAsyncInternal(
        string db,
        string directoryPath,
        CancellationToken ct)
    {
        if (!await ValidateAsync(db, ct))
            throw new InvalidOperationException("Source database is invalid.");

        Directory.CreateDirectory(directoryPath);

        return await CreateValidatedBackupFileAsync(
            db,
            directoryPath,
            ct);
    }

    private static async Task<string> CreateValidatedBackupFileAsync(
        string db,
        string directoryPath,
        CancellationToken ct)
    {
        var target = Path.Combine(
            directoryPath,
            $"ghost-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.db");

        await using (var source =
            new SqliteConnection($"Data Source={db};Pooling=False"))
        {
            await source.OpenAsync(ct);

            await using var destination =
                new SqliteConnection($"Data Source={target};Pooling=False");

            await destination.OpenAsync(ct);
            source.BackupDatabase(destination);
        }

        if (!File.Exists(target))
            throw new InvalidOperationException("Backup file was not created.");

        if (!await ValidateFileAsync(target, ct))
        {
            File.Delete(target);
            throw new InvalidOperationException("Backup validation failed.");
        }

        return target;
    }

    private async Task<string> ArchiveBackupAsync(
        string backupPath,
        BackupOptions options,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var archiveRoot = options.ArchiveDirectoryPath;

        if (string.IsNullOrWhiteSpace(archiveRoot))
            archiveRoot = Path.Combine(options.DirectoryPath, "archive");

        var now = DateTimeOffset.UtcNow;

        var archiveDirectory = Path.Combine(
            archiveRoot,
            now.ToString("yyyy"),
            now.ToString("MM"));

        Directory.CreateDirectory(archiveDirectory);

        var archivePath = Path.Combine(
            archiveDirectory,
            Path.GetFileName(backupPath));

        File.Copy(backupPath, archivePath, false);

        if (!await ValidateFileAsync(archivePath, ct))
        {
            File.Delete(archivePath);
            throw new InvalidOperationException(
                "Archived backup validation failed.");
        }

        return archivePath;
    }

    private static async Task<bool> ValidateFileAsync(
        string path,
        CancellationToken ct)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            await using var connection = new SqliteConnection(
                $"Data Source={path};Mode=ReadOnly;Pooling=False");

            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = "PRAGMA integrity_check;";
            var integrity = (string?)await command.ExecuteScalarAsync(ct);

            if (integrity != "ok")
                return false;

            command.CommandText =
                "SELECT COUNT(*) FROM sqlite_master " +
                "WHERE type='table' AND name IN ('Devices','Sessions','Users');";

            return Convert.ToInt32(
                await command.ExecuteScalarAsync(ct)) == 3;
        }
        catch
        {
            return false;
        }
    }

    private AuthenticatedUser Require(string role)
    {
        var actor = currentUser.RequireAuthenticated();

        var allowed =
            role == "Manager"
                ? actor.IsInRole("Manager") || actor.IsInRole("Admin")
                : actor.IsInRole("Admin");

        if (!allowed)
            throw new UnauthorizedAccessException("Operation is not authorized.");

        return actor;
    }

    private async Task Audit(
        Guid actorId,
        string action,
        string type,
        string value,
        CancellationToken ct)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = type,
            NewValue = value
        });

        await dbContext.SaveChangesAsync(ct);
    }

    private static void Rotate(BackupOptions options)
    {
        var files = new DirectoryInfo(options.DirectoryPath)
            .GetFiles("ghost-*.db")
            .OrderByDescending(x => x.CreationTimeUtc)
            .ToList();

        foreach (var file in files.Skip(Math.Max(1, options.RetentionCount)))
            file.Delete();
    }
}