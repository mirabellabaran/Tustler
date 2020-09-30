namespace TustlerFSharpPlatform

open TustlerModels
open System.Collections.Generic
open CloudWeaver.Types
open CloudWeaver.AWS
open System.IO

[<RequireQualifiedAccess>]
type FilePickerMode =
    | Open
    | Save

[<RequireQualifiedAccess>]
type UITaskMode =
    | Unknown
    | SelectTask
    | RestartTask           // restart a completed task
    | SetArgument           // set an argument on the agent
    | Continue
    | ForEachIndependantTask

[<RequireQualifiedAccess>]
type UITaskArgument =
    | SelectedTask of TaskItem
    | Bucket of Bucket
    | ForEach of IEnumerable<TaskItem>
    | S3MediaReference of S3MediaReference
    | FileMediaReference of FileMediaReference          // for media files
    | FilePath of FileInfo * string * FilePickerMode    // all other file types (the second argument is the required file extension used to determine the SetArgument type)
    | TranscriptionLanguageCode of string
    | TranscriptionVocabularyName of string
    | TranscriptionDefaultTranscript of string
    | TranslationLanguageCodeSource of string
    | TranslationTargetLanguages of IEnumerable<LanguageCode>
    | TranslationTerminologyNames of IEnumerable<string>

/// Collects arguments used by user control command source objects
type UITaskArguments () =
    let mutable mode = UITaskMode.Unknown
    let mutable arguments: UITaskArgument[] = Array.empty

    member this.Mode with get () = mode and set _mode = mode <- _mode
    member this.TaskArguments
        with get() = Seq.ofArray arguments
        and set args = arguments <- Seq.toArray args

