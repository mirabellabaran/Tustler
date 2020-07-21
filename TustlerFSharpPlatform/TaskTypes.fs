namespace TustlerFSharpPlatform

//open TustlerServicesLib
//open TustlerAWSLib
//open TustlerModels
//open System.Collections.Generic

////module public FileServices =
////    /// Use various techniques to detect the mimetype of the data in the specified file path (see also Tustler.Helpers.FileServices)
////    let GetMimeType filePath =

////        let detectors = [|
////            TustlerServicesLib.MimeTypeDictionary.GetMimeTypeFromList
////            TustlerWinPlatformLib.NativeMethods.GetMimeTypeFromFile
////            TustlerWinPlatformLib.RegistryServices.GetMimeTypeFromRegistry
////        |]

////        let chooser (detector:string -> string) =
////            match detector filePath with
////            | null -> None
////            | result -> Some(result)

////        Seq.tryPick chooser detectors

////    //let GetExtension (filePath:string) =

////    let CheckAddExtension (filePath:string) =

////        let mimetype = GetMimeType filePath
////        if mimetype.IsNone then
////            Seq.singleton Poo
////        else
////            let extension =
////                let ext = Path.GetExtension filePath

////                if String.IsNullOrEmpty ext then
////                    match TustlerServicesLib.MimeTypeDictionary.GetExtensionFromMimeType mimetype with
////                    | null -> None
////                    | ext -> Some(ext)
////                else
////                    ext.Substring(1).ToLowerInvariant()

////            if extension.IsNone then

////                // extension cannot be inferred
////                Poo "No file extension was supplied and the extension cannot be inferred. Please specify an extension."
////            else

////                Poo "The inferred mimetype is {mimetype} with extension {extension}. Select Yes to upload the file with this extension, or No to add your own extension."
////                let newpath = Path.ChangeExtension(path, extension)

//    /// A sub task in the overall task sequence (subtasks may be sequentially dependant or independant)
//    type SubTaskItem = {
//        TaskName: string;           // the task function name of the sub-task
//        Description: string;
//    }

//    /// A reference to a media file item stored in an S3 Bucket
//    type S3MediaReference(bucketName: string, key: string, mimeType: string, extension: string) =
//        let mutable _bucketName = bucketName
//        let mutable _key = key
//        let mutable _mimeType = mimeType
//        let mutable _extension = extension

//        member this.BucketName with get() = _bucketName and set(value) = _bucketName <- value
//        member this.Key with get() = _key and set(value) = _key <- value
//        member this.MimeType with get() = _mimeType and set(value) = _mimeType <- value
//        member this.Extension with get() = _extension and set(value) = _extension <- value

//        new() = S3MediaReference(null, null, null, null)

//    /// A reference to a media file stored locally (normally a file to be uploaded to S3)
//    type FileMediaReference(filePath: string, mimeType: string, extension: string) =
//        let mutable _filePath = filePath
//        let mutable _mimeType = mimeType
//        let mutable _extension = extension

//        member this.FilePath with get() = _filePath and set(value) = _filePath <- value
//        member this.MimeType with get() = _mimeType and set(value) = _mimeType <- value
//        member this.Extension with get() = _extension and set(value) = _extension <- value

//        new() = FileMediaReference(null, null, null)

//    /// Tasks and subtasks return a sequence of responses as defined here
//    [<RequireQualifiedAccess>]
//    type TaskResponse =
//        | TaskInfo of string
//        | TaskComplete of string
//        | TaskPrompt of string                  // prompt the user to continue (a single Continue button is displayed along with the prompt message)
//        | TaskSelect of string                  // prompt the user to select an item (this is also a truncation point for subsequent reselection)
//        | TaskMultiSelect of IEnumerable<SubTaskItem>       // user selects zero or more sub-tasks to perform
//        | TaskSequence of IEnumerable<SubTaskItem>          // a sequence of tasks that flow from one to the next without any intervening UI
//        //| TaskDelay of int                                  // delay re-calling the task for the specified number of milliseconds
//        | TaskContinue of int                               // re-invoke the current function after the specified number of milliseconds
//        | TaskArgumentSave                                  // save any arguments set on the event stack for subsequent sessions
    
