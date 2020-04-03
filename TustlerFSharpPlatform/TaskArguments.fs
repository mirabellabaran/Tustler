namespace TustlerFSharpPlatform

open System.Threading.Tasks
open TustlerServicesLib
open AWSInterface
open TustlerModels
open System.Collections.ObjectModel

module public TaskArguments =

    type MediaReference(bucketName: string, key: string, mimeType: string, extension: string) =

        member val BucketName = bucketName
        member val Key = key
        member val MimeType = mimeType
        member val Extension = extension

    type TaskArgumentMember =
        | TaskName of string
        | MediaRef of MediaReference
        | FilePath of string
        | LanguageCode of string
        | VocabularyName of string

    type ITaskArgument =

        // get the names of the values required to populate the TaskArgument e.g. taskName, languageCode, or mediaReference (-> tag names of user controls)
        abstract member GetRequiredMembers : unit -> seq<string>
        
        // set the value of a TaskArgument member
        abstract member SetValue : taskMember:TaskArgumentMember -> unit

        // true when all required members are no longer set to None
        abstract member IsComplete : unit -> bool

    type TranscribeAudioArguments() =
       
        let mutable taskName = None
        let mutable mediaRef = None
        let mutable filePath = None
        let mutable languageCode = None
        let mutable vocabularyName = None

        member this.TaskName with get () = taskName.Value
        member this.MediaRef with get () = mediaRef.Value
        member this.FilePath with get () = filePath.Value
        member this.LanguageCode with get () = languageCode.Value
        member this.VocabularyName with get () = vocabularyName.Value

        interface ITaskArgument with

            member this.GetRequiredMembers () =
                seq { "taskName"; "mediaRef"; "filePath"; "languageCode"; "vocabularyName" }

            member this.SetValue taskMember =
                match taskMember with
                | TaskName myTaskName -> taskName <- Some(myTaskName)
                | MediaRef myMediaRef -> mediaRef <- Some(myMediaRef)
                | FilePath myFilePath -> filePath <- Some(myFilePath)
                | LanguageCode myLanguageCode -> languageCode <- Some(myLanguageCode)
                | VocabularyName myVocabularyName -> vocabularyName <- Some(myVocabularyName)

            member this.IsComplete () =
                taskName.IsSome && mediaRef.IsSome && filePath.IsSome && languageCode.IsSome && vocabularyName.IsSome