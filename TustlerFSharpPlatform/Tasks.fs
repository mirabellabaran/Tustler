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
open System.Runtime.InteropServices

// an attribute to tell the UI not to show certain task functions (those that are called as sub-tasks)
type HideFromUI() = inherit System.Attribute()

[<RequireQualifiedAccess>]
type ContinueWithArgument =
    | Next
    | None

type SubTaskItem = {
    TaskName: string;           // the task function name of the sub-task
    Description: string;
}

[<RequireQualifiedAccess>]
type TaskResponse =
    | StringArgument of string
    | TaskInfo of string
    | TaskComplete of string
    | TaskPrompt of string                  // prompt the user to continue (a single Continue button is displayed along with the prompt message)
    | TaskSelect of string                  // prompt the user to select an item (this is also a truncation point for subsequent reselection)
    | TaskMultiSelect of IEnumerable<SubTaskItem>       // user selects zero or more sub-tasks to perform
    | TaskItem of SubTaskItem                           // the current subtask function name and description (one of the user-selected items from the MultiSelect list)
    | TaskContinueWith of ContinueWithArgument
    | Notification of Notification
    | DelaySequence of int
    | Bucket of Bucket
    | BucketItem of BucketItem
    | BucketsModel of BucketViewModel
    | BucketItemsModel of BucketItemViewModel
    | TranscriptionJobsModel of TranscriptionJobsViewModel

[<RequireQualifiedAccess>]
type TaskEvent =
    | InvokingFunction
    | SetArgument of TaskResponse
    | ForEach of Stack<SubTaskItem>
    | SubTask of string     // the name of the sub-task
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
    | DeleteBucketItem of NotificationsList * bool * string                 // returns success flag and the deleted key
    | DownloadBucketItem of NotificationsList * bool * string * string      // returns success flag, the key of the downloaded item and the download file path

type TaskUpdate with
    member x.Deconstruct([<Out>] notifications : byref<NotificationsList>, [<Out>] success : byref<bool>, [<Out>] key : byref<string>) =
        match x with
        | TaskUpdate.DeleteBucketItem (a, b, c) ->
            notifications <- a
            success <- b
            key <- c
        | _ -> invalidArg "DeleteBucketItem" "TaskUpdate.Deconstruct: unknown type"

    member x.Deconstruct([<Out>] notifications : byref<NotificationsList>, [<Out>] success : byref<bool>, [<Out>] key : byref<string>, [<Out>] filePath : byref<string>) =
        match x with
        | TaskUpdate.DownloadBucketItem (a, b, c, d) ->
            notifications <- a
            success <- b
            key <- c
            filePath <- d
        | _ -> invalidArg "DownloadBucketItem" "TaskUpdate.Deconstruct: unknown type"

[<RequireQualifiedAccess>]
type MiniTaskMode =
    | Unknown
    | Delete
    | Download
    | Select
    | Continue
    | AutoContinue
    | ForEach

[<RequireQualifiedAccess>]
type MiniTaskArgument =
    | Bool of bool
    | String of string
    | Int of int
    | Bucket of Bucket
    | ForEach of IEnumerable<SubTaskItem>
    | ContinueWithArgument of ContinueWithArgument

/// Collects MiniTask arguments (used by user control command source objects)
type MiniTaskArguments () =
    let mutable mode = MiniTaskMode.Unknown
    let mutable arguments: MiniTaskArgument[] = Array.empty

    member this.Mode with get () = mode and set _mode = mode <- _mode
    member this.TaskArguments
        with get() = Seq.ofArray arguments
        and set args = arguments <- Seq.toArray args

[<RequireQualifiedAccess>]
type TaskFunction =
    | GetBuckets of (AmazonWebServiceInterface -> NotificationsList -> Async<ObservableCollection<Bucket>>)
    | GetBucketItems of (AmazonWebServiceInterface -> NotificationsList -> string -> Async<BucketItemsCollection>)
    | StartTranscriptionJob of (TranscribeAudioArguments -> Async<ObservableCollection<TranscriptionJob>>)

