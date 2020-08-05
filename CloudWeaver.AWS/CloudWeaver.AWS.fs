﻿namespace CloudWeaver.AWS

open System
open System.Runtime.InteropServices
open TustlerServicesLib
open TustlerModels
open System.Collections.Generic
open CloudWeaver.Types

open TustlerModels.Services
open TustlerAWSLib
open System.Text.Json
open System.IO

/// A reference to a media file item stored in an S3 Bucket
type S3MediaReference(bucketName: string, key: string, mimeType: string, extension: string) =
    let mutable _bucketName = bucketName
    let mutable _key = key
    let mutable _mimeType = mimeType
    let mutable _extension = extension

    member this.BucketName with get() = _bucketName and set(value) = _bucketName <- value
    member this.Key with get() = _key and set(value) = _key <- value
    member this.MimeType with get() = _mimeType and set(value) = _mimeType <- value
    member this.Extension with get() = _extension and set(value) = _extension <- value

    new() = S3MediaReference(null, null, null, null)

/// A reference to a media file stored locally (normally a file to be uploaded to S3)
type FileMediaReference(filePath: string, mimeType: string, extension: string) =
    let mutable _filePath = filePath
    let mutable _mimeType = mimeType
    let mutable _extension = extension

    member this.FilePath with get() = _filePath and set(value) = _filePath <- value
    member this.MimeType with get() = _mimeType and set(value) = _mimeType <- value
    member this.Extension with get() = _extension and set(value) = _extension <- value

    new() = FileMediaReference(null, null, null)

/// Display values passed by this module to the user interface for display (note that AWSArgument types may also be displayed)
type AWSDisplayValue =
    | DisplayBucketItemsModel of BucketItemViewModel                // show bucket items
    | DisplayTranscriptionJob of TranscriptionJob                   // show details of a specific transcription job
    | DisplayTranscriptionJobsModel of TranscriptionJobsViewModel   // show the model that wraps transcription jobs (see also AWSArgument.SetTranscriptionJobsModel)

/// Wrapper for the display values used by this module
and AWSShowIntraModule(arg: AWSDisplayValue) =
    interface IShowValue with
        member this.Identifier with get() = Identifier (CommonUtilities.toString arg)

    member this.Argument with get() = arg

/// Arguments used by this module.
/// These values are set on the events stack (as TaskEvent SetArgument).
type AWSArgument =
    | SetAWSInterface of AmazonWebServiceInterface
    | SetBucket of Bucket                           // set an argument on the events stack for the selected bucket
    | SetBucketsModel of BucketViewModel            // set an argument on the events stack for the available buckets
    | SetS3MediaReference of S3MediaReference                       // Amazon S3 file reference to the uploaded file
    | SetTranscriptionJobName of string                             // the name of the new transcription job
    | SetTranscriptURI of string                                    // the URI (S3 bucket/key) of the transcript from a completed transcription job
    | SetTranscriptionJobsModel of TranscriptionJobsViewModel       // the model that wraps transcription jobs
    | SetFileMediaReference of FileMediaReference
    | SetTranscriptionLanguageCode of string
    | SetTranslationLanguageCode of string
    | SetVocabularyName of string
    with
    member this.toSetArgumentTaskResponse() = TaskResponse.SetArgument (AWSShareIntraModule(this))
    member this.toTaskEvent() = TaskEvent.SetArgument(this.toSetArgumentTaskResponse());

