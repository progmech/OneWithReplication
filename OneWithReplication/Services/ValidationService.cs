using System.Text;
using OneWithReplication.Settings;

namespace OneWithReplication.Services;

public static class ValidationService
{
    public static bool ValidateEmailSettings(EmailSettings settings, out string errorMessage)
    {
        if (settings is null)
        {
            errorMessage = "Все настройки почтового сервиса.";
            return false;
        }

        var builder = new StringBuilder();

        if (string.IsNullOrEmpty(settings.SmtpServer))
        {
            builder.Append($"{nameof(settings.SmtpServer)}, ");
        }
        if (string.IsNullOrEmpty(settings.UserName))
        {
            builder.Append($"{nameof(settings.UserName)}, ");
        }
        if (string.IsNullOrEmpty(settings.Password))
        {
            builder.Append($"{nameof(settings.Password)}, ");
        }
        if (string.IsNullOrEmpty(settings.From))
        {
            builder.Append($"{nameof(settings.From)}, ");
        }
        if (string.IsNullOrEmpty(settings.To))
        {
            builder.Append($"{nameof(settings.To)}.");
        }

        errorMessage = builder.ToString();

        return errorMessage.Length == 0;
    }

    public static bool ValidateDatabaseSettings(DatabaseSettings settings, out string errorMessage)
    {
        if (settings is null)
        {
            errorMessage = "Все настройки базы данных.";
            return false;
        }

        var builder = new StringBuilder();

        if (string.IsNullOrEmpty(settings.DatabasePath))
        {
            builder.Append($"{nameof(settings.DatabasePath)}, ");
        }

        if (string.IsNullOrEmpty(settings.DatabaseName))
        {
            builder.Append($"{nameof(settings.DatabaseName)}, ");
        }

        if (string.IsNullOrEmpty(settings.BackupPath))
        {
            builder.Append($"{nameof(settings.BackupPath)}, ");
        }

        if (string.IsNullOrEmpty(settings.BackupName))
        {
            builder.Append($"{nameof(settings.BackupName)}.");
        }

        errorMessage = builder.ToString();

        return errorMessage.Length == 0;
    }

    public static bool ValidateBackupSettings(BackupSettings settings, out string errorMessage)
    {
        var builder = new StringBuilder();

        if (!ValidateEmailSettings(settings.EmailSettings, out string? settingsError))
        {
            builder.Append(settingsError);
        }

        foreach (var databaseSetting in settings.DatabaseSettings)
        {
            if (!ValidateDatabaseSettings(databaseSetting, out settingsError))
            {
                builder.Append(settingsError);
            }
        }

        if (string.IsNullOrEmpty(settings.ServiceUrl))
        {
            builder.Append($"{nameof(settings.ServiceUrl)}.");
        }

        if (string.IsNullOrEmpty(settings.AccessKey))
        {
            builder.Append($"{nameof(settings.AccessKey)}.");
        }

        if (string.IsNullOrEmpty(settings.SecretKey))
        {
            builder.Append($"{nameof(settings.SecretKey)}.");
        }

        if (string.IsNullOrEmpty(settings.BucketName))
        {
            builder.Append($"{nameof(settings.BucketName)}.");
        }

        errorMessage = builder.ToString();

        return errorMessage.Length == 0;
    }
}