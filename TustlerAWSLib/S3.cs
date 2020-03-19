using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib
{
    // AmazonS3Exception

    public class S3
    {
        /// <summary>
        /// Get a list of all buckets and their creation times
        /// </summary>
        /// <returns></returns>
        public async static Task<AWSResult<List<S3Bucket>>> ListBuckets()
        {
            try
            {
                using (var client = new AmazonS3Client())
                {
                    var response = await client.ListBucketsAsync();
                    return new AWSResult<List<S3Bucket>>(response.Buckets, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<S3Bucket>>(null, new AWSException("ListBuckets", "Not connected.", ex));
            }
        }

        /// <summary>
        /// Get a list of bucket items for the specified bucket
        /// </summary>
        /// <param name="bucketName">The name of the bucket to fetch items from</param>
        /// <returns></returns>
        public async static Task<AWSResult<List<S3Object>>> ListBucketItems(string bucketName)
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 10
                };
                ListObjectsV2Response response;
                using (var client = new AmazonS3Client())
                {
                    var combinedResponse = new List<S3Object>(request.MaxKeys);
                    do
                    {
                        response = await client.ListObjectsV2Async(request);

                        // Process the response.
                        foreach (S3Object entry in response.S3Objects)
                        {
                            combinedResponse.Add(new S3Object {
                                BucketName = entry.BucketName,
                                Key = entry.Key,
                                Owner = entry.Owner,
                                ETag = entry.ETag,
                                LastModified = entry.LastModified,
                                Size = entry.Size,
                                StorageClass = entry.StorageClass
                            });
                        }
                        request.ContinuationToken = response.NextContinuationToken;
                    } while (response.IsTruncated);

                    return new AWSResult<List<S3Object>>(combinedResponse, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<S3Object>>(null, new AWSException("ListBucketItems", "Not connected.", ex));
            }
        }

        public async static Task<AWSResult<MetadataCollection>> GetItemMetadata(string bucketName, string key)
        {
            try
            {
                using (var client = new AmazonS3Client())
                {
                    var response = await client.GetObjectMetadataAsync(bucketName, key);

                    return new AWSResult<MetadataCollection>(response.Metadata, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<MetadataCollection>(null, new AWSException("GetItemMetadata", "Not connected.", ex));
            }
        }

        public async static Task<AWSResult<bool?>> DeleteBucketItem(string bucketName, string key)
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                using (var client = new AmazonS3Client())
                {
                    var response = await client.DeleteObjectAsync(deleteObjectRequest);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent) // default return code
                    {
                        return new AWSResult<bool?>(null, new AWSException("DeleteBucketItem", $"Request returned status code: {response.HttpStatusCode}", null));
                    }
                    else
                    {
                        return new AWSResult<bool?>(true, null);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<bool?>(null, new AWSException("DeleteBucketItem", "Not connected.", ex));
            }
        }

        /// <summary>
        /// Upload a file to an S3 bucket
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file to upload</param>
        /// <param name="mimetype">The mimetype of the file (may be null)</param>
        /// <param name="extension">The file extension of the file (may be null)</param>
        /// <returns></returns>
        public async static Task<AWSResult<bool?>> UploadItem(string bucketName, string filePath, string mimetype, string extension)
        {
            try
            {
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    FilePath = filePath,
                    StorageClass = S3StorageClass.Standard,
                };
                if (!string.IsNullOrEmpty(mimetype))
                {
                    fileTransferUtilityRequest.Metadata.Add("mimetype", mimetype);
                }
                if (!string.IsNullOrEmpty(extension))
                {
                    fileTransferUtilityRequest.Metadata.Add("extension", extension);
                }

                using (var client = new AmazonS3Client())
                {
                    var fileTransferUtility = new TransferUtility(client);

                    await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                    return new AWSResult<bool?>(true, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<bool?>(null, new AWSException("UploadItem", "Not connected.", ex));
            }
        }

        public async static Task<AWSResult<bool?>> DownloadItem(string bucketName, string key, string filePath)
        {
            try
            {
                var fileTransferUtilityRequest = new TransferUtilityDownloadRequest
                {
                    FilePath = filePath,
                    BucketName = bucketName,
                    Key = key,
                };

                using (var client = new AmazonS3Client())
                {
                    var fileTransferUtility = new TransferUtility(client);

                    await fileTransferUtility.DownloadAsync(fileTransferUtilityRequest);
                    return new AWSResult<bool?>(true, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<bool?>(null, new AWSException("DownloadItem", "Not connected.", ex));
            }
        }

    }
}
