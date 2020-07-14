namespace TustlerFSharpPlatform

open System
open System.Runtime.InteropServices
open TustlerServicesLib
open AWSInterface
open TustlerModels
open System.Collections.Generic

// an attribute to tell the UI not to show certain task functions (those that are called as sub-tasks)
type HideFromUI() = inherit System.Attribute()

//[<RequireQualifiedAccess>]
//type ContinueWithArgument =
//    | Continue of string    // auto-continue displaying a string message
//    | Next                  // auto-continue to next task (default message)
//    | None                  // auto-continue with no message
//    override x.ToString() =
//        match x with
//        | Continue str -> str
//        | Next -> "Next"
//        | None -> ""

//type LocalResolverFunction = (TaskArgumentRecord -> AmazonWebServiceInterface -> NotificationsList -> seq<TaskResponse>)


//[<RequireQualifiedAccess>]
//type TaskFunction =
//    | GetBuckets of (AmazonWebServiceInterface -> NotificationsList -> Async<ObservableCollection<Bucket>>)
//    | GetBucketItems of (AmazonWebServiceInterface -> NotificationsList -> string -> Async<BucketItemsCollection>)
//    | StartTranscriptionJob of (TranscribeAudioArguments -> Async<ObservableCollection<TranscriptionJob>>)

