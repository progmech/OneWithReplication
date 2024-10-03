namespace OneWithReplication.Settings;

public class BackupSettings
{
    public int ArchiveDepthInDays { get; init; } = 30;
    public string BucketName { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string ServiceUrl { get; init; } = string.Empty;
    public EmailSettings EmailSettings { get; init; } = new();
    public List<DatabaseSettings> DatabaseSettings { get; init; } = new();
}