/// Wrapper for the arguments used by this module
and AWSShareIntraModule(arg: AWSArgument) =
    interface IShareIntraModule with
        member this.Identifier with get() = Identifier (CommonUtilities.toString arg)
        member this.AsBytes () =
            JsonSerializer.SerializeToUtf8Bytes(arg)
        member this.Serialize writer =
            match arg with
            | SetAWSInterface _awsInterface -> ()
            | SetBucket bucket -> writer.WritePropertyName("SetBucket"); JsonSerializer.Serialize<Bucket>(writer, bucket)
            | SetBucketsModel bucketViewModel -> writer.WritePropertyName("SetBucketsModel"); JsonSerializer.Serialize<BucketViewModel>(writer, bucketViewModel)
            | SetS3MediaReference s3MediaReference -> writer.WritePropertyName("SetFileUpload"); JsonSerializer.Serialize<S3MediaReference>(writer, s3MediaReference)
            | SetTranscriptionJobName jobName -> writer.WritePropertyName("SetTranscriptionJobName"); JsonSerializer.Serialize<string>(writer, jobName)
            | SetTranscriptURI transcriptURI -> writer.WritePropertyName("SetTranscriptURI"); JsonSerializer.Serialize<string>(writer, transcriptURI)
            | SetTranscriptionJobsModel transcriptionJobsViewModel -> writer.WritePropertyName("SetTranscriptionJobsModel"); JsonSerializer.Serialize<TranscriptionJobsViewModel>(writer, transcriptionJobsViewModel)
            | SetFileMediaReference fileMediaReference -> writer.WritePropertyName("SetFileMediaReference"); JsonSerializer.Serialize<FileMediaReference>(writer, fileMediaReference)
            | SetTranscriptionLanguageCode transcriptionLanguageCode -> writer.WritePropertyName("SetTranscriptionLanguageCode"); JsonSerializer.Serialize<string>(writer, transcriptionLanguageCode)
            | SetTranslationLanguageCode translationLanguageCode -> writer.WritePropertyName("SetTranslationLanguageCode"); JsonSerializer.Serialize<string>(writer, translationLanguageCode)
            | SetVocabularyName vocabularyName -> writer.WritePropertyName("SetVocabularyName"); JsonSerializer.Serialize<string>(writer, vocabularyName)
    
    member this.Argument with get() = arg

    static member fromString idString =
        CommonUtilities.fromString<AWSArgument> idString

    static member Deserialize propertyName (jsonString:string) =
        let awsArgument =
            match propertyName with
            | "SetNotificationsList" ->
                invalidArg "propertyName" "NotificationsList should not be serialized"
            | "SetAWSInterface" ->
                invalidArg "propertyName" "AWSInterface should not be serialized"
            | "SetBucket" ->
                let data = JsonSerializer.Deserialize<Bucket>(jsonString)
                AWSArgument.SetBucket data
            | "SetBucketsModel" ->
                let data = JsonSerializer.Deserialize<BucketViewModel>(jsonString)
                AWSArgument.SetBucketsModel data
            | "SetFileUpload" ->
                let data = JsonSerializer.Deserialize<S3MediaReference>(jsonString)
                AWSArgument.SetS3MediaReference data
            | "SetTranscriptionJobName" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranscriptionJobName data
            | "SetTranscriptURI" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranscriptURI data
            | "SetTranscriptionJobsModel" ->
                let data = JsonSerializer.Deserialize<TranscriptionJobsViewModel>(jsonString)
                AWSArgument.SetTranscriptionJobsModel data
            | "SetFileMediaReference" ->
                let data = JsonSerializer.Deserialize<FileMediaReference>(jsonString)
                AWSArgument.SetFileMediaReference data
            | "SetTranscriptionLanguageCode" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranscriptionLanguageCode data
            | "SetTranslationLanguageCode" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranslationLanguageCode data
            | "SetVocabularyName" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetVocabularyName data
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" propertyName)

        AWSShareIntraModule(awsArgument)

/// Requests used by this module
type AWSRequest =
    | RequestAWSInterface
    | RequestBucket
    | RequestFileMediaReference
    | RequestS3MediaReference
    | RequestTranscriptionJobName
    | RequestTranscriptURI
    | RequestTranscriptionLanguageCode
    | RequestTranslationLanguageCode
    | RequestVocabularyName