module public MiniTasks =

    [<RequireQualifiedAccess>]
    type MiniTaskArgument =
        | Bool of bool
        | String of string
        | Int of int

    type MiniTaskMode =
        | Unknown
        | Delete
        | Download
        //| Select
        //| Continue
        //| AutoContinue
        //| ForEachIndependantTask
    
    type MiniTaskArguments () =
        let mutable mode = MiniTaskMode.Unknown
        let mutable arguments: MiniTaskArgument[] = Array.empty
    
        member this.Mode with get () = mode and set _mode = mode <- _mode
        member this.TaskArguments
            with get() = Seq.ofArray arguments
            and set args = arguments <- Seq.toArray args
    
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
    
    let Delete awsInterface (notifications: NotificationsList) (context:TaskResponse) (args:MiniTaskArgument[]) =
        if args.Length >= 2 then
            match context with
            | TaskResponse.SetBucketItemsModel _ ->
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
            | TaskResponse.SetBucketItemsModel _ ->
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

    ///// Find the first unset argument (skipping arguments resolved via UI request) and call the matching resolver function to set the argument value
    //let private resolveLocally (args:InfiniteList<MaybeResponse>) (argsRecord:TaskArgumentRecord) awsInterface notifications (resolvers:LocalResolverFunction[]) =
    //    // get the resolver for the last unset argument
    //    let resolverIndex = 
    //        args
    //        |> Seq.skip argsRecord.InitialArgs      // skip over the UI-resolved required arguments
    //        |> Seq.takeWhile (fun mr -> mr.IsSet)
    //        |> Seq.length
    //    resolvers.[resolverIndex] argsRecord awsInterface notifications

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
        
    //let private checkTaskFolder taskName = ()

    //let private CheckFileExistsReplaceWithFilePath (fn:TaskFunction) = fn

    //let private CheckFileExistsReplaceWithContents (fn:TaskFunction) = fn

    //let private ReplaceWithConstant (fn:TaskFunction) =
    //    match fn with
    //    | TaskFunction.GetBuckets _ ->
    //        let bucket = Bucket(Name="Poop", CreationDate=System.DateTime.Now)
    //        (TaskFunction.GetBuckets (fun s3Interface notifications -> async { return new ObservableCollection<Bucket>( seq { bucket } ) }))
    //    | TaskFunction.GetBucketItems _ ->
    //        let bucketItems = [|
    //            BucketItem(Key="AAA", Size=33L, LastModified=System.DateTime.Now, Owner="Me")
    //            BucketItem(Key="BBB", Size=44L, LastModified=System.DateTime.Now, Owner="Me")
    //        |]
    //        (TaskFunction.GetBucketItems (fun s3Interface notifications string -> async { return new BucketItemsCollection( bucketItems ) }))


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
    let MinimalMethod (_args: InfiniteList<MaybeResponse>) = seq { yield TaskResponse.TaskInfo "Minimal method" }

    let S3FetchItems (resolvable_arguments: InfiniteList<MaybeResponse>) =

        //let getBuckets awsInterface (notifications: NotificationsList) =
        //    S3.getBuckets awsInterface notifications

        //let getBucketItems awsInterface (notifications: NotificationsList) bucketName =
        //    S3.getBucketItems awsInterface notifications bucketName

        // prepare expensive function steps (may be replaced with cached values)
        //let (TaskFunction.GetBuckets getBuckets) = ReplaceWithConstant (TaskFunction.GetBuckets (getBuckets))
        //let (TaskFunction.GetBucketItems getBucketItems) = ReplaceWithConstant (TaskFunction.GetBucketItems (getBucketItems))

        let showS3Data argsRecord =
            // assert the following as always set
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value

            // assert the following may be None on first call
            let bucketModel = argsRecord.S3BucketModel
            let selectedBucket = argsRecord.S3Bucket

            seq {
                if bucketModel.IsNone then
                    yield TaskResponse.TaskInfo "Retrieving buckets..."
                    let model = S3.getBuckets awsInterface notifications |> Async.RunSynchronously
                    yield! getNotificationResponse notifications
                    if model.Buckets.Count > 0 then
                        yield TaskResponse.TaskSelect "Choose a bucket:"
                        yield TaskResponse.SetBucketsModel model

                //// is SelectedBucket set?   (SelectedBucket is set via UI selection; see TaskSelect above)
                //let selectedBucket = args.Pop()

                //// is BucketItemsModel set?
                //if selectedBucket.IsSet && args.Pop().IsNotSet then
                    //let bucketName =
                    //    match selectedBucket.Value with
                    //    | TaskResponse.SetBucket bucket -> bucket.Name
                    //    | _ -> invalidArg "SelectedBucket" "Expecting a Bucket" 
                if bucketModel.IsSome && selectedBucket.IsSome then
                    let bucketName = selectedBucket.Value.Name
                    yield TaskResponse.TaskInfo (sprintf "Retrieving bucket items from %s..." bucketName)

                    let model = S3.getBucketItems awsInterface notifications bucketName |> Async.RunSynchronously
                    yield! getNotificationResponse notifications
                    yield TaskResponse.SetBucketItemsModel model

                    yield TaskResponse.TaskComplete "Finished"
            }

        seq {
            // Eventually expecting three arguments: SetBucketsModel, SetBucket, SetBucketItemsModel

            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            yield! showS3Data resolvedRecord
        }

    [<HideFromUI>]
    let CleanTranscriptionJobHistory (resolvable_arguments: InfiniteList<MaybeResponse>) =

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

        let cleanHistory argsRecord =
            // assert the following as always set
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value
            let taskInfo = argsRecord.SubTaskItem.Value

            // assert the following may be None on first call
            let transcriptionJobsModel = argsRecord.TranscriptionJobsModel

            seq {
                if transcriptionJobsModel.IsNone then
                    yield TaskResponse.TaskInfo (sprintf "Running %s" taskInfo.Description)

                    yield TaskResponse.TaskInfo "Retrieving transcription jobs..."

                    let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                    yield! getNotificationResponse notifications
                    yield TaskResponse.SetTranscriptionJobsModel model
                    yield TaskResponse.TaskPrompt "Delete all completed transcription jobs?"

                if transcriptionJobsModel.IsSome then
                    let model = transcriptionJobsModel.Value

                    if hasDeleteableJobs model then
                        yield! deleteAll awsInterface notifications model
                        yield! getNotificationResponse notifications

                        let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                        yield! getNotificationResponse notifications
                        yield TaskResponse.ShowTranscriptionJobsSummary model
                    else
                        yield TaskResponse.TaskInfo "No transcription jobs to delete"

                    yield TaskResponse.TaskComplete (sprintf "Completed %s" taskInfo.Description)
            }

        seq {
            // eventually expecting two arguments: SubTaskItem and TranscriptionJobsModel

            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            yield! cleanHistory resolvedRecord
        }

    [<HideFromUI>]
    let SomeSubTask (_args: InfiniteList<MaybeResponse>) =

        seq {
            yield TaskResponse.TaskInfo "Doing SomeSubTask"

            yield TaskResponse.TaskComplete "Finished SomeSubTask"
        }

    let Cleanup (args: InfiniteList<MaybeResponse>) =

        seq {
            // show the sub-task names (the TaskName is used for function selection)
            yield TaskResponse.TaskMultiSelect ([|
                { TaskName = "CleanTranscriptionJobHistory"; Description = "Transcription Job History" };
                { TaskName = "SomeSubTask"; Description = "Other" }
            |])
        }

    [<HideFromUI>]
    let UploadMediaFile (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let uploadMediaFile argsRecord =
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value

            let bucketName = argsRecord.S3Bucket.Value.Name
            let media = argsRecord.FileMediaReference.Value
            let newKey = Guid.NewGuid().ToString()

            let success = S3.uploadBucketItem awsInterface notifications bucketName newKey media.FilePath media.MimeType media.Extension |> Async.RunSynchronously

            seq {
                yield! getNotificationResponse notifications
                if success then
                    yield TaskResponse.SetFileUpload (S3MediaReference(bucketName, newKey, media.MimeType, media.Extension))
                yield TaskResponse.TaskComplete "Uploaded media file"
            }

        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs
            if resolvedRecord.ReadyForSubTask 0 then
                yield! uploadMediaFile resolvedRecord
        }

    [<HideFromUI>]
    let StartTranscription (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let startTranscription argsRecord =
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value

            let s3Mmedia = argsRecord.S3MediaReference.Value
            let languageCode = argsRecord.TranscriptionLanguageCode.Value
            let vocabularyName = argsRecord.VocabularyName.Value

            let jobName = Guid.NewGuid().ToString()

            // note: the task name may be used as the output S3 key
            let jobsModel = Transcribe.startTranscriptionJob awsInterface notifications jobName s3Mmedia.BucketName s3Mmedia.Key languageCode vocabularyName |> Async.RunSynchronously

            seq {
                yield! getNotificationResponse notifications
                yield TaskResponse.SetTranscriptionJobName jobName
                yield TaskResponse.ShowTranscriptionJobsSummary jobsModel
                yield TaskResponse.TaskComplete "Transcription started"
            }

        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs
            if resolvedRecord.ReadyForSubTask 1 then
                yield! startTranscription resolvedRecord
        }

    [<HideFromUI>]
    let MonitorTranscription (resolvable_arguments: InfiniteList<MaybeResponse>) =


        let monitorTranscription argsRecord =
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value

            let jobName = argsRecord.TranscriptionJobName.Value

            seq {
                // note: the task name may be used as the output S3 key
                let jobsModel = Transcribe.getTranscriptionJobByName awsInterface notifications jobName |> Async.RunSynchronously
                yield! getNotificationResponse notifications

                if jobsModel.IsSome then
                    let isComplete = (jobsModel.Value.TranscriptionJobStatus = "COMPLETED") //Amazon.TranscribeService.TranscriptionJobStatus.COMPLETED)
                    if isComplete then
                        yield TaskResponse.TaskComplete "Transcription Job Completed"
                    else
                        yield TaskResponse.TaskInfo "Querying job status"
                        yield TaskResponse.TaskContinue 3000
            }

        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs
            if resolvedRecord.ReadyForSubTask 2 then
                yield! monitorTranscription resolvedRecord
        }

    /// Upload and transcribe some audio
    /// The function is called multiple times from the UI until all arguments are resolved
    let TranscribeAudio (resolvable_arguments: InfiniteList<MaybeResponse>) =
        
        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.AllRequestsSet then  // restored from a previous session OR resolved by request to the UI
                yield TaskResponse.TaskArgumentSave     // save the resolved arguments (if not already saved)
                yield TaskResponse.TaskSequence ([|
                    { TaskName = "UploadMediaFile"; Description = "Upload a media file to transcribe" };
                    { TaskName = "StartTranscription"; Description = "Start a transcription job" };
                    { TaskName = "MonitorTranscription"; Description = "Monitor the transcription job" }
                |])
                yield TaskResponse.TaskComplete ""
            else
                yield! resolveByRequest resolvable_arguments [| TaskResponse.RequestVocabularyName; TaskResponse.RequestTranscriptionLanguageCode; TaskResponse.RequestFileMediaReference; TaskResponse.RequestBucket |]
        }