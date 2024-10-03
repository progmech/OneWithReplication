using Amazon.S3;
using Amazon.S3.Model;
using OneWithReplication.Contracts;
using OneWithReplication.Settings;
using Serilog;

namespace OneWithReplication.Services;

public sealed class CloudService : ICloudService
{
    private readonly AmazonS3Client _s3Client;
    private readonly BackupSettings _settings;

    public CloudService(BackupSettings settings)
    {
        if (ValidationService.ValidateBackupSettings(settings, out string errorMessage))
        {
            _settings = settings;
            var configsS3 = new AmazonS3Config
            {
                ServiceURL = _settings.ServiceUrl,
            };

            _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, configsS3);
        }
        else
        {
            throw new Exception($"Неправильные настройки облачного провайдера! Следующие параметры не указаны: {errorMessage}");
        }
    }

    public void CleanupBucket(string bucketName, string objectName, int storageDepth)
    {
        Task<ListObjectsResponse> files = ListObjectsAsync(bucketName);
        foreach (S3Object? obj in files.Result.S3Objects)
        {
            if (!obj.Key.StartsWith(objectName) || obj.LastModified >= DateTime.Today.AddDays(-storageDepth)) continue;

            Task<DeleteObjectResponse> response = DeleteObjectAsync(bucketName, obj.Key);
            if (response.Result.HttpStatusCode == System.Net.HttpStatusCode.NoContent && response.IsCompletedSuccessfully)
            {
                Log.Information("Файл {Key} удалён из хранилища {Name}", obj.Key, bucketName);
            }
            else
            {
                throw new Exception($"Ошибка при удалении файла {obj.Key} из хранилища {bucketName}");
            }
        }
    }

    public void CompareChecksum(string bucketName, string archiveName)
    {
        string sourceCheckSum;
        using (FileStream fop = File.OpenRead(archiveName))
        {
            sourceCheckSum = BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(fop)).Replace("-", string.Empty);
        }
        Task<ListObjectsResponse> files = ListObjectsAsync(bucketName);
        foreach (S3Object? obj in files.Result.S3Objects)
        {
            if (obj.Key == Path.GetFileName(archiveName))
            {
                string targetCheckSum = obj.ETag.ToUpper().Replace("\"", string.Empty);
                if (sourceCheckSum != targetCheckSum)
                {
                    throw new Exception($"Контрольные суммы файла {archiveName} и файла {obj.Key} в облаке {bucketName} не совпадают!");
                }
            }
        }
    }

    public async Task<ListBucketsResponse> ListBucketsAsync()
    {
        var response = await _s3Client.ListBucketsAsync();
        return response;
    }

    public async Task<ListObjectsResponse> ListObjectsAsync(string bucketName)
    {
        var request = new ListObjectsRequest
        {
            BucketName = bucketName
        };
        ListObjectsResponse? response = await _s3Client.ListObjectsAsync(request);
        return response;
    }

    public async Task<PutObjectResponse> PutObjectAsync(string bucketName, string archiveName)
    {
        string chksum;
        using (FileStream fop = File.OpenRead(archiveName))
        {
            chksum = BitConverter.ToString(System.Security.Cryptography.SHA1.Create().ComputeHash(fop));
        }
        PutObjectRequest request = new()
        {
            BucketName = bucketName,
            Key = Path.GetFileName(archiveName),
            UseChunkEncoding = false,
            FilePath = archiveName,
            ChecksumAlgorithm = ChecksumAlgorithm.SHA1,
            ChecksumSHA1 = chksum + 5
        };
        PutObjectResponse? response = await _s3Client.PutObjectAsync(request);
        return response;
    }

    private async Task<DeleteObjectResponse> DeleteObjectAsync(string bucketName, string key)
    {
        DeleteObjectResponse? response = await _s3Client.DeleteObjectAsync(bucketName, key);
        return response;
    }
}