/// Wrapper for the requests used by this module
type AWSRequestIntraModule(awsRequest: AWSRequest) =
    
    interface IRequestIntraModule with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> IRequestIntraModule).Identifier.AsString()
            let str2 = (obj :?> IRequestIntraModule).Identifier.AsString()
            System.String.Compare(str1, str2)
        member this.Identifier with get() = Identifier (CommonUtilities.toString awsRequest)

    member this.Request with get() = awsRequest

/// Wrapper for the pre-assigned values used by this module
/// (values that are known in advance by the user interface layer)
type AWSKnownArguments(awsInterface) =
    interface IKnownArguments with
        member this.KnownRequests with get() =
            seq {
                AWSRequestIntraModule(AWSRequest.RequestAWSInterface);
            }
        member this.GetKnownArgument(request: IRequestIntraModule) =
            let unWrapRequest (request:IRequestIntraModule) =
                match request with
                | :? AWSRequestIntraModule as awsRequestIntraModule -> awsRequestIntraModule.Request
                | _ -> invalidArg "request" "The request is not of type AWSRequestIntraModule"
            match (unWrapRequest request) with
            | RequestAWSInterface -> AWSArgument.SetAWSInterface(awsInterface).toTaskEvent()
            | _ -> invalidArg "request" "The request is not a known argument"

