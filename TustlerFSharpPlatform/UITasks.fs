namespace TustlerFSharpPlatform


//[<RequireQualifiedAccess>]
//type UITaskArgument =
//    | SelectedTask of byte[]
//    | Bucket of byte[]
//    | ForEach of IEnumerable<TaskItem>
//    | S3MediaReference of S3MediaReference
//    | FileMediaReference of byte[]  //FileMediaReference          // for media files
//    | FilePath of byte[]    // FileInfo * string * FilePickerMode    // all other file types (the second argument is the required file extension used to determine the SetArgument type)
//    | TranscriptionLanguageCode of byte[]
//    | TranscriptionVocabularyName of string
//    | TranscriptionDefaultTranscript of string
//    | TranslationLanguageCodeSource of byte[]
//    | TranslationTargetLanguages of byte[]  //IEnumerable<LanguageCode>
//    | TranslationTerminologyNames of byte[] //IEnumerable<string>

/// Collects arguments used by user control command source objects
type UITaskArguments (mode: UITaskMode, moduleName: string, propertyName: string, argument: byte[]) =

    member this.Mode with get () = mode
    member this.ModuleName with get() = moduleName
    member this.PropertyName with get() = propertyName
    member this.TaskArgument with get() = argument

