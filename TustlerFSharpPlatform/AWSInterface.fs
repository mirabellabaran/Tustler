namespace TustlerFSharpPlatform

open TustlerModels
open TustlerModels.Services

module public AWSInterface =

    //let downCastNotification (note:Notification) : string =
    //    match note with
    //    | :? ApplicationErrorInfo as error -> sprintf "%s: %s: %s" error.Context error.Message error.Exception.InnerException.Message
    //    | :? ApplicationMessageInfo as message -> sprintf "%s: %s" message.Message message.Detail
    //    | _ -> "Unknown type"

    module S3 =

        let getBuckets s3Interface notifications =
            async {
                let model = BucketViewModel()
                do! model.Refresh (s3Interface, true, notifications) |> Async.AwaitTask
                return model.Buckets
            }

        let getBucketItems s3Interface notifications bucketName =
            async {
                let model = BucketItemViewModel()
                do! model.Refresh(s3Interface, notifications, bucketName) |> Async.AwaitTask
                return model.BucketItems
            }

        let deleteBucketItem s3Interface notifications bucketName key =
            async {
                let awsResult = S3Services.DeleteItem(s3Interface, bucketName, key) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDeleteBucketItemResult(notifications, awsResult, key)
            }

        let uploadBucketItem s3Interface notifications bucketName newKey filePath mimeType extension =
            async {
                let awsResult = S3Services.UploadItem(s3Interface, bucketName, newKey, filePath, mimeType, extension) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessUploadItemResult(notifications, awsResult)
            }

        let downloadBucketItem s3Interface notifications bucketName key filePath =
            async {
                let awsResult = S3Services.DownloadItem(s3Interface, bucketName, key, filePath) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDownloadItemResult(notifications, awsResult)
            }

    module Transcribe =

        let startTranscriptionJob notifications jobName bucketName s3MediaKey languageCode vocabularyName =
            async {
                let model = TranscriptionJobsViewModel()
                do! model.AddNewTask (notifications, jobName, bucketName, s3MediaKey, languageCode, vocabularyName) |> Async.AwaitTask
                return model.TranscriptionJobs
            }

        let listTranscriptionJobs notifications =
            async {
                let model = TranscriptionJobsViewModel()
                do! model.ListTasks (notifications) |> Async.AwaitTask
                return model.TranscriptionJobs
            }

        let listVocabularies notifications =
            async {
                let model = TranscriptionVocabulariesViewModel()
                do! model.Refresh (notifications) |> Async.AwaitTask
                return model.TranscriptionVocabularies
            }
