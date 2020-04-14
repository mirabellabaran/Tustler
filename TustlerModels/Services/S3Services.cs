using System;
using System.Threading.Tasks;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels.Services
{
    /// <summary>
    /// Manages S3 services such as uploads and downloads from an S3 bucket
    /// </summary>
    public static class S3Services
    {
        public static async Task<AWSResult<bool?>> UploadItem(IAmazonWebInterfaceS3 s3Interface, string bucketName, string newKey, string filePath, string mimetype, string extension)
        {
            return await s3Interface.UploadItem(bucketName, newKey, filePath, mimetype, extension).ConfigureAwait(false);
        }

        public static async Task<AWSResult<bool?>> DownloadItem(IAmazonWebInterfaceS3 s3Interface, string bucketName, string key, string filePath)
        {
            return await s3Interface.DownloadItem(bucketName, key, filePath).ConfigureAwait(false);
        }

        public static async Task<AWSResult<bool?>> DeleteItem(IAmazonWebInterfaceS3 s3Interface, string bucketName, string key)
        {
            return await s3Interface.DeleteBucketItem(bucketName, key).ConfigureAwait(false);
        }

        public static bool ProcessUploadItemResult(NotificationsList notifications, AWSResult<bool?> uploadResult)
        {
            bool success = false;

            if (uploadResult.IsError)
            {
                notifications.HandleError(uploadResult);
            }
            else
            {
                success = (uploadResult.Result.HasValue && uploadResult.Result.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Upload {successStr}";
                notifications.ShowMessage(message, $"Task: Upload item to S3 completed @ {DateTime.Now.ToShortTimeString()}");
            }

            return success;
        }

        public static bool ProcessDownloadItemResult(NotificationsList notifications, AWSResult<bool?> downloadResult)
        {
            bool success = false;

            if (downloadResult.IsError)
            {
                notifications.HandleError(downloadResult);
            }
            else
            {
                success = (downloadResult.Result.HasValue && downloadResult.Result.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Download {successStr}";
                notifications.ShowMessage(message, $"Task: Download item from S3 completed @ {DateTime.Now.ToShortTimeString()}");
            }

            return success;
        }

        public static bool ProcessDeleteBucketItemResult(NotificationsList notifications, AWSResult<bool?> deleteResult, string key)
        {
            bool success = false;

            if (deleteResult.IsError)
            {
                notifications.HandleError(deleteResult);
            }
            else
            {
                success = (deleteResult.Result.HasValue && deleteResult.Result.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Delete operation {successStr}";
                notifications.ShowMessage(message, $"Task: Delete item '{key}' completed @ {DateTime.Now.ToShortTimeString()}");
            }

            return success;
        }

    }
}
