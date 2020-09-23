﻿namespace CloudWeaver.AWS

open System
open TustlerServicesLib
open TustlerModels
open System.Collections.Generic
open CloudWeaver.Types

open TustlerAWSLib
open System.Text.Json
open System.IO
open Microsoft.FSharp.Reflection

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
    with
    override this.ToString() =
        match this with
        | DisplayBucketItemsModel bucketItemViewModel -> (sprintf "DisplayBucketItemsModel: %s" (bucketItemViewModel.ToString()))
        | DisplayTranscriptionJob transcriptionJob -> (sprintf "DisplayTranscriptionJob: %s" (transcriptionJob.ToString()))
        | DisplayTranscriptionJobsModel transcriptionJobsViewModel -> (sprintf "DisplayTranscriptionJobsModel: %s" (transcriptionJobsViewModel.ToString()))

/// Wrapper for the display values used by this module
and AWSShowIntraModule(arg: AWSDisplayValue) =
    interface IShowValue with
        member this.Identifier with get() = Identifier (CommonUtilities.toString arg)
        member this.ToString () = sprintf "AWSShowIntraModule(%s)" (arg.ToString())

    member this.Argument with get() = arg

/// Used for iterable values (see TaskEvent.ForEach...)
type AWSIterationArgument =
    | LanguageCode of LanguageCode
    | Test of string

type AWSShareIterationArgument(arg: AWSIterationArgument) =
    interface IShareIterationArgument with
        member this.ToString () =
            match arg with
            | LanguageCode languageCode -> sprintf "AWSShareIterationArgument(LanguageCode: %s)" (languageCode.Name)
            | Test str -> sprintf "AWSShareIterationArgument(Test: %s)" str
        member this.Serialize writer =
            writer.WriteStartObject()
            match arg with
            | LanguageCode languageCode -> writer.WritePropertyName("LanguageCode"); JsonSerializer.Serialize(writer, languageCode)
            | Test str -> writer.WritePropertyName("Test"); JsonSerializer.Serialize(writer, str)
            writer.WriteEndObject()

    member this.UnWrap with get() = arg

    static member Deserialize (wrappedObject: JsonElement) =
        let property = wrappedObject.EnumerateObject() |> Seq.exactlyOne

        let iterationArgument =
            match property.Name with
            | "LanguageCode" ->
                let data = JsonSerializer.Deserialize<LanguageCode>(property.Value.GetRawText())
                AWSIterationArgument.LanguageCode data
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" property.Name)

        AWSShareIterationArgument(iterationArgument) :> IShareIterationArgument

/// An iteration argument stack (IConsumable) for AWSIterationArgument types
type AWSIterationStack(items: IEnumerable<IShareIterationArgument>) =
    inherit RetainingStack(items)

    override this.ModuleName with get() = "AWSShareIterationArgument"


//module public AWSUnWrap =

//    let unWrapLanguageCode arg =
//        match arg with
//        | LanguageCode languageCode -> languageCode
//        | _ -> invalidArg "arg" "Expecting a languageCode"

//    let unWrap (LanguageCode arg) = arg
//    let unWrap (Poop arg) = arg

//    let (|LanguageCode|) = function
//        | LanguageCode code -> code
//        | Poop poo -> poo

type AWSIterationWrapper(consumable: IConsumable) =

    let getWrapperFrom (consumable: IConsumable) = 
        match consumable with
        | :? RetainingStack as stack ->
            match stack.Current with
            | :? AWSShareIterationArgument as arg -> arg
            | _ -> invalidArg "consumable" "Unknown item type for RetainingStack; expecting an AWSShareIterationArgument item"
        | _ -> invalidArg "consumable" "Unknown IConsumable type; expecting RetainingStack"

    let (| APLanguageCode |) (argWrapper: AWSShareIterationArgument) =
        match argWrapper.UnWrap with
        | LanguageCode languageCode -> languageCode
        | _ -> invalidArg "arg" "Expecting a languageCode"

    let (| APPoop |) (argWrapper: AWSShareIterationArgument) =
        match argWrapper.UnWrap with
        | Test str -> Some(str)
        | _ -> None

    member this.UnWrap with get() = consumable

    member this.LanguageCode with get() = //(APLanguageCode (getWrapperFrom consumable))
        match (getWrapperFrom consumable) with
        | APLanguageCode languageCode -> languageCode

    member this.Poop with get() =
        match (getWrapperFrom consumable) with
        | APPoop str -> str
