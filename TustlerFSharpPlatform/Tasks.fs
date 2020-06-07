﻿namespace TustlerFSharpPlatform

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
    //| BucketItem of BucketItem
    | Bucket of Bucket                      // selected a bucket
    | BucketsModel of BucketViewModel
    | BucketItemsModel of BucketItemViewModel
    | S3MediaReference of S3MediaReference
    | FileMediaReference of FileMediaReference
    | FilePath of string
    | FileUploadSuccess of bool
    | TranscriptionJobsModel of TranscriptionJobsViewModel
    | BucketRequest
    | FileMediaReferenceRequest
    | S3MediaReferenceRequest

[<RequireQualifiedAccess>]
type TaskEvent =
    | InvokingFunction
    | SetArgument of TaskResponse
    | ForEach of RetainingStack<SubTaskItem>
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

type TranscribeAudioArgs = {
    S3BucketName: string option
    FileMediaReference: FileMediaReference option
}
type TranscribeAudioArgs with
    member x.AllSet =  x.S3BucketName.IsSome && x.FileMediaReference.IsSome
    member x.NumArgs = 2
    member x.Update response =
        match response with
        | TaskResponse.Bucket bucket -> { x with S3BucketName = Some(bucket.Name) }
        | TaskResponse.FileMediaReference media -> { x with FileMediaReference = Some(media) }
        | _ -> invalidArg "response" "Unexpected type"


[<RequireQualifiedAccess>]
type TaskArgumentRecord =
    | TranscribeAudio of TranscribeAudioArgs
    | Simple of string

type TaskArgumentRecord with
    member x.AllSet =
        match x with
        | TaskArgumentRecord.TranscribeAudio arg -> arg.AllSet
        | TaskArgumentRecord.Simple arg -> arg.Length > 0
    member x.NumArgs =
        match x with
        | TaskArgumentRecord.TranscribeAudio arg -> arg.NumArgs
        | TaskArgumentRecord.Simple arg -> arg.Length
    member x.Update response =
        match x with
        | TaskArgumentRecord.TranscribeAudio arg -> TaskArgumentRecord.TranscribeAudio (arg.Update response)
        | TaskArgumentRecord.Simple arg -> TaskArgumentRecord.Simple (arg)

