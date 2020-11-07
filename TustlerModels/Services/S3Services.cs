using CloudWeaver.Foundation.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels.Services
{
    /// <summary>
    /// Manages S3 services such as uploads and downloads from an S3 bucket
    /// </summary>
    public static class S3Services
    {
        public static async Task<AWSResult<(bool?, string)>> UploadItem(AmazonWebServiceInterface awsInterface, string bucketName, string newKey, string filePath, string mimetype, string extension)
        {
            return await awsInterface.S3.UploadItem(bucketName, newKey, filePath, mimetype, extension).ConfigureAwait(false);
        }

        public static async Task<AWSResult<(bool?, string)>> DownloadItemToFile(AmazonWebServiceInterface awsInterface, string bucketName, string key, string filePath)
        {
            return await awsInterface.S3.DownloadItemToFile(bucketName, key, filePath).ConfigureAwait(false);
        }

        public static async Task<AWSResult<Stream>> DownloadItemAsStream(AmazonWebServiceInterface awsInterface, string bucketName, string key)
        {
            return await awsInterface.S3.DownloadItemAsStream(bucketName, key).ConfigureAwait(false);
        }

        public static async Task<AWSResult<(bool?, string)>> DeleteItem(AmazonWebServiceInterface awsInterface, string bucketName, string key)
        {
            return await awsInterface.S3.DeleteBucketItem(bucketName, key).ConfigureAwait(false);
        }

        public static bool ProcessUploadItemResult(NotificationsList notifications, AWSResult<(bool?, string)> uploadResult)
        {
            bool success = false;

            if (uploadResult.IsError)
            {
                notifications.HandleError(uploadResult);
            }
            else
            {
                var (flag, filePath) = uploadResult.Result;
                success = (flag.HasValue && flag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Upload {successStr}";
                notifications.ShowMessage(message, $"Task: Upload item '{Path.GetFileName(filePath)}' to S3 completed @ {DateTime.Now.ToShortTimeString()}");
            }

            return success;
        }

        public static bool ProcessDownloadItemResult(NotificationsList notifications, AWSResult<(bool?, string)> downloadResult)
        {
            bool success = false;

            if (downloadResult.IsError)
            {
                notifications.HandleError(downloadResult);
            }
            else
            {
                var (flag, filePath) = downloadResult.Result;
                success = (flag.HasValue && flag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Download {successStr}";
                notifications.ShowMessage(message, $"Task: Download item from S3 to '{Path.GetFileName(filePath)}' completed @ {DateTime.Now.ToShortTimeString()}");
            }

            return success;
        }

        public static byte[] ProcessDownloadItemStreamResult(NotificationsList notifications, AWSResult<Stream> downloadResult)
        {
            byte[] result = null;

            if (downloadResult.IsError)
            {
                notifications.HandleError(downloadResult);
            }
            else
            {
                var stream = downloadResult.Result;
                if (stream is object && stream.CanRead && stream.Length > 0)
                {
                    // copy the stream data into a locally owned memory stream
                    static byte[] GetData(Stream sourceStream)
                    {
                        using var memoryStream = new MemoryStream();
                        sourceStream.CopyTo(memoryStream);
                        sourceStream.Close();
                        return memoryStream.ToArray();      // was GetBuffer()
                    }

                    notifications.ShowMessage("Download succeeded", $"Task: Download text item from S3 completed @ {DateTime.Now.ToShortTimeString()}");
                    return GetData(stream);
                }
            }

            return result;
        }

        public static bool ProcessDeleteBucketItemResult(NotificationsList notifications, AWSResult<(bool?, string)> deleteResult)
        {
            bool success = false;

            if (deleteResult.IsError)
            {
                notifications.HandleError(deleteResult);
            }
            else
            {
                var (flag, key) = deleteResult.Result;
                success = (flag.HasValue && flag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Delete operation {successStr}";
                notifications.ShowMessage(message, $"Task: Delete item '{key}' completed @ {DateTime.Now.ToShortTimeString()}");
            }

            return success;
        }

    }
}
