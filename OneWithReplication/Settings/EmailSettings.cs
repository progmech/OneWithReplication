namespace OneWithReplication.Settings;

public class EmailSettings
{
    public string SmtpServer { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public int Port { get; init; } = 25;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}