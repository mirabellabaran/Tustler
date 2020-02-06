using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tustler.Models;

namespace Tustler.Helpers
{
    /// <summary>
    /// Manages uploads and downloads from an S3 bucket
    /// </summary>
    public static class TransferManager
    {
        public static async Task<TustlerAWSLib.AWSResult<bool?>> UploadItem(string bucketName, string filePath, string mimetype, string extension)
        {
            return await TustlerAWSLib.S3.UploadItem(bucketName, filePath, mimetype, extension).ConfigureAwait(false);
        }

        public static async Task<TustlerAWSLib.AWSResult<bool?>> DownloadItem(string bucketName, string key, string filePath)
        {
            return await TustlerAWSLib.S3.DownloadItem(bucketName, key, filePath).ConfigureAwait(false);
        }

        public static async Task<TustlerAWSLib.AWSResult<bool?>> DeleteItem(string bucketName, string key)
        {
            return await TustlerAWSLib.S3.DeleteBucketItem(bucketName, key).ConfigureAwait(false);
        }

        public static void ProcessUploadItemResult(NotificationsList notifications, TustlerAWSLib.AWSResult<bool?> uploadResult)
        {
            if (uploadResult.IsError)
            {
                notifications.HandleError(uploadResult);
            }
            else
            {
                var resultFlag = uploadResult.Result;
                var success = (resultFlag.HasValue && resultFlag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Upload {successStr}";
                notifications.ShowMessage(message, $"Task: Upload item to S3 completed @ {DateTime.Now.ToShortTimeString()}");
            }
        }

        public static void ProcessDownloadItemResult(NotificationsList notifications, TustlerAWSLib.AWSResult<bool?> downloadResult)
        {
            if (downloadResult.IsError)
            {
                notifications.HandleError(downloadResult);
            }
            else
            {
                var resultFlag = downloadResult.Result;
                var success = (resultFlag.HasValue && resultFlag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Download {successStr}";
                notifications.ShowMessage(message, $"Task: Download item to S3 completed @ {DateTime.Now.ToShortTimeString()}");
            }
        }

        public static bool ProcessDeleteBucketItemResult(NotificationsList notifications, TustlerAWSLib.AWSResult<bool?> deleteResult, string key)
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
