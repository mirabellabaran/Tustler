namespace TustlerFSharpPlatform

open System.Threading.Tasks
open TustlerServicesLib
open AWSInterface
open TaskArguments
open TustlerModels
open System.Collections.ObjectModel
open TustlerInterfaces
open TustlerAWSLib
open System.Collections.Generic
open System

[<RequireQualifiedAccess>]
type TaskResponse =
    | StringArgument of string
    | TaskInfo of string
    | TaskComplete of string
    | TaskSelect of string                  // prompt the user to select an item (this is also a truncation point for subsequent reselection)
    | Notification of Notification
    | DelaySequence of int
    | Bucket of Bucket
    | BucketItem of BucketItem
    | BucketsModel of BucketViewModel
    | BucketItemsModel of BucketItemViewModel
    | TranscriptionJob of TranscriptionJob

[<RequireQualifiedAccess>]
type TaskEvent =
    | InvokingFunction
    | SetArgument of TaskResponse
    | SelectArgument
    | ClearArguments
    | FunctionCompleted

[<RequireQualifiedAccess>]
type MaybeResponse =
    | Just of TaskResponse
    | Nothing

type MaybeResponse with
    member x.IsSet = match x with MaybeResponse.Just _ -> true | MaybeResponse.Nothing -> false
    member x.IsNotSet = match x with MaybeResponse.Nothing -> true | MaybeResponse.Just _ -> false
    member x.Value = match x with MaybeResponse.Nothing -> invalidArg "MaybeResponse.Value" "Value not set" | MaybeResponse.Just tr -> tr

[<RequireQualifiedAccess>]
type TaskUpdate =
    | DeleteBucketItem of bool * NotificationsList

[<RequireQualifiedAccess>]
type MiniTaskArgument =
    | Bool of bool
    | String of string
    | Int of int

[<RequireQualifiedAccess>]
type TaskFunction =
    | GetBuckets of (AmazonWebServiceInterface -> NotificationsList -> Async<ObservableCollection<Bucket>>)
    | GetBucketItems of (AmazonWebServiceInterface -> NotificationsList -> string -> Async<BucketItemsCollection>)
    | StartTranscriptionJob of (TranscribeAudioArguments -> Async<ObservableCollection<TranscriptionJob>>)

module public MiniTasks =

    let Delete awsInterface (notifications: NotificationsList) (args:MiniTaskArgument[]) =
        if args.Length >= 2 then
            let (bucketName, key) =
                match (args.[0], args.[1]) with
                | (MiniTaskArgument.String a, MiniTaskArgument.String b) -> (a, b)
                | _ -> invalidArg "args" "MiniTasks.Delete: Expecting two string arguments"
            let success = S3.deleteBucketItem awsInterface notifications bucketName key |> Async.RunSynchronously
            TaskUpdate.DeleteBucketItem (success, notifications)
        else invalidArg "args" "Expecting two arguments"


