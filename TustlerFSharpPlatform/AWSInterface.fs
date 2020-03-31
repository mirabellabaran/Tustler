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

        let getBuckets notifications =
            async {
                let model = BucketViewModel()
                do! model.Refresh (true, notifications) |> Async.AwaitTask
                return model.Buckets
            }

        let getBucketItems notifications bucketName =
            async {
                let model = BucketItemViewModel()
                do! model.Refresh(notifications, bucketName) |> Async.AwaitTask
                return model.BucketItems
            }

        let deleteBucketItem notifications bucketName key =
            async {
                let awsResult = S3Services.DeleteItem(bucketName, key) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDeleteBucketItemResult(notifications, awsResult, key)
            }

        let uploadBucketItem notifications bucketName newKey filePath mimeType extension =
            async {
                let awsResult = S3Services.UploadItem(bucketName, newKey, filePath, mimeType, extension) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessUploadItemResult(notifications, awsResult)
            }

        let downloadBucketItem notifications bucketName key filePath =
            async {
                let awsResult = S3Services.DownloadItem(bucketName, key, filePath) |> Async.AwaitTask |> Async.RunSynchronously
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