module public MiniTasks =

    let Delete awsInterface (notifications: NotificationsList) (context:TaskResponse) (args:MiniTaskArgument[]) =
        if args.Length >= 2 then
            match context with
            | TaskResponse.BucketItemsModel _ ->
                let (bucketName, key) =
                    match (args.[0], args.[1]) with
                    | (MiniTaskArgument.String a, MiniTaskArgument.String b) -> (a, b)
                    | _ -> invalidArg "args" "MiniTasks.Delete: Expecting two string arguments"
                let success = S3.deleteBucketItem awsInterface notifications bucketName key |> Async.RunSynchronously
                TaskUpdate.DeleteBucketItem (notifications, success, key)
            | _ -> invalidArg "context" "MiniTasks.Delete: Unrecognized context"
        else invalidArg "args" "MiniTasks.Delete: Expecting two arguments"

    let Download awsInterface (notifications: NotificationsList) (context:TaskResponse) (args:MiniTaskArgument[]) =
        if args.Length >= 3 then
            match context with
            | TaskResponse.BucketItemsModel _ ->
                let (bucketName, key, filePath) =
                    match (args.[0], args.[1], args.[2]) with
                    | (MiniTaskArgument.String a, MiniTaskArgument.String b, MiniTaskArgument.String c) -> (a, b, c)
                    | _ -> invalidArg "args" "MiniTasks.Download: Expecting three string arguments"
                let success = S3.downloadBucketItem awsInterface notifications bucketName key filePath |> Async.RunSynchronously
                TaskUpdate.DownloadBucketItem (notifications, success, key, filePath)
            | _ -> invalidArg "context" "MiniTasks.Download: Unrecognized context"
        else invalidArg "args" "MiniTasks.Download: Expecting three arguments"

