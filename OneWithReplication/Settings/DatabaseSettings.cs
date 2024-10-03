namespace OneWithReplication.Settings;

public class DatabaseSettings
{
    public const string DatabaseOptionsName = "Database";
    public string DatabasePath { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public string BackupPath { get; init; } = string.Empty;
    public string BackupName { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}