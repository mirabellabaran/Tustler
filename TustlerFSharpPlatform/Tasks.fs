namespace TustlerFSharpPlatform

open TustlerServicesLib
open AWSInterface
open TustlerModels
open System.Collections.ObjectModel

[<RequireQualifiedAccess>]
type TaskArgument =
    | NoArguments
    | S3MediaReference of struct(string * string * string * string * string)    // taskName, bucketName, filePath, mimeType, extension

[<RequireQualifiedAccess>]
type TaskResponse =
    | Notification of Notification
    | Bucket of Bucket
    | BucketItem of BucketItem
    | TranscriptionJob of TranscriptionJob
    
[<RequireQualifiedAccess>]
type TaskFunction =
    | Buckets of (NotificationsList -> ObservableCollection<Bucket>)
    | BucketItems of (NotificationsList -> string -> BucketItemsCollection)
    | UploadItem of (NotificationsList -> string -> string -> string -> string -> unit)
    | StartTranscription of (NotificationsList -> string -> string -> string -> string -> string -> ObservableCollection<TranscriptionJob>)
    | ListVocabularies of (NotificationsList -> ObservableCollection<Vocabulary>)

module public Tasks =            

    let private getNotificationResponse (notifications: NotificationsList) =
        Seq.map (fun note -> TaskResponse.Notification note) notifications.Notifications
        
    let private checkTaskFolder taskName = ()

    let private CheckFileExistsReplaceWithFilePath (fn:TaskFunction) = fn

    let private CheckFileExistsReplaceWithContents (fn:TaskFunction) = fn

    let private ReplaceWithConstant (fn:TaskFunction) =
        match fn with
        | TaskFunction.Buckets _ ->
            let bucket = Bucket(Name="Poop", CreationDate=System.DateTime.Now)
            (TaskFunction.Buckets (fun notifications -> new ObservableCollection<Bucket>(seq { bucket } )))
        | TaskFunction.BucketItems _ ->
            let bucketItems = [|
                BucketItem(Key="AAA", Size=33L, LastModified=System.DateTime.Now, Owner="Me")
                BucketItem(Key="BBB", Size=44L, LastModified=System.DateTime.Now, Owner="Me")
            |]
            (TaskFunction.BucketItems (fun notifications string -> new BucketItemsCollection( bucketItems )))

    let S3FetchItems (_: TaskArgument) =

        //let (TaskArgument.TaskNameFilePath (taskName, filePath)) = args

        // prepare expensive function steps (may be replaced with cached values)
        let (TaskFunction.Buckets getBuckets) = ReplaceWithConstant (TaskFunction.Buckets (fun notifications -> S3.getBuckets notifications |> Async.RunSynchronously))
        let (TaskFunction.BucketItems getBucketItems) = ReplaceWithConstant (TaskFunction.BucketItems (fun notifications bucketName -> S3.getBucketItems notifications bucketName |> Async.RunSynchronously))

        let notifications = NotificationsList()

        seq {
            let buckets: ObservableCollection<Bucket> = getBuckets notifications
            yield! getNotificationResponse notifications

            if buckets.Count > 0 then
                let bucket = Seq.head buckets
                yield TaskResponse.Bucket bucket

                let items = getBucketItems notifications bucket.Name
                yield! getNotificationResponse notifications
                yield! Seq.map (fun item -> TaskResponse.BucketItem item) items
        }

    // upload and transcribe some audio
    let TranscribeAudio (arg: TaskArgument) =

        let (TaskArgument.S3MediaReference (taskName, bucketName, filePath, mimeType, extension)) = arg
        
        //let (TaskFunction.UploadItem uploadItem) = (TaskFunction.UploadItem (fun notifications bucketName filePath mimeType extension -> S3.uploadBucketItem notifications bucketName filePath mimeType extension |> Async.RunSynchronously))
        let (TaskFunction.StartTranscription startTranscriptionJob) = (TaskFunction.StartTranscription (fun notifications jobName bucketName s3MediaKey languageCode vocabularyName -> Transcribe.startTranscriptionJob notifications jobName bucketName s3MediaKey languageCode vocabularyName |> Async.RunSynchronously))
        let (TaskFunction.ListVocabularies listVocabularies) = (TaskFunction.ListVocabularies (fun notifications -> Transcribe.listVocabularies notifications |> Async.RunSynchronously))

        let notifications = NotificationsList()
        
        seq {
            // note: using task name as new S3 key
            let success = S3.uploadBucketItem notifications bucketName taskName filePath mimeType extension |> Async.RunSynchronously
            yield! getNotificationResponse notifications

            if success then

                let vocabs = listVocabularies notifications
                yield! getNotificationResponse notifications

                let vocab =
                    let head = Seq.tryHead vocabs
                    if head.IsSome then head.Value.VocabularyName else null

                // note: task name used as job name and as S3 media key (from upload)
                let transcribeTasks = startTranscriptionJob notifications taskName bucketName taskName "en" vocab   // should be "en-US"
                yield! getNotificationResponse notifications
                yield! Seq.map (fun item -> TaskResponse.TranscriptionJob item) transcribeTasks
        }