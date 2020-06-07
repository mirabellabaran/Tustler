namespace TustlerFSharpPlatform

open TustlerServicesLib
open TustlerInterfaces
open TustlerAWSLib
open System
open System.IO

//module public FileServices =
//    /// Use various techniques to detect the mimetype of the data in the specified file path (see also Tustler.Helpers.FileServices)
//    let GetMimeType filePath =

//        let detectors = [|
//            TustlerServicesLib.MimeTypeDictionary.GetMimeTypeFromList
//            TustlerWinPlatformLib.NativeMethods.GetMimeTypeFromFile
//            TustlerWinPlatformLib.RegistryServices.GetMimeTypeFromRegistry
//        |]

//        let chooser (detector:string -> string) =
//            match detector filePath with
//            | null -> None
//            | result -> Some(result)

//        Seq.tryPick chooser detectors

//    //let GetExtension (filePath:string) =

//    let CheckAddExtension (filePath:string) =

//        let mimetype = GetMimeType filePath
//        if mimetype.IsNone then
//            Seq.singleton Poo
//        else
//            let extension =
//                let ext = Path.GetExtension filePath

//                if String.IsNullOrEmpty ext then
//                    match TustlerServicesLib.MimeTypeDictionary.GetExtensionFromMimeType mimetype with
//                    | null -> None
//                    | ext -> Some(ext)
//                else
//                    ext.Substring(1).ToLowerInvariant()

//            if extension.IsNone then

//                // extension cannot be inferred
//                Poo "No file extension was supplied and the extension cannot be inferred. Please specify an extension."
//            else

//                Poo "The inferred mimetype is {mimetype} with extension {extension}. Select Yes to upload the file with this extension, or No to add your own extension."
//                let newpath = Path.ChangeExtension(path, extension)

module public TaskArguments =

    type UIGridReference = {
        RowIndex: int
        ColumnIndex: int
        RowSpan: int
        ColumnSpan: int
        Tag: string
    }
    
    type RequiredMembersOption(rows, columns, members: UIGridReference[]) =

        let mutable rows = rows
        let mutable columns = columns
        let mutable members = members

        new() = RequiredMembersOption(0, 0, [||])

        member this.Rows with get() = rows
        member this.Columns with get() = columns
        member this.Members with get() = members
        member this.IsRequired with get () = members.Length > 0

    /// A reference to a media file item stored in an S3 Bucket
    type S3MediaReference(bucketName: string, key: string, mimeType: string, extension: string) =
        member val BucketName = bucketName
        member val Key = key
        member val MimeType = mimeType
        member val Extension = extension

    /// A reference to a media file stored locally (normally a file to be uploaded to S3)
    type FileMediaReference(filePath: string, mimeType: string, extension: string) =
        member val FilePath = filePath
        member val MimeType = mimeType
        member val Extension = extension

    type TaskArgumentMember =
        | TaskName of string
        | MediaRef of S3MediaReference
        | FilePath of string
        | TranscriptionLanguageCode of string
        | TranslationLanguageCode of string
        | VocabularyName of string
        | Poop of string

    type ITaskArgumentCollection =

        // get the names of the values required to populate the TaskArgument e.g. taskName, languageCode, or mediaReference (-> tag names of user controls)
        abstract member GetRequiredMembers : unit -> RequiredMembersOption
        
        // set the value of a TaskArgument member
        abstract member SetValue : taskMember:TaskArgumentMember -> unit

        // true when all required members are no longer set to None
        abstract member IsComplete : unit -> bool

    type NotificationsOnlyArguments(awsInterface: AmazonWebServiceInterface, notifications: NotificationsList) =

        member val AWSInterface = awsInterface with get
        member val Notifications = notifications with get

        interface ITaskArgumentCollection with
            member this.GetRequiredMembers () = RequiredMembersOption()

            member this.SetValue taskMember = ()

            member this.IsComplete () = true

    type TranscribeAudioArguments(awsInterface: AmazonWebServiceInterface, notifications: NotificationsList) =
       
        let mutable taskName = None
        let mutable mediaRef = None
        let mutable filePath = None
        let mutable transcriptionLanguageCode = None
        let mutable vocabularyName = None

        member val AWSInterface = awsInterface with get
        member val Notifications = notifications with get
        
        member this.TaskName with get () = taskName.Value
        member this.MediaRef with get () = mediaRef.Value
        member this.FilePath with get () = filePath.Value
        member this.TranscriptionLanguageCode with get () = transcriptionLanguageCode.Value
        member this.VocabularyName with get () = vocabularyName.Value

        interface ITaskArgumentCollection with

            member this.GetRequiredMembers () =
                RequiredMembersOption(3, 2,
                    [|
                        { RowIndex = 0; ColumnIndex = 0; RowSpan = 1; ColumnSpan = 1; Tag = "taskName" };
                        { RowIndex = 0; ColumnIndex = 1; RowSpan = 1; ColumnSpan = 1; Tag = "filePath" };
                        { RowIndex = 1; ColumnIndex = 0; RowSpan = 1; ColumnSpan = 1; Tag = "transcriptionLanguageCode" };
                        { RowIndex = 1; ColumnIndex = 1; RowSpan = 1; ColumnSpan = 1; Tag = "vocabularyName" };
                        { RowIndex = 2; ColumnIndex = 0; RowSpan = 1; ColumnSpan = 2; Tag = "mediaRef" };
                    |] )

            member this.SetValue taskMember =
                match taskMember with
                | TaskName myTaskName -> taskName <- Some(myTaskName)
                | MediaRef myMediaRef -> mediaRef <- Some(myMediaRef)
                | FilePath myFilePath -> filePath <- Some(myFilePath)
                | TranscriptionLanguageCode myLanguageCode -> transcriptionLanguageCode <- Some(myLanguageCode)
                | VocabularyName myVocabularyName -> vocabularyName <- Some(myVocabularyName)
                | _ -> ()

            member this.IsComplete () =
                taskName.IsSome && mediaRef.IsSome && filePath.IsSome && transcriptionLanguageCode.IsSome && vocabularyName.IsSome