//        | Notification of Notification
    
//        // Values for UI display only
//        | ShowTranscriptionJobsSummary of TranscriptionJobsViewModel
    
//        // Values that are set on the events stack (as TaskEvent SetArgument)
//        | SetNotificationsList of NotificationsList
//        | SetAWSInterface of AmazonWebServiceInterface
//        | SetTaskItem of SubTaskItem                    // the current subtask function name and description (one of the user-selected items from the MultiSelect list)
//        | SetBucket of Bucket                           // set an argument on the events stack for the selected bucket
//        | SetBucketsModel of BucketViewModel            // set an argument on the events stack for the available buckets
//        | SetBucketItemsModel of BucketItemViewModel    // set an argument on the events stack for the selected bucket item
//        | SetFileUpload of S3MediaReference                // set an argument on the events stack for the file upload details
//        | SetTranscriptionJobName of string             // set an argument on the events stack for the name of the new transcription job
//        | SetTranscriptionJobsModel of TranscriptionJobsViewModel      // set an argument on the events stack for the new transcription job
//        | SetFilePath of string
//        | SetFileMediaReference of FileMediaReference
//        | SetTranscriptionLanguageCode of string
//        | SetTranslationLanguageCode of string
//        | SetVocabularyName of string
    
//        // Values that are sent as requests to the user
//        | RequestBucket
//        | RequestFileMediaReference
//        | RequestS3MediaReference
//        | RequestTranscriptionLanguageCode
//        | RequestTranslationLanguageCode
//        | RequestVocabularyName
    
//    /// The event stack is composed of the following event type
//    [<RequireQualifiedAccess>]
//    type TaskEvent =
//        | InvokingFunction
//        | SetArgument of TaskResponse
//        | ForEach of RetainingStack<SubTaskItem>
//        | SubTask of string     // the name of the sub-task
//        | SelectArgument
//        | ClearArguments
//        | FunctionCompleted

//    /// A simpler option type for use in C# space
//    [<RequireQualifiedAccess>]
//    type MaybeResponse =
//        | Just of TaskResponse
//        | Nothing
//    type MaybeResponse with
//        member x.IsSet = match x with MaybeResponse.Just _ -> true | MaybeResponse.Nothing -> false
//        member x.IsNotSet = match x with MaybeResponse.Nothing -> true | MaybeResponse.Just _ -> false
//        member x.Value = match x with MaybeResponse.Nothing -> invalidArg "MaybeResponse.Value" "Value not set" | MaybeResponse.Just tr -> tr

//    /// The set of all possible argument types (passed to Task functions)
//    type TaskArgumentRecord = {
//        // common arguments required by many Task functions
//        Notifications: NotificationsList option                 // notifications (informational or error messages) generated by function calls
//        AWSInterface: AmazonWebServiceInterface option          // an interface to all defined AWS functions (including mocked versions)

//        // arguments that normally requiring user resolution (via TaskResponse.Request*)
//        S3Bucket: Bucket option
//        S3BucketModel: BucketViewModel option
//        FileMediaReference: FileMediaReference option           // a reference to a media file to be uploaded
//        TranscriptionLanguageCode: string option                // a transcription language code
//        VocabularyName: string option                           // the name of an optional transcription vocabulary

//        // arguments generated in response to proevious Task function calls
//        SubTaskItem: SubTaskItem option
//        S3MediaReference: S3MediaReference option               // a reference to an uploaded media file
//        TranscriptionJobName: string option                     // job name used when starting a new transcription job
//        TranscriptionJobsModel: TranscriptionJobsViewModel option   // the state of all transcription jobs, running or completed
//    }
//    type TaskArgumentRecord with
//        static member Init () =
//            {
//                Notifications = None;
//                AWSInterface = None;
                
