using System.IO.Compression;
using Amazon.S3.Model;
using OneWithReplication.Contracts;
using OneWithReplication.Settings;
using Serilog;

namespace OneWithReplication;

public class Replicator
{
    private readonly IEmailService _emailService = default!;
    private readonly ICloudService _cloudService = default!;
    private readonly BackupSettings _backupOptions = default!;

    public Replicator(BackupSettings backupOptions, IEmailService emailServicer, ICloudService cloudService)
    {
        try
        {
            _emailService = emailServicer;
            _cloudService = cloudService;
            _backupOptions = backupOptions;
        }
        catch (Exception ex)
        {
            Log.Error("Ошибка инициализации в конструкторе: {Message}", ex.Message);
            _emailService?.SendMessage($"Ошибка инициализации в конструкторе: {ex.Message}");
        }
    }

    public void Replicate()
    {
        Log.Information("Резервное копирование начато {Time}.", DateTime.Now);
        foreach (DatabaseSettings databaseOption in _backupOptions.DatabaseSettings)
        {
            if (!databaseOption.IsActive)
            {
                continue;
            }
            try
            {
                Backup(databaseOption);
                Cleanup(databaseOption);
            }
            catch (Exception ex)
            {
                Log.Error("{Message}", ex.Message);
                _emailService.SendMessage(ex.Message);
            }
        }
        Log.Information("Резервное копирование закончено {Time}.", DateTime.Now);
    }

    private void Backup(DatabaseSettings databaseOption)
    {
        CheckPathsExist(databaseOption, out string copyFrom, out string copyTo, out string archiveTo);
        CheckBucketExists(_backupOptions.BucketName);
        CreateDatabaseCopy(copyFrom, copyTo);
        string archiveName = ArchiveDatabaseCopy(copyTo, archiveTo, databaseOption.DatabaseName);
        CopyArchiveToCloud(_backupOptions.BucketName, archiveName);
        _cloudService.CompareChecksum(_backupOptions.BucketName, archiveName);
    }

    private void Cleanup(DatabaseSettings databaseOption)
    {
        CleanupBackupFolder(databaseOption);
        _cloudService.CleanupBucket(_backupOptions.BucketName, databaseOption.BackupName,
            _backupOptions.ArchiveDepthInDays);
    }

    private void CleanupBackupFolder(DatabaseSettings dbSetting)
    {
        string fullFileName = Path.Combine(dbSetting.BackupPath, dbSetting.BackupName);
        foreach (var file in Directory.EnumerateFiles(dbSetting.BackupPath).Where(f => f.StartsWith(fullFileName)))
        {
            try
            {
                if (File.GetCreationTime(file) < DateTime.Today.AddDays(-1 * _backupOptions.ArchiveDepthInDays))
                {
                    File.Delete(file);
                    Log.Information("Файл {File} удалён из папки {BackupPath}.", file, dbSetting.BackupPath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка {ex.Message} при удалении файла {file}!");
            }
        }
    }

    private void CheckBucketExists(string bucketName)
    {
        Task<ListBucketsResponse> response = _cloudService.ListBucketsAsync();
        if (!response.Result.Buckets.Any(b => b.BucketName == bucketName))
        {
            throw new Exception($"Каталога {bucketName} не существует в облаке!");
        }
    }

    private void CopyArchiveToCloud(string bucketName, string archiveName)
    {
        Task<PutObjectResponse> putResponse = _cloudService.PutObjectAsync(bucketName, archiveName);
        if (putResponse.Result.HttpStatusCode == System.Net.HttpStatusCode.OK && putResponse.IsCompletedSuccessfully)
        {
            Log.Information("Файл {Archive} скопирован в хранилище {Bucket}", archiveName, bucketName);
        }
        else
        {
            throw new Exception($"Ошибка при копировании файла {archiveName} в хранилище {bucketName}");
        }
    }

    private void CheckPathsExist(DatabaseSettings databaseOption, out string copyFrom, out string copyTo, out string archivePath)
    {

        copyFrom = Path.Combine(databaseOption.DatabasePath, databaseOption.DatabaseName);
        copyTo = Path.Combine(databaseOption.BackupPath, databaseOption.DatabaseName);
        archivePath = Path.Combine(databaseOption.BackupPath, databaseOption.BackupName);
    }

    private void CreateDatabaseCopy(string copyFrom, string copyTo)
    {
        if (!File.Exists(copyFrom))
        {
            throw (new Exception($"Файл базы данных {copyFrom} не существует!"));
        }
        DateTime copyToTime = File.GetCreationTime(copyTo);
        DateTime copyFromTime = File.GetCreationTime(copyFrom);
        if (File.Exists(copyTo) && copyToTime != copyFromTime)
        {
            File.Delete(copyTo);
            Log.Information("Файл {CopyTo} с датой создания {CopyToTime} удалён.", copyTo, copyToTime);
        }
        File.Copy(copyFrom, copyTo);
        Log.Information("База данных {CopyFrom} скопирована в {CopyTo}.", copyFrom, copyTo);
    }

    private string ArchiveDatabaseCopy(string sourcePath, string archiveTo, string fileName)
    {
        DateTime today = DateTime.Today;
        var archivePath = $"{archiveTo}-{today:yyyy}{today:MM}{today:dd}.zip";
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }
        using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(sourcePath, fileName);
        }
        Log.Information("База данных {SourcePath} заархивирована в {ArchivePath}.", sourcePath, archivePath);
        return archivePath;
    }
}