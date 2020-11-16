namespace CloudWeaver.AWS

open TustlerModels
open TustlerModels.Services

module public AWSInterface =

    module S3 =

        let getBuckets awsInterface notifications =
            async {
                let model = BucketViewModel()
                do! model.Refresh (awsInterface, true, notifications) |> Async.AwaitTask
                return model
            }

        let getBucketItems awsInterface notifications bucketName =
            async {
                let model = BucketItemViewModel()
                do! model.Refresh(awsInterface, notifications, bucketName) |> Async.AwaitTask
                return model
            }

        let deleteBucketItem awsInterface notifications bucketName key =
            async {
                let awsResult = S3Services.DeleteItem(awsInterface, bucketName, key) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDeleteBucketItemResult(notifications, awsResult)
            }

        let uploadBucketItem awsInterface notifications bucketName newKey filePath mimeType extension =
            async {
                let awsResult = S3Services.UploadItem(awsInterface, bucketName, newKey, filePath, mimeType, extension) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessUploadItemResult(notifications, awsResult)
            }

        let downloadBucketItemToFile awsInterface notifications bucketName key filePath =
            async {
                let awsResult = S3Services.DownloadItemToFile(awsInterface, bucketName, key, filePath) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDownloadItemResult(notifications, awsResult)
            }

        let downloadBucketItemAsBytes awsInterface notifications bucketName key =
            async {
                let awsResult = S3Services.DownloadItemAsStream(awsInterface, bucketName, key) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDownloadItemStreamResult(notifications, awsResult)
            }

    module Transcribe =

        let startTranscriptionJob awsInterface notifications jobName bucketName s3MediaKey languageCode vocabularyName =
            async {
                let model = TranscriptionJobsViewModel()
                let success = model.AddNewTask (awsInterface, notifications, jobName, bucketName, s3MediaKey, languageCode, vocabularyName) |> Async.AwaitTask |> Async.RunSynchronously
                return (success, model)
            }

        let listTranscriptionJobs awsInterface notifications =
            async {
                let model = TranscriptionJobsViewModel()
                let _ = model.ListTasks (awsInterface, notifications) |> Async.AwaitTask |> Async.RunSynchronously
                return model
            }

        /// returns the specified job after first forcing an update of the job details
        let getTranscriptionJobByName awsInterface notifications jobName =
            async {
                let model = TranscriptionJobsViewModel()
                let success = model.GetTaskByName (awsInterface, notifications, jobName) |> Async.AwaitTask |> Async.RunSynchronously
                return (success, model.[jobName])
            }

        let deleteTranscriptionJobByName awsInterface notifications jobName =
            async {
                let model = TranscriptionJobsViewModel()
                return model.DeleteTaskByName (awsInterface, notifications, jobName) |> Async.AwaitTask |> Async.RunSynchronously
            }

        let listVocabularies awsInterface notifications =
            async {
                let model = TranscriptionVocabulariesViewModel()
                do! model.Refresh (awsInterface, notifications) |> Async.AwaitTask
                return model.TranscriptionVocabularies
            }

    module Translate =

        let getArchivedJob jobName =
            let archiveFilePath = TranslateServices.GetArchivedJob(jobName)
            if isNull archiveFilePath then
                None
            else
                Some(archiveFilePath)

        let getArchivedJobInFolder folder jobName =
            let archiveFilePath = TranslateServices.GetArchivedJob(folder, jobName)
            if isNull archiveFilePath then
                None
            else
                Some(archiveFilePath)

        let translateLargeText awsInterface notifications progress useArchivedJob jobName sourceLanguageCode targetLanguageCode textFilePath terminologyNames = 
            async {
                TranslateServices.TranslateLargeText(awsInterface, notifications, progress, useArchivedJob,
                    jobName, sourceLanguageCode, targetLanguageCode, textFilePath, terminologyNames) |> Async.AwaitTask |> Async.RunSynchronously
            }

        let translateSentences awsInterface notifications progress useArchivedJob jobName sourceLanguageCode targetLanguageCode textFilePath terminologyNames =
            async {
                TranslateServices.TranslateSentences(awsInterface, notifications, progress, useArchivedJob,
                    jobName, sourceLanguageCode, targetLanguageCode, textFilePath, terminologyNames) |> Async.AwaitTask |> Async.RunSynchronously
            }

        let getTranslator awsInterface chunker notifications jobName sourceLanguageCode targetLanguageCode terminologyNames =
            let translator (index, text) =
                TranslateServices.TranslateProcessor(awsInterface, chunker, notifications, null, jobName, sourceLanguageCode, targetLanguageCode, terminologyNames, index, text)
            translator