/// The set of all possible argument types (passed to Task functions) that are of interest to the AWS module
type TaskArgumentRecord = {
    // common arguments required by many Task functions
    Notifications: NotificationsList option                 // notifications (informational or error messages) generated by function calls
    AWSInterface: AmazonWebServiceInterface option          // an interface to all defined AWS functions (including mocked versions)
    WorkingDirectory: DirectoryInfo option

    // arguments that normally requiring user resolution (via TaskResponse.Request*)
    S3Bucket: Bucket option
    S3BucketModel: BucketViewModel option
    FileMediaReference: FileMediaReference option           // a reference to a media file to be uploaded
    TranscriptionLanguageCode: string option                // a transcription language code
    VocabularyName: string option                           // the name of an optional transcription vocabulary

    // arguments generated in response to proevious Task function calls
    TaskItem: TaskItem option
    S3MediaReference: S3MediaReference option               // a reference to an uploaded media file
    TranscriptionJobName: string option                     // job name used when starting a new transcription job
    TranscriptionJobsModel: TranscriptionJobsViewModel option   // the state of all transcription jobs, running or completed
    TranscriptURI: string option                            // location of the transcript for a completed transcription job

    //TranslationLanguageCode: string option
}
type TaskArgumentRecord with
    static member Init () =
        {
            Notifications = None;
            AWSInterface = None;
            WorkingDirectory = None;
                
            S3Bucket = None;
            S3BucketModel = None;
            FileMediaReference = None;
            TranscriptionLanguageCode = None;
            VocabularyName = None;

            TaskItem = None;
            S3MediaReference = None;
            TranscriptionJobName = None;
            TranscriptionJobsModel = None;
            TranscriptURI = None;
        }
    member x.InitialArgs = 4
    member x.Update response =
        match response with
        | TaskResponse.SetArgument arg ->
            match arg with
            | :? AWSShareIntraModule as awsRequestIntraModule ->
                match awsRequestIntraModule.Argument with
                | SetAWSInterface awsInterface -> { x with AWSInterface = Some(awsInterface) }
                | SetBucket bucket -> { x with S3Bucket = Some(bucket) }
                | SetBucketsModel bucketModel -> { x with S3BucketModel = Some(bucketModel) }
                | SetS3MediaReference s3MediaReference -> { x with S3MediaReference = Some(s3MediaReference) }
                | SetTranscriptionJobName transcriptionJobName -> { x with TranscriptionJobName = Some(transcriptionJobName) }
                | SetTranscriptURI transcriptURI -> { x with TranscriptURI = Some(transcriptURI) }
                | SetTranscriptionJobsModel transcriptionJobsModel -> { x with TranscriptionJobsModel = Some(transcriptionJobsModel) }
                | SetFileMediaReference fileMediaReference -> { x with FileMediaReference = Some(fileMediaReference) }
                | SetTranscriptionLanguageCode transcriptionLanguageCode -> { x with TranscriptionLanguageCode = Some(transcriptionLanguageCode) }
                | SetVocabularyName vocabularyName -> { x with VocabularyName = Some(vocabularyName) }
                // the following are unused for now
                | SetTranslationLanguageCode _translationLanguageCode -> x
            | :? StandardShareIntraModule as stdRequestIntraModule ->
                match stdRequestIntraModule.Argument with
                | SetNotificationsList notifications -> { x with Notifications = Some(notifications) }
                | SetTaskItem taskItem -> { x with TaskItem = taskItem }
                | SetWorkingDirectory workingDirectory -> { x with WorkingDirectory = workingDirectory }
            | _ -> x    // the request is not of type AWSShareIntraModule or StandardShareIntraModule therefore don't process it

        | _ -> invalidArg "response" "Expected SetArgument in AWSTaskArgumentRecord Update method"

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

        let downloadBucketItem awsInterface notifications bucketName key filePath =
            async {
                let awsResult = S3Services.DownloadItem(awsInterface, bucketName, key, filePath) |> Async.AwaitTask |> Async.RunSynchronously
                return S3Services.ProcessDownloadItemResult(notifications, awsResult)
            }

    module Transcribe =

        let startTranscriptionJob awsInterface notifications jobName bucketName s3MediaKey languageCode vocabularyName =
            async {
                let model = TranscriptionJobsViewModel()
                let _success = model.AddNewTask (awsInterface, notifications, jobName, bucketName, s3MediaKey, languageCode, vocabularyName) |> Async.AwaitTask |> Async.RunSynchronously
                return model
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
                return if success then Some(model.[jobName]) else None
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

open AWSInterface
open System.Text.RegularExpressions

[<CloudWeaverTaskFunctionModule>]
module public Tasks =

    /// Find the first unresolved request and send to the UI to resolve the value
    let private resolveByRequest (args:InfiniteList<MaybeResponse>) (required:TaskResponse[]) =
        // take all arguments that are set and map them to an AWSRequest or StandardRequest type
        let resolvedRequests =
            args
            |> Seq.takeWhile (fun mr -> mr.IsSet)
            |> Seq.choose (fun mr ->
                match mr.Value with
                | TaskResponse.SetArgument arg ->
                    match arg with
                    | :? AWSShareIntraModule as awsShareIntraModule ->
                        match awsShareIntraModule.Argument with
                        // arguments corresponding to AWSRequest items
                        | SetAWSInterface _ -> Some(AWSRequestIntraModule(RequestAWSInterface) :> IRequestIntraModule)
                        | SetBucket _ -> Some(AWSRequestIntraModule(RequestBucket) :> IRequestIntraModule)
                        | SetFileMediaReference _ -> Some(AWSRequestIntraModule(RequestFileMediaReference) :> IRequestIntraModule)
                        | SetS3MediaReference _ -> Some(AWSRequestIntraModule(RequestS3MediaReference) :> IRequestIntraModule)
                        | SetTranscriptionJobName _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobName) :> IRequestIntraModule)
                        | SetTranscriptURI _ -> Some(AWSRequestIntraModule(RequestTranscriptURI) :> IRequestIntraModule)
                        | SetTranscriptionLanguageCode _ -> Some(AWSRequestIntraModule(RequestTranscriptionLanguageCode) :> IRequestIntraModule)
                        | SetTranslationLanguageCode _ -> Some(AWSRequestIntraModule(RequestTranslationLanguageCode) :> IRequestIntraModule)
                        | SetVocabularyName _ -> Some(AWSRequestIntraModule(RequestVocabularyName) :> IRequestIntraModule)

                        // ignore: these are resolved internally within a task function (therefore no Request is ever generated)
                        | SetBucketsModel _ -> None
                        | SetTranscriptionJobsModel _ -> None       // see also AWSDisplayValue.DisplayTranscriptionJobsModel
                    | :? StandardShareIntraModule as stdShareIntraModule ->
                        match stdShareIntraModule.Argument with
                        | SetNotificationsList _ -> Some(StandardRequestIntraModule(RequestNotifications) :> IRequestIntraModule)
                        | SetTaskItem _ -> Some(StandardRequestIntraModule(RequestTaskItem) :> IRequestIntraModule)
                        | SetWorkingDirectory _ -> Some(StandardRequestIntraModule(RequestWorkingDirectory) :> IRequestIntraModule)
                    | _ -> None     // ignore request types from other modules
                | _ -> None
            )
            |> Set.ofSeq

        let unresolvedRequests =
            required
            |> Seq.choose(fun response ->
                match response with
                | TaskResponse.RequestArgument arg -> Some(arg)
                | _ -> None
            )
            |> Seq.filter (fun request -> not (resolvedRequests.Contains(request)))
            |> Seq.map (fun request -> TaskResponse.RequestArgument request)

        let requestStack = Stack(unresolvedRequests)

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
                        yield (AWSArgument.SetBucketsModel model).toSetArgumentTaskResponse()

                if bucketModel.IsSome && selectedBucket.IsSome then
                    let bucketName = selectedBucket.Value.Name
                    yield TaskResponse.TaskInfo (sprintf "Retrieving bucket items from %s..." bucketName)

                    let model = S3.getBucketItems awsInterface notifications bucketName |> Async.RunSynchronously
                    yield! getNotificationResponse notifications
                    yield TaskResponse.ShowValue (AWSShowIntraModule(AWSDisplayValue.DisplayBucketItemsModel model))

                    yield TaskResponse.TaskComplete "Finished"
            }

        seq {
            // Eventually expecting three arguments: SetBucketsModel, SetBucket, SetBucketItemsModel

            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.AWSInterface.IsSome && resolvedRecord.Notifications.IsSome then
                yield! showS3Data resolvedRecord
            else
                yield! resolveByRequest resolvable_arguments [|
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
                    |]
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
            let taskInfo = argsRecord.TaskItem.Value

            // assert the following may be None on first call
            let transcriptionJobsModel = argsRecord.TranscriptionJobsModel

            seq {
                if transcriptionJobsModel.IsNone then
                    yield TaskResponse.TaskInfo (sprintf "Running %s" taskInfo.Description)

                    yield TaskResponse.TaskInfo "Retrieving transcription jobs..."

                    let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                    yield! getNotificationResponse notifications
                    yield (AWSArgument.SetTranscriptionJobsModel model).toSetArgumentTaskResponse()
                    yield TaskResponse.TaskPrompt "Delete all completed transcription jobs?"

                if transcriptionJobsModel.IsSome then
                    let model = transcriptionJobsModel.Value

                    if hasDeleteableJobs model then
                        yield! deleteAll awsInterface notifications model
                        yield! getNotificationResponse notifications

                        let model = Transcribe.listTranscriptionJobs awsInterface notifications |> Async.RunSynchronously
                        yield! getNotificationResponse notifications
                        yield TaskResponse.ShowValue (AWSShowIntraModule(AWSDisplayValue.DisplayTranscriptionJobsModel model))
                    else
                        yield TaskResponse.TaskInfo "No transcription jobs to delete"

                    yield TaskResponse.TaskComplete (sprintf "Completed %s" taskInfo.Description)
            }

        seq {
            // eventually expecting four arguments: AWSInterface, Notifications, TaskItem and TranscriptionJobsModel
            // of which the first three must be resolved in advance

            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.AWSInterface.IsSome && resolvedRecord.Notifications.IsSome && resolvedRecord.TaskItem.IsSome then
                yield! cleanHistory resolvedRecord
            else
                yield! resolveByRequest resolvable_arguments [|
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskItem));
                    |]
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
                { ModuleName = "CloudWeaver.AWS.Tasks"; TaskName = "CleanTranscriptionJobHistory"; Description = "Transcription Job History" };
                { ModuleName = "CloudWeaver.AWS.Tasks"; TaskName = "SomeSubTask"; Description = "Other" }
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
                    yield (AWSArgument.SetS3MediaReference (S3MediaReference(bucketName, newKey, media.MimeType, media.Extension))).toSetArgumentTaskResponse()
                yield TaskResponse.TaskComplete "Uploaded media file"
            }

        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.AWSInterface.IsSome && resolvedRecord.Notifications.IsSome &&
                resolvedRecord.S3Bucket.IsSome && resolvedRecord.FileMediaReference.IsSome then
                yield! uploadMediaFile resolvedRecord
            else
                yield! resolveByRequest resolvable_arguments [|
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestBucket));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestFileMediaReference));
                    |]
        }

    [<HideFromUI>]
    let StartTranscription (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let startTranscription argsRecord =
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value

            let s3Media = argsRecord.S3MediaReference.Value
            let languageCode = argsRecord.TranscriptionLanguageCode.Value
            let vocabularyName = argsRecord.VocabularyName.Value

            let jobName = Guid.NewGuid().ToString()

            // note: the task name may be used as the output S3 key
            let jobsModel = Transcribe.startTranscriptionJob awsInterface notifications jobName s3Media.BucketName s3Media.Key languageCode vocabularyName |> Async.RunSynchronously

            seq {
                yield! getNotificationResponse notifications
                yield (AWSArgument.SetTranscriptionJobName jobName).toSetArgumentTaskResponse()
                yield TaskResponse.ShowValue (AWSShowIntraModule(AWSDisplayValue.DisplayTranscriptionJobsModel jobsModel))
                yield TaskResponse.TaskComplete "Transcription started"
            }

        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.AWSInterface.IsSome && resolvedRecord.Notifications.IsSome &&
                resolvedRecord.S3MediaReference.IsSome && resolvedRecord.TranscriptionLanguageCode.IsSome && resolvedRecord.VocabularyName.IsSome then
                yield! startTranscription resolvedRecord
            else
                yield! resolveByRequest resolvable_arguments [|
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestS3MediaReference));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestVocabularyName));
                    |]
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
                        yield (AWSArgument.SetTranscriptURI jobsModel.Value.OutputURI).toSetArgumentTaskResponse()
                        yield TaskResponse.ShowValue (AWSShowIntraModule(AWSDisplayValue.DisplayTranscriptionJob jobsModel.Value))
                        yield TaskResponse.TaskComplete "Transcription Job Completed"
                    else
                        yield TaskResponse.TaskInfo "Querying job status"
                        yield TaskResponse.TaskContinue 3000
            }

        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.AWSInterface.IsSome && resolvedRecord.Notifications.IsSome && resolvedRecord.TranscriptionJobName.IsSome then
                yield! monitorTranscription resolvedRecord
            else
                yield! resolveByRequest resolvable_arguments [|
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobName));
                    |]
        }

    //[<HideFromUI>]
    //let DownloadTranscriptFile (resolvable_arguments: InfiniteList<MaybeResponse>) =

    //    let downloadMediaFile argsRecord =
    //        let awsInterface = argsRecord.AWSInterface.Value
    //        let notifications = argsRecord.Notifications.Value

    //        let transcriptURI = argsRecord.TranscriptURI.Value
    //        let matched = Regex.Match(transcriptURI, "poo")
    //        let bucketName = matched.Captures
    //        let key = matched.Captures

    //        let success = S3.downloadBucketItem awsInterface notifications bucketName key filePath |> Async.RunSynchronously

    //        seq {
    //            yield! getNotificationResponse notifications
    //            if success then
    //                yield (AWSArgument.SetS3MediaReference (S3MediaReference(bucketName, newKey, media.MimeType, media.Extension))).toSetArgumentTaskResponse()
    //            yield TaskResponse.TaskComplete "Uploaded media file"
    //        }

    //    seq {
    //        let defaultArgs = TaskArgumentRecord.Init ()
    //        let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

    //        if resolvedRecord.AWSInterface.IsSome && resolvedRecord.Notifications.IsSome &&
    //            resolvedRecord.TranscriptionJob.IsSome then
    //            yield! downloadMediaFile resolvedRecord
    //        else
    //            yield! resolveByRequest resolvable_arguments [|
    //                TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
    //                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
    //                TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptURI));
    //                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
    //                |]
    //    }

    /// Upload and transcribe some audio
    /// The function is called multiple times from the UI until all arguments are resolved
    let TranscribeAudio (resolvable_arguments: InfiniteList<MaybeResponse>) =
        
        seq {
            let defaultArgs = TaskArgumentRecord.Init ()
            let resolvedRecord = integrateUIRequestArguments resolvable_arguments defaultArgs

            if resolvedRecord.S3Bucket.IsSome && resolvedRecord.FileMediaReference.IsSome
                && resolvedRecord.TranscriptionLanguageCode.IsSome && resolvedRecord.VocabularyName.IsSome then
                // restored from a previous session OR resolved by request to the UI
                yield TaskResponse.TaskArgumentSave     // save the resolved arguments (if not already saved)
                yield TaskResponse.TaskSequence ([|
                    { ModuleName = "CloudWeaver.AWS.Tasks"; TaskName = "UploadMediaFile"; Description = "Upload a media file to transcribe" };
                    { ModuleName = "CloudWeaver.AWS.Tasks"; TaskName = "StartTranscription"; Description = "Start a transcription job" };
                    { ModuleName = "CloudWeaver.AWS.Tasks"; TaskName = "MonitorTranscription"; Description = "Monitor the transcription job" }
                |])
                yield TaskResponse.TaskComplete ""
            else
                yield! resolveByRequest resolvable_arguments [|
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestVocabularyName));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestFileMediaReference));
                    TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestBucket));
                |]
        }