// TODO build a dict from args keyed by TaskResponse.Request* and use active pattern functions to retrieve strongly typed values in task functions


/// Arguments used by this module.
/// These values are set on the events stack (as TaskEvent SetArgument).
type AWSArgument =
    | SetAWSInterface of AmazonWebServiceInterface
    | SetBucket of Bucket                           // set an argument on the events stack for the selected bucket
    | SetBucketsModel of BucketViewModel            // set an argument on the events stack for the available buckets
    | SetS3MediaReference of S3MediaReference                       // Amazon S3 file reference to the uploaded file
    | SetTranscriptionJobName of string                             // the name of the new transcription job
    | SetTranscriptURI of string                                    // the URI (S3 bucket/key) of the transcript from a completed transcription job
    | SetTranscriptJSON of ReadOnlyMemory<byte>                     // the JSON transcript generated by the Transctibe service
    | SetTranscriptionDefaultTranscript of string                   // the default transcript extracted from the JSON transcript file
    | SetTranscriptionJobsModel of TranscriptionJobsViewModel       // the model that wraps transcription jobs
    | SetFileMediaReference of FileMediaReference
    | SetTranscriptionLanguageCode of string
    | SetTranscriptionVocabularyName of string
    | SetTranslationLanguageCodeSource of string
    | SetTranslationTargetLanguages of IConsumable                  /// Used for iterable values (see TaskEvent.ForEach...)
    | SetTranslationTerminologyNames of List<string>
    | SetTranslationSegments of SentenceChunker
    | SetSubtitleFilePath of FileInfo
    with
    member this.toSetArgumentTaskResponse() = TaskResponse.SetArgument (AWSShareIntraModule(this))
    member this.toTaskEvent() = TaskEvent.SetArgument(this.toSetArgumentTaskResponse());
    override this.ToString() =
        match this with
        | SetAWSInterface amazonWebServiceInterface -> (sprintf "SetAWSInterface: %s" (amazonWebServiceInterface.ToString()))
        | SetBucket bucket -> (sprintf "SetBucket: %s" (bucket.ToString()))
        | SetBucketsModel bucketViewModel -> (sprintf "SetBucketsModel: %s" (bucketViewModel.ToString()))
        | SetS3MediaReference s3MediaReference -> (sprintf "SetS3MediaReference: %s" (s3MediaReference.ToString()))
        | SetTranscriptionJobName transcriptionJobName -> (sprintf "SetTranscriptionJobName: %s" transcriptionJobName)
        | SetTranscriptURI transcriptURI -> (sprintf "SetTranscriptURI: %s" transcriptURI)
        | SetTranscriptJSON transcriptJSON -> (sprintf "SetTranscriptJSON: %s" ((transcriptJSON :> Object).ToString()))
        | SetTranscriptionDefaultTranscript defaultTranscript -> (sprintf "SetTranscriptionDefaultTranscript: %s..." (defaultTranscript.Substring(0, 30)))
        | SetTranscriptionJobsModel transcriptionJobsViewModel -> (sprintf "SetTranscriptionJobsModel: %s" (transcriptionJobsViewModel.ToString()))
        | SetFileMediaReference fileMediaReference -> (sprintf "SetFileMediaReference: %s" (fileMediaReference.ToString()))
        | SetTranscriptionLanguageCode languageCode -> (sprintf "SetTranscriptionLanguageCode: %s" languageCode)
        | SetTranscriptionVocabularyName vocabularyName -> (sprintf "SetTranscriptionVocabularyName: %s" vocabularyName)
        | SetTranslationLanguageCodeSource languageCode -> (sprintf "SetTranslationLanguageCodeSource: %s" languageCode)
        | SetTranslationTargetLanguages languages -> (sprintf "SetTranslationTargetLanguages: %s" (System.String.Join(", ", (Seq.map (fun (lang) -> lang.ToString) languages))))
        | SetTranslationTerminologyNames terminologyNames -> (sprintf "SetTranslationTerminologyNames: %s" (System.String.Join(", ", terminologyNames)))
        | SetTranslationSegments chunker -> (sprintf "SetTranslationSegments: %s" (chunker.ToString()))
        | SetSubtitleFilePath fileInfo -> (sprintf "SetSubtitleFilePath: %s" fileInfo.FullName)


