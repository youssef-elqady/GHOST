namespace GHOST.Infrastructure.Persistence;

public static class GhostDatabase
{
    public const string DefaultPath = @"C:\ProgramData\GHOST\Data\ghost.db";
    public static string GetConnectionString(string? databasePath = null)
    {
        var path = databasePath ?? DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Database path must include a directory."));
        return $"Data Source={path};Foreign Keys=True";
    }
}