//                S3Bucket = None;
//                S3BucketModel = None;
//                FileMediaReference = None;
//                TranscriptionLanguageCode = None;
//                VocabularyName = None;

//                SubTaskItem = None;
//                S3MediaReference = None;
//                TranscriptionJobName = None;
//                TranscriptionJobsModel = None;
//            }
//        member x.AllRequestsSet =  x.S3Bucket.IsSome && x.FileMediaReference.IsSome && x.TranscriptionLanguageCode.IsSome && x.VocabularyName.IsSome
//        member x.InitialArgs = 4
//        member x.ReadyForSubTask index =
//            match index with
//            | 0 -> x.S3Bucket.IsSome && x.FileMediaReference.IsSome
//            | 1 -> x.S3MediaReference.IsSome && x.TranscriptionLanguageCode.IsSome && x.VocabularyName.IsSome
//            | 2 -> x.TranscriptionJobName.IsSome
//            | _ -> invalidArg "index" "Sub task index out of range"
//        member x.Update response =
//            match response with
//            | TaskResponse.SetNotificationsList notifications -> { x with Notifications = Some(notifications) }
//            | TaskResponse.SetAWSInterface awsInterface -> { x with AWSInterface = Some(awsInterface) }

//            | TaskResponse.SetBucket bucket -> { x with S3Bucket = Some(bucket) }
//            | TaskResponse.SetBucketsModel buckets -> { x with S3BucketModel = Some(buckets) }
//            | TaskResponse.SetFileMediaReference media -> { x with FileMediaReference = Some(media) }
//            | TaskResponse.SetTranscriptionLanguageCode code -> { x with TranscriptionLanguageCode = Some(code) }
//            | TaskResponse.SetVocabularyName name -> { x with VocabularyName = Some(name) }

//            | TaskResponse.SetTaskItem subTaskItem -> { x with SubTaskItem = Some(subTaskItem) }
//            | TaskResponse.SetFileUpload media -> { x with S3MediaReference = Some(media) }
//            | TaskResponse.SetTranscriptionJobName transcriptionJobName -> { x with TranscriptionJobName = Some(transcriptionJobName) }
//            | TaskResponse.SetTranscriptionJobsModel transcriptionJobs -> { x with TranscriptionJobsModel = Some(transcriptionJobs) }

//            | _ -> invalidArg "response" "Unexpected type"
    
    
//    //[<RequireQualifiedAccess>]
//    //type TaskArgumentRecord =
//    //    | TranscribeAudio of TranscribeAudioArgs
//    //    | Simple of string
    
//    //type TaskArgumentRecord with
//    //    member x.AllRequestsSet =
//    //        match x with
//    //        | TaskArgumentRecord.TranscribeAudio arg -> arg.AllRequestsSet
//    //        | TaskArgumentRecord.Simple arg -> arg.Length > 0
//    //    member x.ReadyForSubTask index =
//    //        match x with
//    //        | TaskArgumentRecord.TranscribeAudio arg -> arg.ReadyForSubTask index
//    //        | TaskArgumentRecord.Simple arg -> arg.Length > 0
//    //    member x.InitialArgs =
//    //        match x with
//    //        | TaskArgumentRecord.TranscribeAudio arg -> arg.InitialArgs
//    //        | TaskArgumentRecord.Simple arg -> arg.Length
//    //    member x.Update response =
//    //        match x with
//    //        | TaskArgumentRecord.TranscribeAudio arg -> TaskArgumentRecord.TranscribeAudio (arg.Update response)
//    //        | TaskArgumentRecord.Simple arg -> TaskArgumentRecord.Simple (arg)
    
//    //type UIGridReference = {
//    //    RowIndex: int
//    //    ColumnIndex: int
//    //    RowSpan: int
//    //    ColumnSpan: int
//    //    Tag: string
//    //}
    
//    //type RequiredMembersOption(rows, columns, members: UIGridReference[]) =

//    //    let mutable rows = rows
//    //    let mutable columns = columns
//    //    let mutable members = members