/// Wrapper for the arguments used by this module
and AWSShareIntraModule(arg: AWSArgument) =
    interface IShareIntraModule with
        member this.ModuleTag with get() = Tag "AWSShareIntraModule"
        member this.Identifier with get() = Identifier (CommonUtilities.toString arg)
        member this.ToString () = sprintf "AWSShareIntraModule(%s)" (arg.ToString())
        member this.AsBytes () =
            JsonSerializer.SerializeToUtf8Bytes(arg)
        member this.Serialize writer serializerOptions =
            match arg with
            | SetAWSInterface awsInterface -> writer.WritePropertyName("SetAWSInterface"); JsonSerializer.Serialize<AmazonWebServiceInterface>(writer, awsInterface)
            | SetBucket bucket -> writer.WritePropertyName("SetBucket"); JsonSerializer.Serialize<Bucket>(writer, bucket)
            | SetBucketsModel bucketViewModel -> writer.WritePropertyName("SetBucketsModel"); JsonSerializer.Serialize<BucketViewModel>(writer, bucketViewModel)
            | SetS3MediaReference s3MediaReference -> writer.WritePropertyName("SetFileUpload"); JsonSerializer.Serialize<S3MediaReference>(writer, s3MediaReference)
            | SetTranscriptionJobName jobName -> writer.WritePropertyName("SetTranscriptionJobName"); JsonSerializer.Serialize<string>(writer, jobName)
            | SetTranscriptJSON transcriptData -> writer.WritePropertyName("SetTranscriptJSON"); JsonSerializer.Serialize<byte[]>(writer, transcriptData.ToArray())
            | SetTranscriptionDefaultTranscript defaultTranscript -> writer.WritePropertyName("SetTranscriptionDefaultTranscript"); JsonSerializer.Serialize<string>(writer, defaultTranscript)
            | SetTranscriptURI transcriptURI -> writer.WritePropertyName("SetTranscriptURI"); JsonSerializer.Serialize<string>(writer, transcriptURI)
            | SetTranscriptionJobsModel transcriptionJobsViewModel -> writer.WritePropertyName("SetTranscriptionJobsModel"); JsonSerializer.Serialize<TranscriptionJobsViewModel>(writer, transcriptionJobsViewModel)
            | SetFileMediaReference fileMediaReference -> writer.WritePropertyName("SetFileMediaReference"); JsonSerializer.Serialize<FileMediaReference>(writer, fileMediaReference)
            | SetTranscriptionLanguageCode transcriptionLanguageCode -> writer.WritePropertyName("SetTranscriptionLanguageCode"); JsonSerializer.Serialize<string>(writer, transcriptionLanguageCode)
            | SetTranscriptionVocabularyName vocabularyName -> writer.WritePropertyName("SetTranscriptionVocabularyName"); JsonSerializer.Serialize<string>(writer, vocabularyName)
            | SetTranslationLanguageCodeSource translationLanguageCode -> writer.WritePropertyName("SetTranslationLanguageCodeSource"); JsonSerializer.Serialize<string>(writer, translationLanguageCode)
            | SetTranslationTargetLanguages languages -> writer.WritePropertyName("SetTranslationTargetLanguages"); JsonSerializer.Serialize<RetainingStack>(writer, languages :?> RetainingStack, serializerOptions)
            | SetTranslationTerminologyNames terminologyNames -> writer.WritePropertyName("SetTranslationTerminologyNames"); JsonSerializer.Serialize<IEnumerable<string>>(writer, terminologyNames)
            | SetTranslationSegments chunker -> writer.WritePropertyName("SetTranslationSegments"); JsonSerializer.Serialize<SentenceChunker>(writer, chunker, serializerOptions)
            | SetSubtitleFilePath fileInfo -> writer.WritePropertyName("SetSubtitleFilePath"); JsonSerializer.Serialize<string>(writer, fileInfo.FullName)

    member this.Argument with get() = arg

    static member fromString idString =
        CommonUtilities.fromString<AWSArgument> idString

    static member Deserialize propertyName (jsonString:string) serializerOptions =
        let awsArgument =
            match propertyName with
            | "SetAWSInterface" ->
                let data = JsonSerializer.Deserialize<AmazonWebServiceInterface>(jsonString)
                AWSArgument.SetAWSInterface data
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
            | "SetTranscriptJSON" ->
                let data = JsonSerializer.Deserialize<byte[]>(jsonString)
                let rom = ReadOnlyMemory<byte>(data)
                AWSArgument.SetTranscriptJSON rom
            | "SetTranscriptionDefaultTranscript" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranscriptionDefaultTranscript data
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
            | "SetTranscriptionVocabularyName" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranscriptionVocabularyName data
            | "SetTranslationLanguageCodeSource" ->
                let data = JsonSerializer.Deserialize<string>(jsonString)
                AWSArgument.SetTranslationLanguageCodeSource data
            | "SetTranslationTargetLanguages" ->
                let consumable = JsonSerializer.Deserialize<RetainingStack>(jsonString, serializerOptions)
                AWSArgument.SetTranslationTargetLanguages consumable
            | "SetTranslationTerminologyNames" ->
                let data = JsonSerializer.Deserialize<IEnumerable<string>>(jsonString)
                AWSArgument.SetTranslationTerminologyNames (new List<string>(data))
            | "SetTranslationSegments" ->
                let data = JsonSerializer.Deserialize<SentenceChunker>(jsonString, serializerOptions)
                AWSArgument.SetTranslationSegments data
            | "SetSubtitleFilePath" ->
                let path = JsonSerializer.Deserialize<string>(jsonString)
                let fileInfo = new FileInfo(path)
                AWSArgument.SetSubtitleFilePath fileInfo
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" propertyName)

        AWSShareIntraModule(awsArgument)

