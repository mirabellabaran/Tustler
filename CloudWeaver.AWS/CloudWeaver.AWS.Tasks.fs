namespace CloudWeaver.AWS

open System
open TustlerServicesLib
open TustlerModels
open System.Collections.Generic
open CloudWeaver.Types

open TustlerAWSLib
open System.IO
open AWSInterface
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Text.Json

[<CloudWeaverTaskFunctionModule>]
module public Tasks =

    let private mapResponseToRequest (response: TaskResponse) =
        match response with
        | TaskResponse.SetArgument arg ->
            match arg with
            | :? AWSShareIntraModule as awsShareIntraModule ->
                match awsShareIntraModule.Argument with
                // arguments corresponding to AWSRequest items
                | SetAWSInterface _ -> Some(AWSRequestIntraModule(RequestAWSInterface) :> IRequestIntraModule)
                | SetBucket _ -> Some(AWSRequestIntraModule(RequestBucket) :> IRequestIntraModule)
                | SetBucketsModel _ -> Some(AWSRequestIntraModule(RequestBucketsModel) :> IRequestIntraModule)
                | SetS3MediaReference _ -> Some(AWSRequestIntraModule(RequestS3MediaReference) :> IRequestIntraModule)
                | SetTranscriptionJobsModel _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobsModel) :> IRequestIntraModule)  // see also AWSDisplayValue.DisplayTranscriptionJobsModel
                | SetTranscriptionJobName _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobName) :> IRequestIntraModule)
                | SetTranscriptJSON _ -> Some(AWSRequestIntraModule(RequestTranscriptJSON) :> IRequestIntraModule)
                | SetTranscriptionDefaultTranscript _ -> Some(AWSRequestIntraModule(RequestTranscriptionDefaultTranscript) :> IRequestIntraModule)
                | SetTranscriptURI _ -> Some(AWSRequestIntraModule(RequestTranscriptURI) :> IRequestIntraModule)
                //| SetTranscriptionLanguageCode _ -> Some(AWSRequestIntraModule(RequestTranscriptionLanguageCode) :> IRequestIntraModule)
                //| SetTranslationLanguageCodeSource _ -> Some(AWSRequestIntraModule(RequestTranslationLanguageCodeSource) :> IRequestIntraModule)
                | SetLanguage lang when lang.LanguageDomain = LanguageDomain.Transcription -> Some(AWSRequestIntraModule(RequestTranscriptionLanguageCode) :> IRequestIntraModule)
                | SetLanguage lang when lang.LanguageDomain = LanguageDomain.Translation -> Some(AWSRequestIntraModule(RequestTranslationLanguageCodeSource) :> IRequestIntraModule)
                | SetLanguage _ -> invalidArg "arg" "Unexpected SetLanguage parameters"
                | SetTranslationTargetLanguages _ -> Some(AWSRequestIntraModule(RequestTranslationTargetLanguages) :> IRequestIntraModule)
                | SetTranscriptionVocabularyName _ -> Some(AWSRequestIntraModule(RequestTranscriptionVocabularyName) :> IRequestIntraModule)
                | SetTranslationTerminologyNames _ -> Some(AWSRequestIntraModule(RequestTranslationTerminologyNames) :> IRequestIntraModule)
                | SetTranslationSegments _ -> Some(AWSRequestIntraModule(RequestTranslationSegments) :> IRequestIntraModule)
                | SetSubtitleFilePath _ -> Some(AWSRequestIntraModule(RequestSubtitleFilePath) :> IRequestIntraModule)
            | :? StandardShareIntraModule as stdShareIntraModule ->
                match stdShareIntraModule.Argument with
                | SetNotificationsList _ -> Some(StandardRequestIntraModule(RequestNotifications) :> IRequestIntraModule)
                | SetTaskIdentifier _ -> Some(StandardRequestIntraModule(RequestTaskIdentifier) :> IRequestIntraModule)
                | SetTaskItem _ -> Some(StandardRequestIntraModule(RequestTaskItem) :> IRequestIntraModule)
                | SetWorkingDirectory _ -> Some(StandardRequestIntraModule(RequestWorkingDirectory) :> IRequestIntraModule)
                | SetSaveFlags _ -> Some(StandardRequestIntraModule(RequestSaveFlags) :> IRequestIntraModule)
                | SetJsonEvents _ -> Some(StandardRequestIntraModule(RequestJsonEvents) :> IRequestIntraModule)
                | SetFileMediaReference _ -> Some(StandardRequestIntraModule(RequestFileMediaReference) :> IRequestIntraModule)
                | SetLogFormatEvents _ -> Some(StandardRequestIntraModule(RequestLogFormatEvents) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Open && path.Extension = "json" -> Some(StandardRequestIntraModule(RequestOpenJsonFilePath) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Save && path.Extension = "json" -> Some(StandardRequestIntraModule(RequestSaveJsonFilePath) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Open && path.Extension = "bin" -> Some(StandardRequestIntraModule(RequestOpenLogFormatFilePath) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Save && path.Extension = "bin" -> Some(StandardRequestIntraModule(RequestSaveLogFormatFilePath) :> IRequestIntraModule)
                | SetFilePath _ -> invalidArg "arg" "Unexpected SetFilePath parameters"

            | _ -> None     // ignore request types from other modules
        | _ -> None

    /// Get all requests that are not yet set
    let private getUnResolvedRequests (argMap: Map<IRequestIntraModule, IShareIntraModule>) (required: TaskResponse []) =
        required
        |> Seq.map (fun response ->
            match response with
            | TaskResponse.RequestArgument arg -> arg
            | _ -> invalidArg "response" "Expected RequestArgument in when checking input arguments"
        )
        |> Seq.filter (fun request -> not (argMap.ContainsKey request))
        |> Seq.map (fun request -> TaskResponse.RequestArgument request)
        |> Seq.toArray

    // Get the first unresolved request and send to the UI to resolve the value
    let private resolveByRequest (unresolvedRequests: TaskResponse []) =
    //let private resolveByRequest (args:InfiniteList<MaybeResponse>) (required:TaskResponse[]) =
        //// take all arguments that are set and map them to an AWSRequest or StandardRequest type
        //let resolvedRequests =
        //    args
        //    |> Seq.takeWhile (fun mr -> mr.IsSet)
        //    |> Seq.choose (fun mr ->
        //        mapResponseToRequest mr.Value
        //    )
        //    |> Set.ofSeq

        //let unresolvedRequests =
        //    required
        //    |> Seq.choose(fun response ->
        //        match response with
        //        | TaskResponse.RequestArgument arg -> Some(arg)
        //        | _ -> None
        //    )
        //    |> Seq.filter (fun request -> not (resolvedRequests.Contains(request)))
        //    |> Seq.map (fun request -> TaskResponse.RequestArgument request)

        let requestStack = Stack(unresolvedRequests)

        if requestStack.Count > 0 then
            Seq.singleton (requestStack.Pop())
            //Seq.cast requestStack
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

    ///// Integrate with the default record any request arguments that have been set using the TaskArgumentRecord updater function
    //let private integrateUIRequestArguments (args:InfiniteList<MaybeResponse>) (defaultArgs:TaskArgumentRecord) =
    //    args
    //    |> Seq.takeWhile (fun mr -> mr.IsSet)
    //    |> Seq.fold (fun (argsRecord:TaskArgumentRecord) mr -> argsRecord.Update mr.Value) defaultArgs

    /// Create a map that contains those request arguments that are currently set
    let private integrateUIRequestArguments (args:InfiniteList<MaybeResponse>) =
        args
        |> Seq.takeWhile (fun mr -> mr.IsSet)
        |> Seq.fold (fun (map:Map<_,_>) mr ->
            let request = mapResponseToRequest mr.Value
            let response =
                match mr.Value with
                | TaskResponse.SetArgument arg -> Some(arg)
                | _ -> None
            if request.IsSome && response.IsSome then
                map.Add (request.Value, response.Value)
            else
                map
        ) (Map.empty)

    let private getLastSetArgument (args:InfiniteList<MaybeResponse>) =
        args
        |> Seq.takeWhile (fun mr -> mr.IsSet)
        |> Seq.choose (fun mr ->
            match mr.Value with
            | TaskResponse.SetArgument arg -> Some(arg)
            | _ -> None
        )
        |> Seq.tryLast

    ///// Integrate with the default record any request arguments that have been set using the TaskArgumentRecord updater function
    //let private integrateUIRequestArguments (args:InfiniteList<MaybeResponse>) (defaultArgs:TaskArgumentRecord) =
    //    args
    //    |> Seq.takeWhile (fun mr -> mr.IsSet)
    //    |> Seq.fold (fun (argsRecord:TaskArgumentRecord) mr -> argsRecord.Update mr.Value) defaultArgs

    /// Validate the supplied arguments by type and position; all or some of the arguments can be unset (MaybeResponse.IsNotSet)
    //let private validateArgs expectedNum argChecker (args: InfiniteList<MaybeResponse>) =
    //    if args.Count > expectedNum then
    //        invalidArg "expectedNum" (sprintf "Expecting up to %d set argument values" expectedNum)
    //    args
    //    |> Seq.takeWhile (fun mr -> mr.IsSet)   // only examine arguments that are set
    //    |> Seq.iteri(fun index mr ->
    //        match mr with
    //        | MaybeResponse.Just tr -> argChecker index tr
    //        | MaybeResponse.Nothing -> ()
    //    )

    /// Get any notifications generated from the last AWS call (errors or informational messages)
    let private getNotificationResponse (notifications: NotificationsList) =
        Seq.map (fun note -> TaskResponse.Notification note) notifications.Notifications

    // A minimal task function that does nothing
    [<HideFromUI>]
    let MinimalFunction (queryMode: TaskFunctionQueryMode) (_args: InfiniteList<MaybeResponse>) =
    
        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "A sample task function that does nothing")
        | Inputs -> Seq.empty
        | Outputs -> Seq.empty
        | Invoke ->
            seq { yield TaskResponse.TaskInfo "Minimal task function" }

    let SaveLastArgument(queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let saveLastArgument argMap (lastArg: IShareIntraModule) =

            let argsRecord = {|
                TaskIdentifier = PatternMatchers.getTaskIdentifier argMap;
                WorkingDirectory = PatternMatchers.getWorkingDirectory argMap;
                TranslationTargetLanguages = PatternMatchers.getTranslationTargetLanguages argMap;
                TranslationSegments = PatternMatchers.getTranslationSegments argMap;
            |}

            let taskId = argsRecord.TaskIdentifier.Value
            let workingDirectory = argsRecord.WorkingDirectory.Value
            //let targetLanguageCode = PatternMatchers.Consumable.getLanguageCode (argsRecord.TranslationTargetLanguages.Value)
            //let chunker = argsRecord.TranslationSegments.Value

            //let writerOptions = JsonWriterOptions(Indented = false)
            //let serializerOptions = JsonSerializerOptions()
            //serializerOptions.Converters.Add(RetainingStackConverter())
            //serializerOptions.Converters.Add(TaskSequenceConverter())
            //serializerOptions.Converters.Add(SentenceChunkerConverter())

            //use stream = new MemoryStream()
            //let result = using (new Utf8JsonWriter(stream, writerOptions)) (fun writer ->
            //    lastArg.Serialize writer serializerOptions
            //    writer.Flush()
            //    stream.ToArray()
            //)
            let result: byte[] = Array.empty

            seq {
                // MG add Label to IShareIntraModule
                let fileName = "poo"   //sprintf "Translation-%s-%s.txt" taskId.Value targetLanguageCode.Code
                let filePath = Path.Combine(workingDirectory.Value.FullName, fileName)
                File.WriteAllBytes(filePath, result)

                yield TaskResponse.TaskInfo (sprintf "Working directory is: %s" workingDirectory.Value.FullName)
                yield TaskResponse.TaskComplete ((sprintf "Saved translation to %s" fileName), DateTime.Now)
            }

        let inputs = [|
             TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
             TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationSegments));
             TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages));
             TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
             TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
         |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Save the last argument added to the event stack")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestBucketsModel)))
        | Invoke ->
            seq {
                let lastArg = getLastSetArgument resolvable_arguments

                if lastArg.IsSome then
                    let argMap = integrateUIRequestArguments resolvable_arguments
                    let unresolvedRequests = getUnResolvedRequests argMap inputs

                    if unresolvedRequests.Length = 0 then
                        yield! saveLastArgument argMap lastArg.Value
                    else
                        yield! resolveByRequest unresolvedRequests
                else
                    yield TaskResponse.TaskComplete ("Nothing to save (no argument set)", DateTime.Now)
            }


    /// Read the default arguments file in the working directory (if it exists) and request the UI to select which arguments to set
    let ReadDefaultArguments (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =
        ()





    let S3FetchItems (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let showS3Data argMap =

            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                S3BucketModel = PatternMatchers.getBucketsModel argMap;
                S3Bucket = PatternMatchers.getBucket argMap;
            |}

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

                    yield TaskResponse.TaskComplete ("Finished", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Show the items stored in Amazon S3 buckets")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestBucketsModel)))
        | Invoke ->
            seq {
                // Eventually expecting three arguments: SetBucketsModel, SetBucket, SetBucketItemsModel

                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! showS3Data argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let CleanTranscriptionJobHistory (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

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

        let cleanHistory argMap =

            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                TaskItem = PatternMatchers.getTaskItem argMap;
                TranscriptionJobsModel = PatternMatchers.getTranscriptionJobsModel argMap;
            |}

            // assert the following as always set
            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value
            let taskInfo = argsRecord.TaskItem.Value

            // assert the following may be None on first call
            let transcriptionJobsModel = argsRecord.TranscriptionJobsModel

            seq {
                if taskInfo.IsSome then
                    if transcriptionJobsModel.IsNone then
                        yield TaskResponse.TaskInfo (sprintf "Running %s" taskInfo.Value.Description)

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

                        yield TaskResponse.TaskComplete ((sprintf "Completed %s" taskInfo.Value.Description), DateTime.Now)
                else
                    yield TaskResponse.TaskComplete ("Check value of variable: TaskItem", DateTime.Now)                    
            }

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskItem));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Clean up the transcription job history stored on the AWS Transcribe Service")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobsModel)))
        | Invoke ->
            seq {
                // eventually expecting four arguments: AWSInterface, Notifications, TaskItem and TranscriptionJobsModel
                // of which the first three must be resolved in advance

                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! cleanHistory argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let SomeSubTask (queryMode: TaskFunctionQueryMode) (_args: InfiniteList<MaybeResponse>) =

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "A temporary sample cleanup task")
        | Inputs -> Seq.empty
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                yield TaskResponse.TaskInfo "Doing SomeSubTask"

                yield TaskResponse.TaskComplete ("Finished SomeSubTask", DateTime.Now)
            }

    let Cleanup (queryMode: TaskFunctionQueryMode) (args: InfiniteList<MaybeResponse>) =

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Choose from a selection of cleanup tasks")
        | Inputs -> Seq.empty
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                // show the sub-task names (the TaskName is used for function selection)
                yield TaskResponse.TaskMultiSelect ([|
                    TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "CleanTranscriptionJobHistory", Description = "Transcription Job History");
                    TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "SomeSubTask", Description = "Other");
                |])
            }

    [<HideFromUI>]
    let UploadMediaFile (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let uploadMediaFile argMap =
            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                S3Bucket = PatternMatchers.getBucket argMap;
                FileMediaReference = PatternMatchers.getFileMediaReference argMap;
            |}

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
                yield TaskResponse.TaskComplete ("Uploaded media file", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestBucket));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestFileMediaReference));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Upload a media file to Amazon S3")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestS3MediaReference)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! uploadMediaFile argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let StartTranscription (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let startTranscription argMap =
            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                S3MediaReference = PatternMatchers.getS3MediaReference argMap;
                TranscriptionLanguageCode = PatternMatchers.getTranscriptionLanguageCode argMap;
                TranscriptionVocabularyName = PatternMatchers.getTranscriptionVocabularyName argMap;
            |}

            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value

            let s3Media = argsRecord.S3MediaReference.Value
            let languageCode = argsRecord.TranscriptionLanguageCode.Value
            let vocabularyName = argsRecord.TranscriptionVocabularyName.Value

            let jobName = Guid.NewGuid().ToString()

            // note: the task name may be used as the output S3 key
            let jobsModel = Transcribe.startTranscriptionJob awsInterface notifications jobName s3Media.BucketName s3Media.Key (languageCode.Code) vocabularyName |> Async.RunSynchronously

            seq {
                yield! getNotificationResponse notifications
                yield (AWSArgument.SetTranscriptionJobName jobName).toSetArgumentTaskResponse()
                yield TaskResponse.ShowValue (AWSShowIntraModule(AWSDisplayValue.DisplayTranscriptionJobsModel jobsModel))
                yield TaskResponse.TaskComplete ("Transcription started", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestS3MediaReference));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Start the transcription of a media file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobName)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! startTranscription argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let MonitorTranscription (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let monitorTranscription argMap =
            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                TranscriptionJobName = PatternMatchers.getTranscriptionJobName argMap;
            |}

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
                        yield TaskResponse.TaskComplete ("Transcription Job Completed", DateTime.Now)
                    else
                        yield TaskResponse.TaskInfo "Querying job status"
                        yield TaskResponse.TaskContinue 1000
            }

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobName));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Monitor the transcription of a media file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptURI)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! monitorTranscription argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let DownloadTranscriptFile (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let (|CompiledMatch|_|) pattern input =
            if input = null then None
            else
                let m = Regex.Match(input, pattern, RegexOptions.Compiled)
                if m.Success then Some [for x in m.Groups -> x]
                else None

        let parseBucketItemRef fileUri =
            match fileUri with
            | CompiledMatch @"^https://s3\..*\.amazonaws\.com/(\w+)/(.*)$" [_; bucket; key] ->
                Some((bucket.Value, key.Value))
            | _ -> 
                None

        let downloadTranscriptFile argMap =
            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                TranscriptURI = PatternMatchers.getTranscriptURI argMap;
                TaskIdentifier = PatternMatchers.getTaskIdentifier argMap;
                WorkingDirectory = PatternMatchers.getWorkingDirectory argMap;
                SaveFlags = PatternMatchers.getSaveFlags argMap;
            |}

            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value
            let transcriptURI = argsRecord.TranscriptURI.Value      // e.g. https://s3.ap-southeast-2.amazonaws.com/tator/d2a8856b-bd9a-49bf-a54a-5d91df4b73f7.json
            let taskId = argsRecord.TaskIdentifier.Value
            let workingDirectory = argsRecord.WorkingDirectory.Value
            let saveFlags = argsRecord.SaveFlags.Value

            seq {
                let parsed = parseBucketItemRef transcriptURI
                if parsed.IsNone then       // add to notifications
                    let message = sprintf "Error parsing transcript URI: %s" transcriptURI
                    notifications.HandleError("Task Function = DownloadTranscriptFile",
                        "The transcript URI passed as an argument is not in the correct format",
                        (new System.InvalidOperationException(message)))

                let transcriptData =
                    if parsed.IsSome then
                        let bucketName, key = parsed.Value

                        if saveFlags.IsSome && workingDirectory.IsSome && taskId.IsSome then
                            if saveFlags.Value.IsSet (AWSFlag(AWSFlagItem.TranscribeSaveJSONTranscript)) then
                                let filePath = Path.Combine(workingDirectory.Value.FullName, (sprintf "Transcript-%s.json" taskId.Value))
                                let successfulDownload = S3.downloadBucketItemToFile awsInterface notifications bucketName key filePath |> Async.RunSynchronously
                                if successfulDownload then
                                    let rawData = File.ReadAllBytesAsync(filePath) |> Async.AwaitTask |> Async.RunSynchronously
                                    Some(ReadOnlyMemory(rawData))
                                else
                                    None
                            else
                                let rawData = S3.downloadBucketItemAsBytes awsInterface notifications bucketName key |> Async.RunSynchronously
                                if isNull rawData then
                                    None
                                else
                                    Some(ReadOnlyMemory(rawData))
                        else
                            None                            
                    else
                        None

                yield! getNotificationResponse notifications
                if transcriptData.IsSome then
                    yield (AWSArgument.SetTranscriptJSON transcriptData.Value).toSetArgumentTaskResponse()
                let message =
                    if transcriptData.IsSome then
                        "Downloaded transcript file"
                    else
                        "Transcript data not downloaded. Check the settings for variables: TaskIdentifier, WorkingDirectory and SaveFlags" 
                yield TaskResponse.TaskComplete (message, DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptURI));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Download the transcript produced by transcription of a media file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! downloadTranscriptFile argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let ExtractTranscript (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let extractTranscript argMap =
            let argsRecord = {|
                Notifications = PatternMatchers.getNotifications argMap;
                TranscriptJSON = PatternMatchers.getTranscriptJSON argMap;
            |}

            let notifications = argsRecord.Notifications.Value
            let transcriptJSON = argsRecord.TranscriptJSON.Value

            seq {
                let defaultTranscript = TustlerAWSLib.Utilities.TranscriptParser.ParseTranscriptData(transcriptJSON, notifications) |> Async.AwaitTask |> Async.RunSynchronously
                if notifications.Notifications.Count > 0 then
                    yield! getNotificationResponse notifications
                if not (isNull defaultTranscript) then
                    yield (AWSArgument.SetTranscriptionDefaultTranscript defaultTranscript).toSetArgumentTaskResponse()

                yield TaskResponse.TaskComplete ("Extracted transcript data", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Extract the default text from a transcribed media file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! extractTranscript argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let SaveTranscript (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let saveTranscript argMap =
            let argsRecord = {|
                DefaultTranscript = PatternMatchers.getTranscriptionDefaultTranscript argMap;
                WorkingDirectory = PatternMatchers.getWorkingDirectory argMap;
                TaskIdentifier = PatternMatchers.getTaskIdentifier argMap;
            |}

            let defaultTranscript = argsRecord.DefaultTranscript.Value
            let workingDirectory = argsRecord.WorkingDirectory.Value
            let taskId = argsRecord.TaskIdentifier.Value
            seq {
                if workingDirectory.IsSome && taskId.IsSome then
                    let filePath = Path.Combine(workingDirectory.Value.FullName, (sprintf "Transcript-%s.txt" taskId.Value))
                    File.WriteAllTextAsync(filePath, defaultTranscript) |> Async.AwaitTask |> Async.RunSynchronously
                    yield TaskResponse.TaskComplete ((sprintf "Saved transcript data to %s" filePath), DateTime.Now)
                else
                    yield TaskResponse.TaskComplete ("Check variables: WorkingDirectory && TaskIdentifier", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Save the default text from a transcribed media file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap [|
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
                    |]

                if unresolvedRequests.Length = 0 then
                    let saveFlags = (PatternMatchers.getSaveFlags argMap).Value

                    if saveFlags.IsSome then
                        if saveFlags.Value.IsSet (AWSFlag(AWSFlagItem.TranscribeSaveDefaultTranscript)) then
                            let unresolvedRequests = getUnResolvedRequests argMap [|
                                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
                                TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript));
                                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
                                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
                                |]

                            if unresolvedRequests.Length = 0 then
                                yield! saveTranscript argMap
                            else
                                yield! resolveByRequest unresolvedRequests
                        else
                            yield TaskResponse.TaskComplete ("Save flag not set (TranscribeSaveDefaultTranscript)", DateTime.Now)
                    else
                        yield TaskResponse.TaskComplete ("Check variable: SaveFlags", DateTime.Now)
                else
                    yield! resolveByRequest unresolvedRequests
            }

    /// Upload and transcribe some audio
    /// The function is called multiple times from the UI until all arguments are resolved
    [<EnableLogging>]
    let TranscribeAudio (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =
        
        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestFileMediaReference));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestBucket));

            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
        |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Transcribe an audio file and extract and save the transcripted text")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    // restored from a previous session OR resolved by request to the UI
                    yield TaskResponse.TaskSaveEvents SaveEventsFilter.ArgumentsOnly     // save the resolved arguments (if not already saved)
                    yield TaskResponse.TaskSequence ([|
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "UploadMediaFile", Description = "Upload a media file to transcribe");
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "StartTranscription", Description = "Start a transcription job");
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "MonitorTranscription", Description = "Monitor the transcription job");
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "DownloadTranscriptFile", Description = "Download the transcription job output file from S3");
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "ExtractTranscript", Description = "Extract the transcript from the transcription job output file");
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "SaveTranscript", Description = "Save the extracted transcript to a file");
                    |])
                    yield TaskResponse.TaskComplete ("Starting task", DateTime.Now)
                else
                    yield! resolveByRequest unresolvedRequests
            }


    [<HideFromUI>]
    let CreateSubTitles (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        // Subtitle lines need to:
        //  last for at least three seconds
        //  be no more than ten words long
        //  be preferentially broken after punctuation boundaries

        let makeSubtitles (timingData: seq<Utilities.WordTiming>) =
            let makeSubTitleLine (words: (double * string) list) =
                let ordered = List.rev words
                let startTime, _ = List.head ordered
                let orderedWords =
                    ordered
                    |> Seq.map (fun (_startTime, word) -> word)
                let sentence = System.Text.StringBuilder(words.Length + 3)
                                .Append("[").Append(startTime).Append("]").Append(" ").AppendJoin(" ", orderedWords).Append(".")
                sentence.ToString()

            let words, sentences =
                timingData
                |> Seq.fold (fun (words, sentences) wordTiming ->
                    if wordTiming.Type = Utilities.WordTiming.WordType.Punctuation && wordTiming.Content = "." then
                        let sentence = makeSubTitleLine words
                        ([], sentence :: sentences)
                    else
                        ((wordTiming.StartTime, wordTiming.Content) :: words, sentences)
                ) ([], [])

            let result =
                if not words.IsEmpty then
                    let finalSentence = makeSubTitleLine words
                    finalSentence :: sentences
                else
                    sentences

            result
            |> List.rev
            |> List.toArray

        let createSubTitles argMap =
            let argsRecord = {|
                Notifications = PatternMatchers.getNotifications argMap;
                TranscriptJSON = PatternMatchers.getTranscriptJSON argMap;
                SubtitleFilePath = PatternMatchers.getSubtitleFilePath argMap;
            |}

            let notifications = argsRecord.Notifications.Value
            let transcriptJSON = argsRecord.TranscriptJSON.Value
            let subtitleFilePath = argsRecord.SubtitleFilePath.Value

            seq {
                let timingData = TustlerAWSLib.Utilities.TranscriptParser.ParseWordTimingData(transcriptJSON, notifications) |> Async.AwaitTask |> Async.RunSynchronously
                if notifications.Notifications.Count > 0 then
                    yield! getNotificationResponse notifications                    

                let subTitles = makeSubtitles timingData
                // TODO remove spaces before commas; deal with ellipses and other special cases; split sentences that run too long

                File.WriteAllLines(subtitleFilePath.FullName, subTitles)

                yield TaskResponse.TaskComplete ("Created subtitle data", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestSubtitleFilePath));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Create a subtitles file from a JSON transcript")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! createSubTitles argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }


    [<HideFromUI>]
    let TranslateText (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        // translate a chunk and return the number of attempts (retries)
        let processChunk (translator: (int * string) -> Task<struct (bool * bool)>) (chunk:KeyValuePair<int, string>) numRetriesLastChunk maxDelayLastChunk =
            let maxRetries = SentenceChunker.MaxRetries

            // for testing purposes: record changes in state
            let state = Stack<(int * int64 * bool)>()
            state.Push((0, maxDelayLastChunk, false))

            // unfold a sequence of translation attempts (stopping on success or when the maximum number of retries has been reached)
            // each attempt sets a flag indicating a non-recoverable error
            let attempts =
                Seq.unfold (fun (currentState: Stack<(int * int64 * bool)>) ->
                    let currentRetryNum, delay, complete = currentState.Peek()

                    // limit the number of attempts
                    if complete || currentRetryNum >= maxRetries then
                        None
                    else
                        if delay > 0L then
                            Async.AwaitTask (Task.Delay((int)delay)) |> Async.RunSynchronously
                        let struct (isErrorState, recoverableError) = Async.AwaitTask (translator(chunk.Key, chunk.Value)) |> Async.RunSynchronously

                        let result =
                            if isErrorState then
                                if recoverableError then
                                    // exponential backoff
                                    let totalRetries = numRetriesLastChunk + currentRetryNum
                                    let newDelay = SentenceChunker.GetDelay(totalRetries + 1, SentenceChunker.MinSleepMilliseconds, SentenceChunker.MaxSleepMilliseconds)
                                    currentState.Push((currentRetryNum + 1, newDelay, false))
                                    (false, currentState)
                                else
                                    currentState.Push((0, 0L, true))
                                    (true, currentState)   // first field indicates a non-recoverable error
                            else
                                currentState.Push((0, 0L, true))
                                (false, currentState)
                        Some(result)
                ) state
                |> Seq.toArray

            let nonRecoverableError = attempts |> Seq.exists (fun errorState -> errorState)
            let retries = attempts.Length - 1   // ignore initial and final push
            let _, maxDelay, _ = Seq.maxBy (fun (_currentRetryNum, delay, _complete) -> delay) state
            let stateChanges =
                let stringify (items: seq<string>) = System.String.Join(", ", items)
                state
                |> Seq.skip 1       // ignore final push but keep the initial push (the sequence should just have a single zero delay followed by zero or more longer delays)
                |> Seq.rev
                |> Seq.map (fun (_currentRetryNum, delay, _complete) -> delay)
                |> Seq.map (fun tup -> tup.ToString())
                |> stringify
            (nonRecoverableError, retries, maxDelay, stateChanges)

        /// recursively process each chunk, adjusting the retry level as necessary
        let rec mapChunks index (mapping: (int -> int -> int64 -> KeyValuePair<int, string> -> TaskResponse option * int * int64)) currentRetryLevel currentDelay (chunks: (KeyValuePair<int, string>) list) =
            seq {
                if not (chunks.IsEmpty) then
                    let response, nextRetryLevel, maxDelay =
                        match Seq.tryHead chunks with
                        | Some(chunk) -> mapping index currentRetryLevel currentDelay chunk
                        | None -> None, currentRetryLevel, 0L
                    if response.IsSome then yield response.Value
                    
                    yield! mapChunks (index + 1) mapping nextRetryLevel maxDelay (chunks.Tail)
            }

        let processChunks translator (chunker: SentenceChunker) =
            let prepareInfo index stateChanges =
                if stateChanges = "0" then
                    sprintf "Segment %d completed" index
                else
                    sprintf "Segment %d completed: (delay adjustments (ms): %s)" index stateChanges

            let chunkMapper index currentRetryLevel currentDelay chunk =
                let nonRecoverableError, retries, maxDelay, stateChanges = processChunk translator chunk currentRetryLevel currentDelay
                // retry level is used to calculate the delay between AWS calls; the level can escalate but cannot de-escalate
                let result = if nonRecoverableError then None else Some(TaskResponse.TaskInfo (prepareInfo index stateChanges))
                result, retries, maxDelay

            // ignore already translated chunks
            let chunks =
                chunker.Chunks
                |> Seq.filter (fun chunk -> not (chunker.IsChunkTranslated(chunk.Key)))
                |> Seq.toList

            mapChunks 0 chunkMapper 0 0L chunks

        let translateText argMap =
            let argsRecord = {|
                AWSInterface = PatternMatchers.getAWSInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                DefaultTranscript = PatternMatchers.getTranscriptionDefaultTranscript argMap;
                TranslationLanguageCodeSource = PatternMatchers.getTranslationLanguageCodeSource argMap;
                TranslationTargetLanguages = PatternMatchers.getTranslationTargetLanguages argMap;
                TranslationTerminologyNames = PatternMatchers.getTranslationTerminologyNames argMap;
                TaskIdentifier = PatternMatchers.getTaskIdentifier argMap;
                WorkingDirectory = PatternMatchers.getWorkingDirectory argMap;
            |}

            let awsInterface = argsRecord.AWSInterface.Value
            let notifications = argsRecord.Notifications.Value
            let defaultTranscript = argsRecord.DefaultTranscript.Value
            let sourceLanguageCode = argsRecord.TranslationLanguageCodeSource.Value
            let targetLanguageCode = ConsumablePatternMatcher.getLanguageCode (argsRecord.TranslationTargetLanguages.Value)
            let terminologyNames = argsRecord.TranslationTerminologyNames.Value
            let taskId = argsRecord.TaskIdentifier.Value
            let workingDirectory = argsRecord.WorkingDirectory.Value

            if workingDirectory.IsSome && taskId.IsSome then
                let chunker =
                    let archiveFilePath = Translate.getArchivedJobInFolder workingDirectory.Value.FullName taskId.Value   // using taskId as the job name
                    if archiveFilePath.IsSome then
                        SentenceChunker.DeArchiveChunks(archiveFilePath.Value);
                    else
                        //// the text file is assumed to contain a sequence of sentences, with one complete sentence per line
                        //let sentences = File.ReadAllLines(textFilePath)
                        //new TustlerServicesLib.SentenceChunker(sentences)
                        //TustlerServicesLib.SentenceChunker.FromFile(textFilePath)

                        if awsInterface.RuntimeOptions.IsMocked then
                            // set a small chunk size for testing (needs to be longer than the test sentence length for the chunker to work correctly)
                            let chunkSize = 40
                            new TustlerServicesLib.SentenceChunker(defaultTranscript, chunkSize)
                        else
                            new TustlerServicesLib.SentenceChunker(defaultTranscript)

                let translator = Translate.getTranslator awsInterface chunker notifications taskId.Value (sourceLanguageCode.Code) targetLanguageCode.Code terminologyNames
                seq {
                    yield TaskResponse.TaskInfo (sprintf "Running %s translation..." targetLanguageCode.Name)

                    // send TaskResponse messages to show progress
                    yield! processChunks translator chunker
                    if notifications.Notifications.Count > 0 then
                        yield! getNotificationResponse notifications

                    yield (AWSArgument.SetTranslationSegments chunker).toSetArgumentTaskResponse()

                    if chunker.IsJobComplete then
                        yield TaskResponse.TaskComplete ((sprintf "Translation from %s to %s is complete" (sourceLanguageCode.Name) (targetLanguageCode.Name)), DateTime.Now)
                    else
                        yield TaskResponse.TaskComplete ((sprintf "Translation to %s is incomplete; not all segments were translated" targetLanguageCode.Name), DateTime.Now)
                }
            else
                Seq.singleton (TaskResponse.TaskComplete ("Check variables: WorkingDirectory and TaskIdentifier", DateTime.Now))

        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));

            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames));

            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Translate text from a source language to a target language")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationSegments)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! translateText argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let SaveTranslation (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let saveTranslation argMap =
            let argsRecord = {|
                TaskIdentifier = PatternMatchers.getTaskIdentifier argMap;
                WorkingDirectory = PatternMatchers.getWorkingDirectory argMap;
                TranslationTargetLanguages = PatternMatchers.getTranslationTargetLanguages argMap;
                TranslationSegments = PatternMatchers.getTranslationSegments argMap;
            |}

            let taskId = argsRecord.TaskIdentifier.Value
            let workingDirectory = argsRecord.WorkingDirectory.Value
            let targetLanguageCode = ConsumablePatternMatcher.getLanguageCode (argsRecord.TranslationTargetLanguages.Value)
            let chunker = argsRecord.TranslationSegments.Value

            seq {
                if workingDirectory.IsSome && taskId.IsSome then
                    if chunker.IsJobComplete then
                        // save the translated text
                        let fileName = sprintf "Translation-%s-%s.txt" taskId.Value targetLanguageCode.Code
                        let filePath = Path.Combine(workingDirectory.Value.FullName, fileName)
                        File.WriteAllText(filePath, chunker.CompletedTranslation)

                        yield TaskResponse.TaskInfo (sprintf "Working directory is: %s" workingDirectory.Value.FullName)
                        yield TaskResponse.TaskComplete ((sprintf "Saved translation to %s" fileName), DateTime.Now)
                    else
                        chunker.ArchiveChunks(taskId.Value, workingDirectory.Value.FullName)
                        let filePath =
                            let partial = Path.Combine(workingDirectory.Value.FullName, taskId.Value)
                            Path.ChangeExtension(partial, "zip")
                        yield TaskResponse.TaskComplete
                            ((sprintf "Not all chunks were translated due to processing errors. The results so far for language (%s) have been archived at %s. Rerun the translate function to process the archived results."
                                targetLanguageCode.Name filePath), DateTime.Now)
                else
                    yield TaskResponse.TaskComplete ("Check variables: WorkingDirectory and TaskIdentifier", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationSegments));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Save translated text to a file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap [|
                    TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
                    |]

                if unresolvedRequests.Length = 0 then
                    let saveFlags = (PatternMatchers.getSaveFlags argMap).Value

                    if saveFlags.IsSome then
                        if saveFlags.Value.IsSet (AWSFlag(AWSFlagItem.TranslateSaveTranslation)) then
                            let unresolvedRequests = getUnResolvedRequests argMap [|
                                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
                                TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationSegments));
                                TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages));
                                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
                                TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
                                |]

                            if unresolvedRequests.Length = 0 then
                                yield! saveTranslation argMap
                            else
                                yield! resolveByRequest unresolvedRequests
                        else
                            yield TaskResponse.TaskComplete ("Save flag not set (TranslateSaveTranslation)", DateTime.Now)
                    else
                        yield TaskResponse.TaskComplete ("Check variable: SaveFlags", DateTime.Now)
                else
                    yield! resolveByRequest unresolvedRequests
            }

    /// Translate text into multiple languages
    //[<HideFromUI>]
    [<EnableLogging>]
    let MultiLanguageTranslateText (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =
        
        let inputs = [|
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestAWSInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));

            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages));
            TaskResponse.RequestArgument (AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource));

            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveFlags));
        |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Translate text into multiple languages, saving each translation")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.empty
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    // call the translate and save tasks for each of multiple target languages
                    let targetLanguages = (PatternMatchers.getTranslationTargetLanguages argMap).Value
                    let taskSequence = ([|
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "TranslateText", Description = "Translate text into a specified language" );
                        TaskItem(ModuleName = "CloudWeaver.AWS.Tasks", TaskName = "SaveTranslation", Description = "Save some translated text");
                    |])
                    yield TaskResponse.BeginLoopSequence (targetLanguages, taskSequence)

                    // sending task complete initiates the loop
                    yield TaskResponse.TaskComplete ("Translating text into multiple languages...", DateTime.Now)
                else
                    yield! resolveByRequest unresolvedRequests
            }

    /// Convert the task events in a JSON document file to binary log format and save the result
    [<HideFromUI>]
    let ConvertJsonLogToLogFormat (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let convertToLogFormat argMap =
            let argsRecord = {|
                Notifications = PatternMatchers.getNotifications argMap;
                JsonFilePath = PatternMatchers.getOpenJsonFilePath argMap;
                LogFormatFilePath = PatternMatchers.getSaveLogFormatFilePath argMap;
                LogFormatEvents = PatternMatchers.getLogFormatEvents argMap;
            |}

            let notifications = argsRecord.Notifications.Value
            let jsonFilePath = argsRecord.JsonFilePath.Value
            let binaryFilePath = argsRecord.LogFormatFilePath.Value

            let logFormatEvents = argsRecord.LogFormatEvents

            if logFormatEvents.IsNone then
                try
                    let jsonData = File.ReadAllBytes(jsonFilePath.Path)
                    let options = new JsonDocumentOptions(AllowTrailingCommas = true)
                    let document = JsonDocument.Parse(ReadOnlyMemory(jsonData), options)

                    seq {
                        yield TaskResponse.TaskConvertToBinary document
                    }
                with
                | :? JsonException as ex ->
                    seq {
                        notifications.HandleError("ConvertJsonLogToLogFormat", "Document parsing error", ex)
                        yield! getNotificationResponse notifications
                        yield TaskResponse.TaskComplete ("Task completed with errors", DateTime.Now)
                    }
            else
                File.WriteAllBytes(binaryFilePath.Path, logFormatEvents.Value)
                seq {
                    yield TaskResponse.TaskComplete ("Saved event data in binary log format", DateTime.Now)
                }

        let inputs = [|
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveLogFormatFilePath));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestOpenJsonFilePath));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Convert a JSON event log into binary log format")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestLogFormatEvents)))
        | Invoke ->
                seq {
                    let argMap = integrateUIRequestArguments resolvable_arguments
                    let unresolvedRequests = getUnResolvedRequests argMap inputs

                    if unresolvedRequests.Length = 0 then
                        yield! convertToLogFormat argMap
                    else
                        yield! resolveByRequest unresolvedRequests
                }

    /// Convert the task events in a binary log format file to a JSON document and save the result
    [<HideFromUI>]
    let ConvertLogFormatToJsonLog (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let convertToJson argMap =
            let argsRecord = {|
                LogFormatFilePath = PatternMatchers.getOpenLogFormatFilePath argMap;
                JsonFilePath = PatternMatchers.getSaveJsonFilePath argMap;
                JsonEvents = PatternMatchers.getJsonEvents argMap;
            |}

            let logFormatFilePath = argsRecord.LogFormatFilePath.Value
            let jsonFilePath = argsRecord.JsonFilePath.Value

            let jsonEvents = argsRecord.JsonEvents

            if jsonEvents.IsNone then
                let logFormatData = File.ReadAllBytes(logFormatFilePath.Path)

                seq {
                    yield TaskResponse.TaskConvertToJson logFormatData
                }
            else
                File.WriteAllBytes(jsonFilePath.Path, jsonEvents.Value)
                seq {
                    yield TaskResponse.TaskComplete ("Saved event data as JSON", DateTime.Now)
                }

        let inputs = [|
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestSaveJsonFilePath));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestOpenLogFormatFilePath));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Convert a binary log format event log into a JSON event log")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestJsonEvents)))
        | Invoke ->
            seq {
                let argMap = integrateUIRequestArguments resolvable_arguments
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! convertToJson argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    /// Choose a task function to run
    let SelectTask (queryMode: TaskFunctionQueryMode) (resolvable_arguments: InfiniteList<MaybeResponse>) =

        let inputs = [| |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Select a task to run")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestJsonEvents)))
        | Invoke ->
            seq {
                    yield TaskResponse.ChooseTask
            }