type LocalResolverFunction = (TaskArgumentRecord -> AmazonWebServiceInterface -> NotificationsList -> seq<TaskResponse>)



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
    | FilePath of string
    | ForEach of IEnumerable<SubTaskItem>
    | ContinueWithArgument of ContinueWithArgument
    | S3MediaReference of S3MediaReference
    | FileMediaReference of FileMediaReference

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
    
    /// Find the first unset argument and send a request to the UI to resolve the value
    let private resolveByRequest (args:InfiniteList<MaybeResponse>) (required:TaskResponse[]) =
        let requestStack = Stack(required)

        // consume the request stack for each argument that is set
        args
        |> Seq.takeWhile (fun mr -> mr.IsSet)
        |> Seq.iter (fun mr -> requestStack.Pop() |> ignore)

        if requestStack.Count > 0 then
            Seq.singleton (requestStack.Pop())
        else
            Seq.empty

    /// Find the first unset argument (skipping arguments resolved via UI request) and call the matching resolver function to set the argument value
    let private resolveLocally (args:InfiniteList<MaybeResponse>) (argsRecord:TaskArgumentRecord) awsInterface notifications (resolvers:LocalResolverFunction[]) =
        // get the resolver for the last unset argument
        let resolverIndex = 
            args
            |> Seq.skip argsRecord.NumArgs      // skip over the UI-resolved required arguments
            |> Seq.takeWhile (fun mr -> mr.IsSet)
            |> Seq.length
        resolvers.[resolverIndex] argsRecord awsInterface notifications

    /// Integrate with the default record any request arguments that have been set using the TaskArgumentRecord updater function
    let private integrateUIRequestArguments (args:InfiniteList<MaybeResponse>) (defaultArgs:TaskArgumentRecord) =
        args
        |> Seq.takeWhile (fun mr -> mr.IsSet)
        |> Seq.fold (fun (argsRecord:TaskArgumentRecord) mr -> argsRecord.Update mr.Value) defaultArgs

    /// Validate the supplied arguments by type and position; all or some of the arguments can be unset (MaybeResponse.IsNotSet)
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

    /// Get any notifications generated from the last AWS call (errors or informational messages)
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

    // A minimal method that does nothing
    [<HideFromUI>]
    let MinimalMethod (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) = seq { yield TaskResponse.TaskInfo "Minimal method" }

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

        seq {
            yield TaskResponse.TaskInfo "Doing SomeSubTask"

            yield TaskResponse.TaskComplete "Finished SomeSubTask"
        }

    let Cleanup (arguments: ITaskArgumentCollection) (args: InfiniteList<MaybeResponse>) =

        seq {
            // show the sub-task names (the TaskName is used for function selection)
            yield TaskResponse.TaskMultiSelect ([|
                { TaskName = "CleanTranscriptionJobHistory"; Description = "Transcription Job History" };
                { TaskName = "SomeSubTask"; Description = "Other" }
            |])
        }

    /// Upload and transcribe some audio
    /// The function is called multiple times from the UI until all arguments are resolved
    let TranscribeAudio (common_arguments: ITaskArgumentCollection) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        //let isTranscriptionComplete (jobName: string) (jobs: ObservableCollection<TranscriptionJob>) =
        //    let currentJob = jobs |> Seq.find (fun job -> job.TranscriptionJobName = jobName)
        //    currentJob.TranscriptionJobStatus = "COMPLETED"
        
        let uploadMediaFile = fun argsRecord awsInterface notifications ->
            let wrapped =
                match argsRecord with
                | TaskArgumentRecord.TranscribeAudio arg -> arg
                | _ -> invalidArg "argsRecord" "Expecting a TranscribeAudio record type"
            let bucketName = wrapped.S3BucketName.Value
            let media = wrapped.FileMediaReference.Value

            let taskName = Guid.NewGuid().ToString()

            // note: the task name may be used as the new S3 key
            let success = S3.uploadBucketItem awsInterface notifications bucketName taskName media.FilePath media.MimeType media.Extension |> Async.RunSynchronously

            seq {
                yield! getNotificationResponse notifications
                yield TaskResponse.FileUploadSuccess success
            }

        let awsInterface = (common_arguments :?> NotificationsOnlyArguments).AWSInterface
        let notifications = (common_arguments :?> NotificationsOnlyArguments).Notifications
        //validateArgs 2 argChecker args
        
        seq {
            yield! resolveByRequest resolvable_arguments [| TaskResponse.FileMediaReferenceRequest; TaskResponse.BucketRequest |]

            let defaultArgs = TaskArgumentRecord.TranscribeAudio { TranscribeAudioArgs.FileMediaReference = None; TranscribeAudioArgs.S3BucketName = None }
            let transcribeAudioArgs = integrateUIRequestArguments resolvable_arguments defaultArgs
            if transcribeAudioArgs.AllSet then
                
                yield! resolveLocally resolvable_arguments transcribeAudioArgs awsInterface notifications [|
                    uploadMediaFile
                |]

            //// is MediaReference set?
            //let mediaReferenceResponse = args.Pop()

            //if mediaReferenceResponse.IsNotSet then
            //    yield TaskResponse.MediaReferenceRequest
            //else
            //    let mediaReference =
            //        match mediaReferenceResponse.Value with
            //        | TaskResponse.MediaReference media -> media
            //        | _ -> invalidArg "mediaReferenceResponse" "Expecting a MediaReference" 

            //    // is FilePath set?
            //    let filePathResponse = args.Pop()

            //    if filePathResponse.IsNotSet then
            //        yield TaskResponse.FilePathRequest
            //    else
            //        let filePath =
            //            match filePathResponse.Value with
            //            | TaskResponse.FilePath path -> path
            //            | _ -> invalidArg "filePathResponse" "Expecting a FilePath" 

            //        // note: the task name may be used as the new S3 key
            //        let success = S3.uploadBucketItem awsInterface notifications mediaReference.BucketName mediaReference.Key filePath mediaReference.MimeType mediaReference.Extension |> Async.RunSynchronously
            //        yield! getNotificationResponse notifications

            //        if success then

            //            // note: task name used as job name and as S3 media key (from upload)
            //            let jobsModel = Transcribe.startTranscriptionJob awsInterface notifications args.TaskName mediaReference.BucketName mediaReference.Key args.TranscriptionLanguageCode args.VocabularyName |> Async.RunSynchronously

            //            //let transcribeTasks = startTranscriptionJob args |> Async.RunSynchronously
            //            //yield! getNotificationResponse notifications
            //            //yield! Seq.map (fun item -> TaskResponse.TranscriptionJob item) transcribeTasks

            //            let waitOnCompletionSeq =
            //                0 // try ten times from zero
            //                |> Seq.unfold (fun i ->
            //                    Task.Delay(1000) |> Async.AwaitTask |> Async.RunSynchronously
            //                    let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
            //                    if i > 9 || isTranscriptionComplete args.TaskName model.TranscriptionJobs then
            //                        None
            //                    else
            //                        Some( TaskResponse.DelaySequence i, i + 1))
            //            yield! waitOnCompletionSeq
            //            yield  TaskResponse.TaskComplete "Finished"
        }