/// Requests used by this module
type AWSRequest =
    | RequestAWSInterface
    | RequestBucket
    | RequestBucketsModel
    | RequestFileMediaReference
    | RequestS3MediaReference
    | RequestTranscriptionJobName
    | RequestTranscriptJSON
    | RequestTranscriptURI
    | RequestTranscriptionLanguageCode
    | RequestTranscriptionVocabularyName
    | RequestTranscriptionJobsModel
    | RequestTranscriptionDefaultTranscript
    | RequestTranslationLanguageCodeSource
    | RequestTranslationTargetLanguages
    | RequestTranslationTerminologyNames
    | RequestTranslationSegments
    | RequestSubtitleFilePath   // the path to a file that stores subtitles
    with
    override this.ToString() =
        match this with
        | RequestAWSInterface -> "RequestAWSInterface"
        | RequestBucket -> "RequestBucket"
        | RequestBucketsModel -> "RequestBucketsModel"
        | RequestFileMediaReference -> "RequestFileMediaReference"
        | RequestS3MediaReference -> "RequestS3MediaReference"
        | RequestTranscriptionJobName -> "RequestTranscriptionJobName"
        | RequestTranscriptJSON -> "RequestTranscriptJSON"
        | RequestTranscriptionJobsModel -> "RequestTranscriptionJobsModel"
        | RequestTranscriptionDefaultTranscript -> "RequestTranscriptionDefaultTranscript"
        | RequestTranscriptURI -> "RequestTranscriptURI"
        | RequestTranscriptionLanguageCode -> "RequestTranscriptionLanguageCode"
        | RequestTranscriptionVocabularyName -> "RequestTranscriptionVocabularyName"
        | RequestTranslationLanguageCodeSource -> "RequestTranslationLanguageCodeSource"
        | RequestTranslationTargetLanguages -> "RequestTranslationTargetLanguages"
        | RequestTranslationTerminologyNames -> "RequestTranslationTerminologyNames"
        | RequestTranslationSegments -> "RequestTranslationSegments"
        | RequestSubtitleFilePath -> "RequestSubtitleFilePath"

/// Wrapper for the requests used by this module
type AWSRequestIntraModule(awsRequest: AWSRequest) =
    
    interface IRequestIntraModule with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> IRequestIntraModule).Identifier.AsString()
            let str2 = (obj :?> IRequestIntraModule).Identifier.AsString()
            System.String.Compare(str1, str2)
        member this.Identifier with get() = Identifier (CommonUtilities.toString awsRequest)
        member this.ToString () = sprintf "AWSRequestIntraModule(%s)" (awsRequest.ToString())

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


