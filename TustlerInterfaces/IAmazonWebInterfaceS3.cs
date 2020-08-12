using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TustlerInterfaces
{
    public interface IAmazonWebInterfaceS3
    {
        public abstract Task<AWSResult<List<S3Bucket>>> ListBuckets();
        public abstract Task<AWSResult<List<S3Object>>> ListBucketItems(string bucketName);
        public abstract Task<AWSResult<MetadataCollection>> GetItemMetadata(string bucketName, string key);
        public abstract Task<AWSResult<(bool?, string)>> DeleteBucketItem(string bucketName, string key);
        public abstract Task<AWSResult<(bool?, string)>> UploadItem(string bucketName, string newKey, string filePath, string mimetype, string extension);
        public abstract Task<AWSResult<(bool?, string)>> DownloadItemToFile(string bucketName, string key, string filePath);
        public abstract Task<AWSResult<Stream>> DownloadItemAsStream(string bucketName, string key);
    }
}
