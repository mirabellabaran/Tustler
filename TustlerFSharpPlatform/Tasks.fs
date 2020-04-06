namespace TustlerFSharpPlatform

open System.Threading.Tasks
open TustlerServicesLib
open AWSInterface
open TaskArguments
open TustlerModels
open System.Collections.ObjectModel

[<RequireQualifiedAccess>]
type TaskResponse =
    | Notification of Notification
    | DelaySequence of int
    | TaskComplete of string
    | Bucket of Bucket
    | BucketItem of BucketItem
    | TranscriptionJob of TranscriptionJob
    
[<RequireQualifiedAccess>]
type TaskFunction =
    | GetBuckets of (NotificationsList -> Async<ObservableCollection<Bucket>>)
    | GetBucketItems of (NotificationsList -> string -> Async<BucketItemsCollection>)
    | StartTranscriptionJob of (TranscribeAudioArguments -> Async<ObservableCollection<TranscriptionJob>>)

module public Tasks =            

    let private getNotificationResponse (notifications: NotificationsList) =
        Seq.map (fun note -> TaskResponse.Notification note) notifications.Notifications
        
    let private checkTaskFolder taskName = ()

    let private CheckFileExistsReplaceWithFilePath (fn:TaskFunction) = fn

    let private CheckFileExistsReplaceWithContents (fn:TaskFunction) = fn

    let private ReplaceWithConstant (fn:TaskFunction) =
        match fn with
        | TaskFunction.GetBuckets _ ->
            let bucket = Bucket(Name="Poop", CreationDate=System.DateTime.Now)
            (TaskFunction.GetBuckets (fun notifications -> async { return new ObservableCollection<Bucket>( seq { bucket } ) }))
        | TaskFunction.GetBucketItems _ ->
            let bucketItems = [|
                BucketItem(Key="AAA", Size=33L, LastModified=System.DateTime.Now, Owner="Me")
                BucketItem(Key="BBB", Size=44L, LastModified=System.DateTime.Now, Owner="Me")
            |]
            (TaskFunction.GetBucketItems (fun notifications string -> async { return new BucketItemsCollection( bucketItems ) }))

    let S3FetchItems (arguments: ITaskArgumentCollection) =

        let getBuckets (notifications: NotificationsList) =
            S3.getBuckets notifications

        let getBucketItems (notifications: NotificationsList) bucketName =
            S3.getBucketItems notifications bucketName

        // prepare expensive function steps (may be replaced with cached values)
        let (TaskFunction.GetBuckets getBuckets) = ReplaceWithConstant (TaskFunction.GetBuckets (getBuckets))
        let (TaskFunction.GetBucketItems getBucketItems) = ReplaceWithConstant (TaskFunction.GetBucketItems (getBucketItems))

        let notifications = (arguments :?> NotificationsOnlyArguments).Notifications

        seq {
            let buckets: ObservableCollection<Bucket> = getBuckets notifications |> Async.RunSynchronously
            yield! getNotificationResponse notifications

            if buckets.Count > 0 then
                let bucket = Seq.head buckets
                yield TaskResponse.Bucket bucket

                let items = getBucketItems notifications bucket.Name |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield! Seq.map (fun item -> TaskResponse.BucketItem item) items
        }

    // upload and transcribe some audio
    let TranscribeAudio (arguments: ITaskArgumentCollection) =
        
        let startTranscriptionJob (args: TranscribeAudioArguments) =
            // note: task name used as job name and as S3 media key (from upload)
            Transcribe.startTranscriptionJob args.Notifications args.TaskName args.MediaRef.BucketName args.MediaRef.Key args.LanguageCode args.VocabularyName

        let (TaskFunction.StartTranscriptionJob startTranscriptionJob) = CheckFileExistsReplaceWithFilePath (TaskFunction.StartTranscriptionJob (startTranscriptionJob))

        let isTranscriptionComplete (jobName: string) (jobs: ObservableCollection<TranscriptionJob>) =
            let currentJob = jobs |> Seq.find (fun job -> job.TranscriptionJobName = jobName)
            currentJob.TranscriptionJobStatus = "COMPLETED"

        let args = arguments :?> TranscribeAudioArguments
        let notifications = args.Notifications
        let mediaReference = args.MediaRef
        
        seq {
            // note: the task name may be used as the new S3 key
            let success = S3.uploadBucketItem notifications mediaReference.BucketName mediaReference.Key args.FilePath mediaReference.MimeType mediaReference.Extension |> Async.RunSynchronously
            yield! getNotificationResponse notifications

            if success then

                let transcribeTasks = startTranscriptionJob args |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield! Seq.map (fun item -> TaskResponse.TranscriptionJob item) transcribeTasks

                let waitOnCompletionSeq =
                    0 // try ten times from zero
                    |> Seq.unfold (fun i ->
                        Task.Delay(1000) |> Async.AwaitTask |> Async.RunSynchronously
                        let jobs = Transcribe.listTranscriptionJobs notifications |> Async.RunSynchronously
                        if i > 9 || isTranscriptionComplete args.TaskName jobs then
                            None
                        else
                            Some( TaskResponse.DelaySequence i, i + 1))
                yield! waitOnCompletionSeq
                yield  TaskResponse.TaskComplete "Finished"
        }