type AWSFlagItem =
    | TranscribeSaveJSONTranscript          // save the JSON transcript generated by the Transcribe service
    | TranscribeSaveDefaultTranscript       // save the default transcript extracted from the JSON transcript file
    | TranslateSaveTranslation              // save a translated text item
    with
    static member GetNames () =
        FSharpType.GetUnionCases typeof<AWSFlagItem>
        |> Seq.map (fun caseInfo ->
            caseInfo.Name
        )
        |> Seq.toArray
    static member Create name =
        let unionCase =
            FSharpType.GetUnionCases typeof<AWSFlagItem>
            |> Seq.find (fun unionCase -> unionCase.Name = name)
        FSharpValue.MakeUnion (unionCase, Array.empty) :?> AWSFlagItem

type AWSFlag(awsFlag: AWSFlagItem) =
    interface ISaveFlag with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> ISaveFlag).Identifier
            let str2 = (obj :?> ISaveFlag).Identifier
            System.String.Compare(str1, str2)
        member this.Identifier with get() = CommonUtilities.toString awsFlag

type AWSFlagSet(flags: AWSFlagItem[]) =
    let mutable _set =
        if isNull(flags) then
            Set.empty
        else
            flags |> Seq.map (fun flagItem -> (AWSFlag(flagItem) :> ISaveFlag)) |> Set.ofSeq

    new() = AWSFlagSet(null)

    member this.Identifier with get() = "AWSFlag"

    member this.SetFlag (flag: ISaveFlag) =
        match flag with
        | :? AWSFlag -> if not (_set.Contains flag) then _set <- _set.Add flag
        | _ -> invalidArg "flag" (sprintf "%s is not an AWSFlag item" flag.Identifier)

    member this.IsSet (flag: ISaveFlag) =
        match flag with
        | :? AWSFlag -> _set.Contains flag
        | _ -> false

    interface ISaveFlagSet with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> ISaveFlagSet).Identifier
            let str2 = (obj :?> ISaveFlagSet).Identifier
            System.String.Compare(str1, str2)
        member this.Identifier with get() = this.Identifier
        member this.SetFlag flag = this.SetFlag flag
        member this.IsSet flag = this.IsSet flag
        override this.ToString(): string = System.String.Join(", ", (_set |> Seq.map(fun flag -> sprintf "%s.%s" this.Identifier flag.Identifier)))