module public Tasks =            
    
    // all or some of the arguments can be Nothing
    let private validateArgs expectedNum argChecker (args: InfiniteList<MaybeResponse>) =
        if args.Count < expectedNum then
            invalidArg "expectedNum" (sprintf "Expecting %d arguments" expectedNum)
        args
        |> Seq.takeWhile (fun mr -> mr.IsSet)   // only examine arguments that are set
        |> Seq.iteri(fun index mr ->
            match mr with
            | MaybeResponse.Just tr -> argChecker index tr
            | MaybeResponse.Nothing -> ()
        )

    let private getNotificationResponse (notifications: NotificationsList) =
        Seq.map (fun note -> TaskResponse.Notification note) notifications.Notifications
        
    let private checkTaskFolder taskName = ()

    let private CheckFileExistsReplaceWithFilePath (fn:TaskFunction) = fn

    let private CheckFileExistsReplaceWithContents (fn:TaskFunction) = fn

    let private ReplaceWithConstant (fn:TaskFunction) =
        match fn with
        | TaskFunction.GetBuckets _ ->
            let bucket = Bucket(Name="Poop", CreationDate=System.DateTime.Now)
            (TaskFunction.GetBuckets (fun s3Interface notifications -> async { return new ObservableCollection<Bucket>( seq { bucket } ) }))
        | TaskFunction.GetBucketItems _ ->
            let bucketItems = [|
                BucketItem(Key="AAA", Size=33L, LastModified=System.DateTime.Now, Owner="Me")
                BucketItem(Key="BBB", Size=44L, LastModified=System.DateTime.Now, Owner="Me")
            |]
            (TaskFunction.GetBucketItems (fun s3Interface notifications string -> async { return new BucketItemsCollection( bucketItems ) }))


    let S3FetchItems (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =      //(events: Queue<TaskEvent>) =

        //let getBuckets awsInterface (notifications: NotificationsList) =
        //    S3.getBuckets awsInterface notifications

        //let getBucketItems awsInterface (notifications: NotificationsList) bucketName =
        //    S3.getBucketItems awsInterface notifications bucketName

        // prepare expensive function steps (may be replaced with cached values)
        //let (TaskFunction.GetBuckets getBuckets) = ReplaceWithConstant (TaskFunction.GetBuckets (getBuckets))
        //let (TaskFunction.GetBucketItems getBucketItems) = ReplaceWithConstant (TaskFunction.GetBucketItems (getBucketItems))

        let argChecker index tr =
            match (index, tr) with
            | 0, TaskResponse.BucketsModel _ -> ()
            | 1, TaskResponse.Bucket _ -> ()
            | 2, TaskResponse.BucketItemsModel _ -> ()
            | _ -> invalidArg "tr" "S3FetchItems: Expecting three arguments: BucketsModel, Bucket, BucketItemsModel" 

        let awsInterface = (arguments :?> NotificationsOnlyArguments).AWSInterface
        let notifications = (arguments :?> NotificationsOnlyArguments).Notifications
        validateArgs 3 argChecker args

        seq {
            // is BucketsModel set?
            if args.Pop().IsNotSet then
                yield TaskResponse.TaskInfo "Retrieving buckets..."
                let model = S3.getBuckets awsInterface notifications |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield TaskResponse.TaskSelect "Choose a bucket:"
                yield TaskResponse.BucketsModel model

            // is SelectedBucket set?   (SelectedBucket is set via UI selection; see TaskSelect above)
            let selectedBucket = args.Pop()

            // is BucketItemsModel set?
            if selectedBucket.IsSet && args.Pop().IsNotSet then
                let bucketName =
                    match selectedBucket.Value with
                    | TaskResponse.Bucket bucket -> bucket.Name
                    | _ -> invalidArg "SelectedBucket" "Expecting a Bucket" 

                yield TaskResponse.TaskInfo "Retrieving bucket items..."

                let model = S3.getBucketItems awsInterface notifications bucketName |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield TaskResponse.BucketItemsModel model

                yield TaskResponse.TaskComplete "Finished"
        }

    let TranscribeCleanup (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =

        let listTranscriptionJobs awsInterface (notifications: NotificationsList) =
            Transcribe.listTranscriptionJobs awsInterface notifications

        let awsInterface = (arguments :?> NotificationsOnlyArguments).AWSInterface
        let notifications = (arguments :?> NotificationsOnlyArguments).Notifications

        seq {
            let jobs = listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
            yield! getNotificationResponse notifications
            yield! Seq.map (fun job -> TaskResponse.TranscriptionJob job) jobs
        }

    // upload and transcribe some audio
    let TranscribeAudio (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =
        
        let startTranscriptionJob (args: TranscribeAudioArguments) =
            // note: task name used as job name and as S3 media key (from upload)
            Transcribe.startTranscriptionJob args.AWSInterface args.Notifications args.TaskName args.MediaRef.BucketName args.MediaRef.Key args.TranscriptionLanguageCode args.VocabularyName

        let (TaskFunction.StartTranscriptionJob startTranscriptionJob) = CheckFileExistsReplaceWithFilePath (TaskFunction.StartTranscriptionJob (startTranscriptionJob))

        let isTranscriptionComplete (jobName: string) (jobs: ObservableCollection<TranscriptionJob>) =
            let currentJob = jobs |> Seq.find (fun job -> job.TranscriptionJobName = jobName)
            currentJob.TranscriptionJobStatus = "COMPLETED"

        let args = arguments :?> TranscribeAudioArguments
        let awsInterface = args.AWSInterface
        let notifications = args.Notifications
        let mediaReference = args.MediaRef
        
        seq {
            // note: the task name may be used as the new S3 key
            let success = S3.uploadBucketItem awsInterface notifications mediaReference.BucketName mediaReference.Key args.FilePath mediaReference.MimeType mediaReference.Extension |> Async.RunSynchronously
            yield! getNotificationResponse notifications

            if success then

                let transcribeTasks = startTranscriptionJob args |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield! Seq.map (fun item -> TaskResponse.TranscriptionJob item) transcribeTasks

                let waitOnCompletionSeq =
                    0 // try ten times from zero
                    |> Seq.unfold (fun i ->
                        Task.Delay(1000) |> Async.AwaitTask |> Async.RunSynchronously
                        let jobs = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                        if i > 9 || isTranscriptionComplete args.TaskName jobs then
                            None
                        else
                            Some( TaskResponse.DelaySequence i, i + 1))
                yield! waitOnCompletionSeq
                yield  TaskResponse.TaskComplete "Finished"
        }