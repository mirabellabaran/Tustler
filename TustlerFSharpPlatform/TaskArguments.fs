namespace TustlerFSharpPlatform

open System.Threading.Tasks
open TustlerServicesLib
open AWSInterface
open TustlerModels
open System.Collections.ObjectModel

module public TaskArguments =

    type RequiredMembersOption(members: string[]) =

        let mutable members = members

        new() = RequiredMembersOption([||])
        member this.Members with get() = members
        member this.IsRequired with get () = members.Length > 0

    type MediaReference(bucketName: string, key: string, mimeType: string, extension: string) =

        member val BucketName = bucketName
        member val Key = key
        member val MimeType = mimeType
        member val Extension = extension

    type TaskArgumentMember =
        | TaskName of string
        | MediaRef of MediaReference
        | FilePath of string
        | TranscriptionLanguageCode of string
        | VocabularyName of string
        | Poop of string

    type ITaskArgumentCollection =

        // get the names of the values required to populate the TaskArgument e.g. taskName, languageCode, or mediaReference (-> tag names of user controls)
        abstract member GetRequiredMembers : unit -> RequiredMembersOption
        
        // set the value of a TaskArgument member
        abstract member SetValue : taskMember:TaskArgumentMember -> unit

        // true when all required members are no longer set to None
        abstract member IsComplete : unit -> bool

    type NotificationsOnlyArguments(notifications: NotificationsList) =

        member val Notifications = notifications with get

        interface ITaskArgumentCollection with
            member this.GetRequiredMembers () = RequiredMembersOption()

            member this.SetValue taskMember = ()

            member this.IsComplete () = true

    type TranscribeAudioArguments(notifications: NotificationsList) =
       
        let mutable taskName = None
        let mutable mediaRef = None
        let mutable filePath = None
        let mutable transcriptionLanguageCode = None
        let mutable vocabularyName = None

        member val Notifications = notifications with get
        
        member this.TaskName with get () = taskName.Value
        member this.MediaRef with get () = mediaRef.Value
        member this.FilePath with get () = filePath.Value
        member this.TranscriptionLanguageCode with get () = transcriptionLanguageCode.Value
        member this.VocabularyName with get () = vocabularyName.Value

        interface ITaskArgumentCollection with

            member this.GetRequiredMembers () =
                RequiredMembersOption( [| "taskName"; "mediaRef"; "filePath"; "transcriptionLanguageCode"; "vocabularyName" |] )

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