/// The set of all possible argument types (passed to Task functions) that are of interest to the AWS module
type TaskArgumentRecord = {
    // common arguments required by many Task functions
    Notifications: NotificationsList option                 // notifications (informational or error messages) generated by function calls
    AWSInterface: AmazonWebServiceInterface option          // an interface to all defined AWS functions (including mocked versions)
    TaskIdentifier: string option
    WorkingDirectory: DirectoryInfo option
    SaveFlags: SaveFlags option

    // arguments that normally requiring user resolution (via TaskResponse.Request*)
    S3Bucket: Bucket option
    S3BucketModel: BucketViewModel option
    FileMediaReference: FileMediaReference option                       // a reference to a media file to be uploaded
    TranscriptionLanguageCode: string option                            // the language code for a transcription audio source
    TranscriptionVocabularyName: string option                          // the name of an optional transcription vocabulary

    // arguments generated in response to previous Task function calls
    TaskItem: TaskItem option
    S3MediaReference: S3MediaReference option                           // a reference to an uploaded media file
    TranscriptionJobName: string option                                 // job name used when starting a new transcription job
    TranscriptJSON: ReadOnlyMemory<byte> option                         // a JSON file downloaded from S3 that contains a transcript 
    DefaultTranscript: string option                                    // the default transcript extracted from the transcript JSON file
    TranscriptionJobsModel: TranscriptionJobsViewModel option           // the state of all transcription jobs, running or completed
    TranscriptURI: string option                                        // location of the transcript for a completed transcription job

    TranslationLanguageCodeSource: string option                        // the language code for a translation source text
    TranslationTargetLanguages: AWSIterationWrapper option              // the translation target languages
    TranslationTerminologyNames: List<string> option                    // optional list of terminologies
    TranslationSegments: SentenceChunker option                         // chunks of translated text (broken on sentence boundaries)

    SubtitleFilePath: FileInfo option                                   // the path to a file storing subtitles

    JsonEvents: byte[] option                                           // an array of bytes representing a collection of TaskEvents in JSON document format
    LogFormatEvents: byte[] option                                      // an array of bytes representing a collection of TaskEvents in binary log format
    JsonFilePath: FileInfo option                                       // the path to a file storing TaskEvents in JSON document format
    LogFormatFilePath: FileInfo option                                  // the path to a file storing TaskEvents in binary log format
}
type TaskArgumentRecord with
    static member Init () =
        {
            Notifications = None;
            AWSInterface = None;
            TaskIdentifier = None;
            WorkingDirectory = None;
            SaveFlags = None;
                
            S3Bucket = None;
            S3BucketModel = None;
            FileMediaReference = None;
            TranscriptionLanguageCode = None;
            TranscriptionVocabularyName = None;

            TaskItem = None;
            S3MediaReference = None;
            TranscriptionJobName = None;
            TranscriptJSON = None
            DefaultTranscript = None
            TranscriptionJobsModel = None;
            TranscriptURI = None;

            TranslationLanguageCodeSource = None;
            TranslationTargetLanguages = None;
            TranslationTerminologyNames = None;
            TranslationSegments = None;

            SubtitleFilePath = None;

            JsonEvents = None;
            LogFormatEvents = None;
            JsonFilePath = None;
            LogFormatFilePath = None;
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
                | SetTranscriptJSON transcriptData -> { x with TranscriptJSON = Some(transcriptData) }
                | SetTranscriptionDefaultTranscript defaultTranscript -> {x with DefaultTranscript = Some(defaultTranscript) }
                | SetTranscriptURI transcriptURI -> { x with TranscriptURI = Some(transcriptURI) }
                | SetTranscriptionJobsModel transcriptionJobsModel -> { x with TranscriptionJobsModel = Some(transcriptionJobsModel) }
                | SetFileMediaReference fileMediaReference -> { x with FileMediaReference = Some(fileMediaReference) }
                | SetTranscriptionLanguageCode transcriptionLanguageCode -> { x with TranscriptionLanguageCode = Some(transcriptionLanguageCode) }
                | SetTranscriptionVocabularyName vocabularyName -> { x with TranscriptionVocabularyName = Some(vocabularyName) }
                | SetTranslationLanguageCodeSource translationLanguageCode -> { x with TranslationLanguageCodeSource = Some(translationLanguageCode) }
                | SetTranslationTargetLanguages languages -> { x with TranslationTargetLanguages = Some(AWSIterationWrapper(languages)) }
                | SetTranslationTerminologyNames terminologyNames -> { x with TranslationTerminologyNames = Some(terminologyNames) }
                | SetTranslationSegments chunker -> { x with TranslationSegments = Some(chunker) }
                | SetSubtitleFilePath fileInfo -> { x with SubtitleFilePath = Some(fileInfo) }
            | :? StandardShareIntraModule as stdRequestIntraModule ->
                match stdRequestIntraModule.Argument with
                | SetNotificationsList notifications -> { x with Notifications = Some(notifications) }
                | SetTaskItem taskItem -> { x with TaskItem = taskItem }
                | SetTaskIdentifier taskId -> { x with TaskIdentifier = taskId }
                | SetWorkingDirectory workingDirectory -> { x with WorkingDirectory = workingDirectory }
                | SetSaveFlags saveFlags -> { x with SaveFlags = saveFlags}
                | SetJsonEvents data -> { x with JsonEvents = Some(data) }
                | SetLogFormatEvents data -> { x with LogFormatEvents = Some(data) }
                | SetOpenJsonFilePath fileInfo -> { x with JsonFilePath = Some(fileInfo) }
                | SetSaveJsonFilePath fileInfo -> { x with JsonFilePath = Some(fileInfo) }
                | SetOpenLogFormatFilePath fileInfo -> { x with LogFormatFilePath = Some(fileInfo) }
                | SetSaveLogFormatFilePath fileInfo -> { x with LogFormatFilePath = Some(fileInfo) }
            | _ -> x    // the request is not of type AWSShareIntraModule or StandardShareIntraModule therefore don't process it

        | _ -> invalidArg "response" "Expected SetArgument in AWSTaskArgumentRecord Update method"
