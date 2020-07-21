namespace CloudWeaver.Types

open TustlerServicesLib
open TustlerAWSLib
open TustlerModels
open System.Collections.Generic

    // an attribute to tell the UI not to show certain task functions (those that are called as sub-tasks)
    type HideFromUI() = inherit System.Attribute()

    /// A sub task in the overall task sequence (subtasks may be sequentially dependant or independant)
    type SubTaskItem = {
        TaskName: string;           // the task function name of the sub-task
        Description: string;
    }

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

    /// Tasks and subtasks return a sequence of responses as defined here
    [<RequireQualifiedAccess>]
    type TaskResponse =
        | TaskInfo of string
        | TaskComplete of string
        | TaskPrompt of string                  // prompt the user to continue (a single Continue button is displayed along with the prompt message)
        | TaskSelect of string                  // prompt the user to select an item (this is also a truncation point for subsequent reselection)
        | TaskMultiSelect of IEnumerable<SubTaskItem>       // user selects zero or more sub-tasks to perform
        | TaskSequence of IEnumerable<SubTaskItem>          // a sequence of tasks that flow from one to the next without any intervening UI
        //| TaskDelay of int                                  // delay re-calling the task for the specified number of milliseconds
        | TaskContinue of int                               // re-invoke the current function after the specified number of milliseconds
        | TaskArgumentSave                                  // save any arguments set on the event stack for subsequent sessions
    
        | Notification of Notification
    
        // Values for UI display only
        | ShowTranscriptionJobsSummary of TranscriptionJobsViewModel
    
        // Values that are set on the events stack (as TaskEvent SetArgument)
        | SetNotificationsList of NotificationsList
        | SetAWSInterface of AmazonWebServiceInterface
        | SetTaskItem of SubTaskItem                    // the current subtask function name and description (one of the user-selected items from the MultiSelect list)
        | SetBucket of Bucket                           // set an argument on the events stack for the selected bucket
        | SetBucketsModel of BucketViewModel            // set an argument on the events stack for the available buckets
        | SetBucketItemsModel of BucketItemViewModel    // set an argument on the events stack for the selected bucket item
        | SetFileUpload of S3MediaReference                // set an argument on the events stack for the file upload details
        | SetTranscriptionJobName of string             // set an argument on the events stack for the name of the new transcription job
        | SetTranscriptionJobsModel of TranscriptionJobsViewModel      // set an argument on the events stack for the new transcription job
        | SetFilePath of string
        | SetFileMediaReference of FileMediaReference
        | SetTranscriptionLanguageCode of string
        | SetTranslationLanguageCode of string
        | SetVocabularyName of string
    
        // Values that are sent as requests to the user
        | RequestBucket
        | RequestFileMediaReference
        | RequestS3MediaReference
        | RequestTranscriptionLanguageCode
        | RequestTranslationLanguageCode
        | RequestVocabularyName
    
    /// The event stack is composed of the following event type
    [<RequireQualifiedAccess>]
    type TaskEvent =
        | InvokingFunction
        | SetArgument of TaskResponse
        | ForEach of RetainingStack<SubTaskItem>
        | SubTask of string     // the name of the sub-task
        | SelectArgument
        | ClearArguments
        | FunctionCompleted

    /// A simpler option type for use in C# space
    [<RequireQualifiedAccess>]
    type MaybeResponse =
        | Just of TaskResponse
        | Nothing
    type MaybeResponse with
        member x.IsSet = match x with MaybeResponse.Just _ -> true | MaybeResponse.Nothing -> false
        member x.IsNotSet = match x with MaybeResponse.Nothing -> true | MaybeResponse.Just _ -> false
        member x.Value = match x with MaybeResponse.Nothing -> invalidArg "MaybeResponse.Value" "Value not set" | MaybeResponse.Just tr -> tr

    /// The set of all possible argument types (passed to Task functions)
    type TaskArgumentRecord = {
        // common arguments required by many Task functions
        Notifications: NotificationsList option                 // notifications (informational or error messages) generated by function calls
        AWSInterface: AmazonWebServiceInterface option          // an interface to all defined AWS functions (including mocked versions)

        // arguments that normally requiring user resolution (via TaskResponse.Request*)
        S3Bucket: Bucket option
        S3BucketModel: BucketViewModel option
        FileMediaReference: FileMediaReference option           // a reference to a media file to be uploaded
        TranscriptionLanguageCode: string option                // a transcription language code
        VocabularyName: string option                           // the name of an optional transcription vocabulary

        // arguments generated in response to proevious Task function calls
        SubTaskItem: SubTaskItem option
        S3MediaReference: S3MediaReference option               // a reference to an uploaded media file
        TranscriptionJobName: string option                     // job name used when starting a new transcription job
        TranscriptionJobsModel: TranscriptionJobsViewModel option   // the state of all transcription jobs, running or completed
    }
    type TaskArgumentRecord with
        static member Init () =
            {
                Notifications = None;
                AWSInterface = None;
                
                S3Bucket = None;
                S3BucketModel = None;
                FileMediaReference = None;
                TranscriptionLanguageCode = None;
                VocabularyName = None;

                SubTaskItem = None;
                S3MediaReference = None;
                TranscriptionJobName = None;
                TranscriptionJobsModel = None;
            }
        member x.AllRequestsSet =  x.S3Bucket.IsSome && x.FileMediaReference.IsSome && x.TranscriptionLanguageCode.IsSome && x.VocabularyName.IsSome
        member x.InitialArgs = 4
        member x.ReadyForSubTask index =
            match index with
            | 0 -> x.S3Bucket.IsSome && x.FileMediaReference.IsSome
            | 1 -> x.S3MediaReference.IsSome && x.TranscriptionLanguageCode.IsSome && x.VocabularyName.IsSome
            | 2 -> x.TranscriptionJobName.IsSome
            | _ -> invalidArg "index" "Sub task index out of range"
        member x.Update response =
            match response with
            | TaskResponse.SetNotificationsList notifications -> { x with Notifications = Some(notifications) }
            | TaskResponse.SetAWSInterface awsInterface -> { x with AWSInterface = Some(awsInterface) }

            | TaskResponse.SetBucket bucket -> { x with S3Bucket = Some(bucket) }
            | TaskResponse.SetBucketsModel buckets -> { x with S3BucketModel = Some(buckets) }
            | TaskResponse.SetFileMediaReference media -> { x with FileMediaReference = Some(media) }
            | TaskResponse.SetTranscriptionLanguageCode code -> { x with TranscriptionLanguageCode = Some(code) }
            | TaskResponse.SetVocabularyName name -> { x with VocabularyName = Some(name) }

            | TaskResponse.SetTaskItem subTaskItem -> { x with SubTaskItem = Some(subTaskItem) }
            | TaskResponse.SetFileUpload media -> { x with S3MediaReference = Some(media) }
            | TaskResponse.SetTranscriptionJobName transcriptionJobName -> { x with TranscriptionJobName = Some(transcriptionJobName) }
            | TaskResponse.SetTranscriptionJobsModel transcriptionJobs -> { x with TranscriptionJobsModel = Some(transcriptionJobs) }

            | _ -> invalidArg "response" "Unexpected type"
