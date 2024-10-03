using Amazon.S3.Model;

namespace OneWithReplication.Contracts;

public interface ICloudService
{
    Task<ListBucketsResponse> ListBucketsAsync();
    Task<ListObjectsResponse> ListObjectsAsync(string bucketName);
    Task<PutObjectResponse> PutObjectAsync(string bucketName, string archiveName);
    void CleanupBucket(string bucketName, string objectName, int storageDepth);
    void CompareChecksum(string bucketName, string archiveName);
}