/// Tasks within a task (for S3FetchItems task)
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
    
    type MiniTaskContext =
        | BucketItemsModel of BucketItemViewModel

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
    
    let checkContextIs requiredContext (context:MiniTaskContext) =
        match context with
        | BucketItemsModel _ -> 
            if context = requiredContext then
                true
            else
                false
        
    let checkContextIsBucketItemsModel (context:MiniTaskContext) = true
        //match context with
        //| BucketItemsModel(_) -> true
        //| _ -> false

    let Delete awsInterface (notifications: NotificationsList) (context:MiniTaskContext) (args:MiniTaskArgument[]) =
        if args.Length >= 2 then
            if checkContextIsBucketItemsModel context then
                let (bucketName, key) =
                    match (args.[0], args.[1]) with
                    | (MiniTaskArgument.String a, MiniTaskArgument.String b) -> (a, b)
                    | _ -> invalidArg "args" "MiniTasks.Delete: Expecting two string arguments"
                let success = S3.deleteBucketItem awsInterface notifications bucketName key |> Async.RunSynchronously
                TaskUpdate.DeleteBucketItem (notifications, success, key)
            else
                invalidArg "context" "MiniTasks.Delete: Unrecognized context"
        else invalidArg "args" "MiniTasks.Delete: Expecting two arguments"

    let Download awsInterface (notifications: NotificationsList) (context:MiniTaskContext) (args:MiniTaskArgument[]) =
        if args.Length >= 3 then
            if checkContextIsBucketItemsModel context then
                let (bucketName, key, filePath) =
                    match (args.[0], args.[1], args.[2]) with
                    | (MiniTaskArgument.String a, MiniTaskArgument.String b, MiniTaskArgument.String c) -> (a, b, c)
                    | _ -> invalidArg "args" "MiniTasks.Download: Expecting three string arguments"
                let success = S3.downloadBucketItem awsInterface notifications bucketName key filePath |> Async.RunSynchronously
                TaskUpdate.DownloadBucketItem (notifications, success, key, filePath)
            else
                invalidArg "context" "MiniTasks.Download: Unrecognized context"
        else invalidArg "args" "MiniTasks.Download: Expecting three arguments"