//    //    new() = RequiredMembersOption(0, 0, [||])

//    //    member this.Rows with get() = rows
//    //    member this.Columns with get() = columns
//    //    member this.Members with get() = members
//    //    member this.IsRequired with get () = members.Length > 0

//    //type TaskArgumentMember =
//    //    | TaskName of string
//    //    | MediaRef of S3MediaReference
//    //    | FilePath of string
//    //    | TranscriptionLanguageCode of string
//    //    | TranslationLanguageCode of string
//    //    | VocabularyName of string
//    //    | Poop of string

//    //type ITaskArgumentCollection =

//    //    // get the names of the values required to populate the TaskArgument e.g. taskName, languageCode, or mediaReference (-> tag names of user controls)
//    //    abstract member GetRequiredMembers : unit -> RequiredMembersOption
        
//    //    // set the value of a TaskArgument member
//    //    abstract member SetValue : taskMember:TaskArgumentMember -> unit

//    //    // true when all required members are no longer set to None
//    //    abstract member IsComplete : unit -> bool

//    //type NotificationsOnlyArguments(awsInterface: AmazonWebServiceInterface, notifications: NotificationsList) =

//    //    member val AWSInterface = awsInterface with get
//    //    member val Notifications = notifications with get

//    //    interface ITaskArgumentCollection with
//    //        member this.GetRequiredMembers () = RequiredMembersOption()

//    //        member this.SetValue taskMember = ()

//    //        member this.IsComplete () = true

//    //type TranscribeAudioArguments(awsInterface: AmazonWebServiceInterface, notifications: NotificationsList) =
       
//    //    let mutable taskName = None
//    //    let mutable mediaRef = None
//    //    let mutable filePath = None
//    //    let mutable transcriptionLanguageCode = None
//    //    let mutable vocabularyName = None

//    //    member val AWSInterface = awsInterface with get
//    //    member val Notifications = notifications with get
        
//    //    member this.TaskName with get () = taskName.Value
//    //    member this.MediaRef with get () = mediaRef.Value
//    //    member this.FilePath with get () = filePath.Value
//    //    member this.TranscriptionLanguageCode with get () = transcriptionLanguageCode.Value
//    //    member this.VocabularyName with get () = vocabularyName.Value

//    //    interface ITaskArgumentCollection with

//    //        member this.GetRequiredMembers () =
//    //            RequiredMembersOption(3, 2,
//    //                [|
//    //                    { RowIndex = 0; ColumnIndex = 0; RowSpan = 1; ColumnSpan = 1; Tag = "taskName" };
//    //                    { RowIndex = 0; ColumnIndex = 1; RowSpan = 1; ColumnSpan = 1; Tag = "filePath" };
//    //                    { RowIndex = 1; ColumnIndex = 0; RowSpan = 1; ColumnSpan = 1; Tag = "transcriptionLanguageCode" };
//    //                    { RowIndex = 1; ColumnIndex = 1; RowSpan = 1; ColumnSpan = 1; Tag = "vocabularyName" };
//    //                    { RowIndex = 2; ColumnIndex = 0; RowSpan = 1; ColumnSpan = 2; Tag = "mediaRef" };
//    //                |] )

//    //        member this.SetValue taskMember =
//    //            match taskMember with
//    //            | TaskName myTaskName -> taskName <- Some(myTaskName)
//    //            | MediaRef myMediaRef -> mediaRef <- Some(myMediaRef)
//    //            | FilePath myFilePath -> filePath <- Some(myFilePath)
//    //            | TranscriptionLanguageCode myLanguageCode -> transcriptionLanguageCode <- Some(myLanguageCode)
//    //            | VocabularyName myVocabularyName -> vocabularyName <- Some(myVocabularyName)
//    //            | _ -> ()

//    //        member this.IsComplete () =
//    //            taskName.IsSome && mediaRef.IsSome && filePath.IsSome && transcriptionLanguageCode.IsSome && vocabularyName.IsSome