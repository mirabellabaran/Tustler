namespace TustlerFSharpPlatform

open TustlerModels
open System.Collections.Generic
open CloudWeaver.Types
open CloudWeaver.AWS

[<RequireQualifiedAccess>]
type UITaskMode =
    | Unknown
    | Select
    | Continue
    | ForEachIndependantTask

[<RequireQualifiedAccess>]
type UITaskArgument =
    | Bucket of Bucket
    | FilePath of string
    | ForEach of IEnumerable<TaskItem>
    | S3MediaReference of S3MediaReference
    | FileMediaReference of FileMediaReference
    | TranscriptionLanguageCode of string
    | VocabularyName of string
    | TranslationLanguageCodeSource of string
    | TranslationTargetLanguages of IEnumerable<LanguageCode>

/// Collects arguments used by user control command source objects
type UITaskArguments () =
    let mutable mode = UITaskMode.Unknown
    let mutable arguments: UITaskArgument[] = Array.empty

    member this.Mode with get () = mode and set _mode = mode <- _mode
    member this.TaskArguments
        with get() = Seq.ofArray arguments
        and set args = arguments <- Seq.toArray args