module public Tasks =            
    
    // all or some of the arguments can be Nothing
    let private validateArgs expectedNum argChecker (args: InfiniteList<MaybeResponse>) =
        if args.Count > expectedNum then
            invalidArg "expectedNum" (sprintf "Expecting up to %d set argument values" expectedNum)
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


    //let AAA (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =

    //    let awsInterface = (arguments :?> NotificationsOnlyArguments).AWSInterface
    //    let notifications = (arguments :?> NotificationsOnlyArguments).Notifications

    //    seq {
    //        let model = S3.getBucketItems awsInterface notifications "tator" |> Async.RunSynchronously
    //        yield! getNotificationResponse notifications
    //        yield TaskResponse.BucketItemsModel model

    //        yield TaskResponse.TaskComplete "Finished"
    //    }        

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
                if model.Buckets.Count > 0 then
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

                yield TaskResponse.TaskInfo (sprintf "Retrieving bucket items from %s..." bucketName)

                let model = S3.getBucketItems awsInterface notifications bucketName |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield TaskResponse.BucketItemsModel model

                yield TaskResponse.TaskComplete "Finished"
        }

    [<HideFromUI>]
    let CleanTranscriptionJobHistory (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =

        let hasDeleteableJobs (model: TranscriptionJobsViewModel) =
            let empty =
                model.TranscriptionJobs
                |> Seq.filter (fun job -> List.contains job.TranscriptionJobStatus [ "COMPLETED"; "FAILED" ] )
                |> Seq.isEmpty
            not empty

        let deleteAll awsInterface notifications (model: TranscriptionJobsViewModel) =
            model.TranscriptionJobs
            |> Seq.filter (fun job -> List.contains job.TranscriptionJobStatus [ "COMPLETED"; "FAILED" ] )      // skip IN_PROGRESS and QUEUED
            |> Seq.map (fun job -> job.TranscriptionJobName)
            |> Seq.map (fun jobName ->
                let success = Transcribe.deleteTranscriptionJobByName awsInterface notifications jobName |> Async.RunSynchronously
                TaskResponse.TaskInfo (sprintf "Delete job %s...%s" jobName (if success then "succeeded" else "failed"))
            )

        let getTaskInfo (taskInfo:MaybeResponse) =
            match taskInfo.Value with
            | TaskResponse.TaskItem item -> item
            | _ -> invalidArg "taskInfo" "Expecting a TaskItem" 

        let argChecker index tr =
            match (index, tr) with
            | 0, TaskResponse.TaskItem _ -> ()
            | 1, TaskResponse.TranscriptionJobsModel _ -> ()
            | _ -> invalidArg "tr" "Cleanup: Expecting two arguments: TaskItem and TranscriptionJobsModel"

        let awsInterface = (arguments :?> NotificationsOnlyArguments).AWSInterface
        let notifications = (arguments :?> NotificationsOnlyArguments).Notifications
        validateArgs 2 argChecker args

        seq {
            let taskInfo = args.Pop()
    
            // is TranscriptionJobsModel set?
            let modelResponse = args.Pop()

            if modelResponse.IsNotSet then
                yield TaskResponse.TaskInfo (sprintf "Running %s" (getTaskInfo taskInfo).Description)

                yield TaskResponse.TaskInfo "Retrieving transcription jobs..."

                let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield TaskResponse.TranscriptionJobsModel model
                yield TaskResponse.TaskPrompt "Delete all completed transcription jobs?"

            if modelResponse.IsSet then
                let model =
                    match modelResponse.Value with
                    | TaskResponse.TranscriptionJobsModel jm -> jm
                    | _ -> invalidArg "modelResponse" "Expecting a TranscriptionJobsModel" 

                if hasDeleteableJobs model then
                    yield! deleteAll awsInterface notifications model
                    yield! getNotificationResponse notifications

                    let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                    yield! getNotificationResponse notifications
                    yield TaskResponse.TranscriptionJobsModel model
                else
                    yield TaskResponse.TaskInfo "No transcription jobs to delete"

                yield TaskResponse.TaskComplete (sprintf "Completed %s" (getTaskInfo taskInfo).Description)
        }

    [<HideFromUI>]
    let SomeSubTask (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =

        let awsInterface = (arguments :?> NotificationsOnlyArguments).AWSInterface
        let notifications = (arguments :?> NotificationsOnlyArguments).Notifications

        seq {
            yield TaskResponse.TaskInfo "Doing SomeSubTask"

            yield TaskResponse.TaskComplete "Finished SomeSubTask"
        }

    let Cleanup (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =

        let awsInterface = (arguments :?> NotificationsOnlyArguments).AWSInterface
        let notifications = (arguments :?> NotificationsOnlyArguments).Notifications

        seq {
            // show the sub-task names (the TaskName is used for function selection)
            yield TaskResponse.TaskMultiSelect ([|
                { TaskName = "CleanTranscriptionJobHistory"; Description = "Transcription Job History" };
                { TaskName = "SomeSubTask"; Description = "Other" }
            |])

            //yield TaskResponse.TaskContinueWith ContinueWithArgument.Next

            //yield TaskResponse.TaskComplete "Finished all selected tasks"
        }

    // upload and transcribe some audio
    let TranscribeAudio (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =
        
        let startTranscriptionJob (args: TranscribeAudioArguments) =
            // note: task name used as job name and as S3 media key (from upload)
            Transcribe.startTranscriptionJob args.AWSInterface args.Notifications args.TaskName args.MediaRef.BucketName args.MediaRef.Key args.TranscriptionLanguageCode args.VocabularyName

        //let (TaskFunction.StartTranscriptionJob startTranscriptionJob) = CheckFileExistsReplaceWithFilePath (TaskFunction.StartTranscriptionJob (startTranscriptionJob))

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

                //let transcribeTasks = startTranscriptionJob args |> Async.RunSynchronously
                //yield! getNotificationResponse notifications
                //yield! Seq.map (fun item -> TaskResponse.TranscriptionJob item) transcribeTasks

                let waitOnCompletionSeq =
                    0 // try ten times from zero
                    |> Seq.unfold (fun i ->
                        Task.Delay(1000) |> Async.AwaitTask |> Async.RunSynchronously
                        let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                        if i > 9 || isTranscriptionComplete args.TaskName model.TranscriptionJobs then
                            None
                        else
                            Some( TaskResponse.DelaySequence i, i + 1))
                yield! waitOnCompletionSeq
                yield  TaskResponse.TaskComplete "Finished"
        }