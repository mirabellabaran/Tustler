namespace TustlerFSharpPlatform

//open TustlerAWSLib
//open TustlerModels
//open TustlerModels.Services

//module public AWSInterface =

//    module S3 =

//        let getBuckets awsInterface notifications =
//            async {
//                let model = BucketViewModel()
//                do! model.Refresh (awsInterface, true, notifications) |> Async.AwaitTask
//                return model
//            }

//        let getBucketItems awsInterface notifications bucketName =
//            async {
//                let model = BucketItemViewModel()
//                do! model.Refresh(awsInterface, notifications, bucketName) |> Async.AwaitTask
//                return model
//            }

//        let deleteBucketItem awsInterface notifications bucketName key =
//            async {
//                let awsResult = S3Services.DeleteItem(awsInterface, bucketName, key) |> Async.AwaitTask |> Async.RunSynchronously
//                return S3Services.ProcessDeleteBucketItemResult(notifications, awsResult)
//            }

//        let uploadBucketItem awsInterface notifications bucketName newKey filePath mimeType extension =
//            async {
//                let awsResult = S3Services.UploadItem(awsInterface, bucketName, newKey, filePath, mimeType, extension) |> Async.AwaitTask |> Async.RunSynchronously
//                return S3Services.ProcessUploadItemResult(notifications, awsResult)
//            }

//        let downloadBucketItem awsInterface notifications bucketName key filePath =
//            async {
//                let awsResult = S3Services.DownloadItem(awsInterface, bucketName, key, filePath) |> Async.AwaitTask |> Async.RunSynchronously
//                return S3Services.ProcessDownloadItemResult(notifications, awsResult)
//            }

//    module Transcribe =

//        let startTranscriptionJob awsInterface notifications jobName bucketName s3MediaKey languageCode vocabularyName =
//            async {
//                let model = TranscriptionJobsViewModel()
//                let _success = model.AddNewTask (awsInterface, notifications, jobName, bucketName, s3MediaKey, languageCode, vocabularyName) |> Async.AwaitTask |> Async.RunSynchronously
//                return model
//            }

//        let listTranscriptionJobs awsInterface notifications =
//            async {
//                let model = TranscriptionJobsViewModel()
//                let _ = model.ListTasks (awsInterface, notifications) |> Async.AwaitTask |> Async.RunSynchronously
//                return model
//            }

//        /// returns the specified job after first forcing an update of the job details
//        let getTranscriptionJobByName awsInterface notifications jobName =
//            async {
//                let model = TranscriptionJobsViewModel()
//                let success = model.GetTaskByName (awsInterface, notifications, jobName) |> Async.AwaitTask |> Async.RunSynchronously
//                return if success then Some(model.[jobName]) else None
//            }

//        let deleteTranscriptionJobByName awsInterface notifications jobName =
//            async {
//                let model = TranscriptionJobsViewModel()
//                return model.DeleteTaskByName (awsInterface, notifications, jobName) |> Async.AwaitTask |> Async.RunSynchronously
//            }

//        let listVocabularies awsInterface notifications =
//            async {
//                let model = TranscriptionVocabulariesViewModel()
//                do! model.Refresh (awsInterface, notifications) |> Async.AwaitTask
//                return model.TranscriptionVocabularies
//            }
