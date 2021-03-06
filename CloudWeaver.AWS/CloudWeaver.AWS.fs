namespace CloudWeaver.AWS

open System
open TustlerServicesLib
open TustlerModels
open System.Collections.Generic
open CloudWeaver.Types

open TustlerAWSLib
open System.Text.Json
open System.IO
open Microsoft.FSharp.Reflection
open System.Text

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

type LanguageDomain =
    | NotSet
    | Translation
    | Transcription
    with
    override this.ToString() =
        match this with
        | NotSet -> "NotSet"
        | Translation -> "Translation"
        | Transcription -> "Transcription"

/// A reference to a language code for a specified language domain
type LanguageCodeDomain(languageDomain: LanguageDomain, name: string, code: string) =
    let mutable _languageDomain = languageDomain
    let mutable _name = name
    let mutable _code = code

    member this.LanguageDomain with get() = _languageDomain and set(value) = _languageDomain <- value
    member this.Name with get() = _name and set(value) = _name <- value
    member this.Code with get() = _code and set(value) = _code <- value

    new() = LanguageCodeDomain(NotSet, null, null)

/// A reference to a nullable name of an AWS Transcription vocabulary
type VocabularyName(vocabularyName: string) =
    let mutable _vocabularyName = if isNull vocabularyName then None else Some(vocabularyName)

    member this.VocabularyName with get() = _vocabularyName and set(value) = _vocabularyName <- value

    new() = VocabularyName(null)

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
        member this.Identifier with get() = Identifier (BaseUtilities.toString arg)
        member this.ToString () = sprintf "AWSShowIntraModule(%s)" (arg.ToString())

    member this.Argument with get() = arg

/// Used for iterable values (see TaskEvent.ForEach...)
[<RequireQualifiedAccess>]
type AWSIterationArgument =
    | LanguageCode of LanguageCode
    | Test of string

type AWSShareIterationArgument(arg: AWSIterationArgument) =
    interface IShareIterationArgument with
        member this.ToString () =
            match arg with
            | AWSIterationArgument.LanguageCode languageCode -> sprintf "AWSShareIterationArgument(LanguageCode: %s)" (languageCode.Name)
            | AWSIterationArgument.Test str -> sprintf "AWSShareIterationArgument(Test: %s)" str
        member this.Serialize writer =
            writer.WriteStartObject()
            match arg with
            | AWSIterationArgument.LanguageCode languageCode -> writer.WritePropertyName("LanguageCode"); JsonSerializer.Serialize(writer, languageCode)
            | AWSIterationArgument.Test str -> writer.WritePropertyName("Test"); JsonSerializer.Serialize(writer, str)
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
type AWSIterationStack(uid: Guid, items: IEnumerable<AWSShareIterationArgument>) =
    inherit RetainingStack(uid, items |> Seq.cast)

    override this.ModuleName with get() = "AWSShareIterationArgument"

module public ConsumablePatternMatcher =

    let private getWrapperFrom (consumable: IConsumable) = 
        match consumable.Current with
        | Some(argInterface) ->
            match argInterface with
            | :? AWSShareIterationArgument as arg -> arg
            | _ -> invalidArg "consumable" "Unknown item type for RetainingStack; expecting an AWSShareIterationArgument item"
        | _ -> invalidArg "consumable" "Current property returns None"

    let private getArguments (consumable: IConsumable) = 
        consumable
        |> Seq.map (fun argInterface ->
            match argInterface with
            | :? AWSShareIterationArgument as arg -> arg
            | _ -> invalidArg "consumable" "Unknown item type for RetainingStack; expecting an AWSShareIterationArgument item"
        )

    let private (| LanguageCode |) (argWrapper: AWSShareIterationArgument) =
        match argWrapper.UnWrap with
        | AWSIterationArgument.LanguageCode languageCode -> languageCode
        | _ -> invalidArg "arg" "Expecting a languageCode"

    let getLanguageCode consumable = match (getWrapperFrom consumable) with | LanguageCode arg -> arg

    let getAllLanguageCodes consumable =
        getArguments consumable
        |> Seq.map (fun arg ->
            match arg with | LanguageCode arg -> arg
        )
        |> Seq.toArray

/// Arguments used by this module.
/// These values are set on the events stack (as TaskEvent SetArgument).
type AWSArgument =
    | SetAWSInterface of AmazonWebServiceInterface
    | SetBucket of Bucket                           // set an argument on the events stack for the selected bucket
    | SetBucketsModel of BucketViewModel            // set an argument on the events stack for the available buckets
    | SetS3MediaReference of S3MediaReference                       // Amazon S3 file reference to the uploaded file
    | SetTranscriptionJobName of string                             // the name of the new transcription job
    | SetTranscriptURI of string                                    // the URI (S3 bucket/key) of the transcript from a completed transcription job
    | SetTranscriptJSON of byte[]                                   // the JSON transcript generated by the Transcribe service
    | SetTranscriptionDefaultTranscript of string                   // the default transcript extracted from the JSON transcript file
    | SetTranscriptionJobsModel of TranscriptionJobsViewModel       // the model that wraps transcription jobs
    | SetTranscriptionVocabularyName of string
    | SetLanguage of LanguageCodeDomain
    | SetTranslationTargetLanguages of IConsumable                  /// Used for iterable values (see TaskEvent.ForEach...)
    | SetTranslationTerminologyNames of List<string>
    | SetTranslationSegments of SentenceChunker
    | SetSubtitleFilePath of FileInfo
    with
    override this.ToString() =
        match this with
        | SetAWSInterface amazonWebServiceInterface -> (sprintf "SetAWSInterface: %s" (amazonWebServiceInterface.ToString()))
        | SetBucket bucket -> (sprintf "SetBucket: %s" (bucket.ToString()))
        | SetBucketsModel bucketViewModel -> (sprintf "SetBucketsModel: %s" (bucketViewModel.ToString()))
        | SetS3MediaReference s3MediaReference -> (sprintf "SetS3MediaReference: %s" (s3MediaReference.ToString()))
        | SetTranscriptionJobName transcriptionJobName -> (sprintf "SetTranscriptionJobName: %s" transcriptionJobName)
        | SetTranscriptURI transcriptURI -> (sprintf "SetTranscriptURI: %s" transcriptURI)
        | SetTranscriptJSON transcriptJSON -> (sprintf "SetTranscriptJSON: %d bytes" (transcriptJSON.Length))
        | SetTranscriptionDefaultTranscript defaultTranscript -> (sprintf "SetTranscriptionDefaultTranscript: %s..." (defaultTranscript.Substring(0, 30)))
        | SetTranscriptionJobsModel transcriptionJobsViewModel -> (sprintf "SetTranscriptionJobsModel: %s" (transcriptionJobsViewModel.ToString()))
        | SetTranscriptionVocabularyName vocabularyName -> (sprintf "SetTranscriptionVocabularyName: %s" vocabularyName)
        | SetLanguage languageCodeDomain -> (sprintf "SetLanguage: %s" languageCodeDomain.Name)
        | SetTranslationTargetLanguages languages -> (sprintf "SetTranslationTargetLanguages: %s" (System.String.Join(", ", (Seq.map (fun (lang) -> lang.ToString) languages))))
        | SetTranslationTerminologyNames terminologyNames -> (sprintf "SetTranslationTerminologyNames: %s" (System.String.Join(", ", terminologyNames)))
        | SetTranslationSegments chunker -> (sprintf "SetTranslationSegments: %s" (chunker.ToString()))
        | SetSubtitleFilePath fileInfo -> (sprintf "SetSubtitleFilePath: %s" fileInfo.FullName)

    member this.toTaskResponse(request) = TaskResponse.SetArgument (request, AWSShareIntraModule(this))
    member this.toTaskEvent(request) = TaskEvent.SetArgument(this.toTaskResponse(request));

/// Wrapper for the arguments used by this module
and AWSShareIntraModule(arg: AWSArgument) =
    interface IShareIntraModule with
        member this.ModuleTag with get() = Tag "AWSShareIntraModule"
        member this.Identifier with get() = Identifier (BaseUtilities.toString arg)
        member this.ToString () = sprintf "AWSShareIntraModule(%s)" (arg.ToString())
        member this.Description () =
            match arg with
            | SetAWSInterface awsInterface -> sprintf "AWSInterface: mocking mode %s" (if awsInterface.RuntimeOptions.IsMocked then "enabled" else "disabled")
            | SetBucket bucket -> sprintf "Bucket: %s" (bucket.Name)
            | SetBucketsModel bucketViewModel -> sprintf "BucketsModel: %s" (if bucketViewModel.Buckets.Count = 0 then "0 buckets" else System.String.Join(", ", bucketViewModel.Buckets |> Seq.map (fun bucket -> bucket.Name)))
            | SetS3MediaReference s3MediaReference -> sprintf "S3MediaReference: key %s from %s (%s)" (s3MediaReference.Key) (s3MediaReference.BucketName) (s3MediaReference.MimeType)
            | SetTranscriptionJobName jobName -> sprintf "TranscriptionJobName: %s" jobName
            | SetTranscriptJSON transcriptData -> sprintf "TranscriptJSON: %d bytes" (transcriptData.Length)
            | SetTranscriptionDefaultTranscript defaultTranscript -> sprintf "TranscriptionDefaultTranscript: %s%s" (defaultTranscript.Substring(0, min 30 (defaultTranscript.Length))) (if defaultTranscript.Length < 30 then "" else "...")
            | SetTranscriptURI transcriptURI -> sprintf "TranscriptURI: %s" transcriptURI
            | SetTranscriptionJobsModel transcriptionJobsViewModel -> sprintf "TranscriptionJobsModel: %s" (if transcriptionJobsViewModel.TranscriptionJobs.Count = 0 then "0 jobs" else System.String.Join(", ", transcriptionJobsViewModel.TranscriptionJobs |> Seq.map (fun job -> job.TranscriptionJobName) |> Seq.sort))
            | SetTranscriptionVocabularyName vocabularyName -> sprintf "TranscriptionVocabularyName: %s" vocabularyName
            | SetLanguage languageCodeDomain -> sprintf "%s Language: %s (%s)" (languageCodeDomain.LanguageDomain.ToString()) (languageCodeDomain.Name) (languageCodeDomain.Code)
            | SetTranslationTargetLanguages languages -> sprintf "TranslationTargetLanguages: %s" (System.String.Join(", ", ConsumablePatternMatcher.getAllLanguageCodes languages |> Seq.map (fun language -> language.Name)))
            | SetTranslationTerminologyNames terminologyNames -> sprintf "TranslationTerminologyNames: %s" (System.String.Join(", ", terminologyNames))
            | SetTranslationSegments chunker -> sprintf "TranslationSegments: %s (%d segments)" (if chunker.IsJobComplete then "completed" else "incomplete") (chunker.NumChunks)
            | SetSubtitleFilePath fileInfo -> sprintf "SubtitleFilePath: %s" (fileInfo.FullName)
        member this.AsBytes serializerOptions =    // returns either a UTF8-encoded string or a UTF8-encoded Json document as a byte array
            match arg with
            | SetAWSInterface awsInterface -> JsonSerializer.SerializeToUtf8Bytes(awsInterface)
            | SetBucket bucket -> JsonSerializer.SerializeToUtf8Bytes(bucket)
            | SetBucketsModel bucketViewModel -> JsonSerializer.SerializeToUtf8Bytes(bucketViewModel)
            | SetS3MediaReference s3MediaReference -> JsonSerializer.SerializeToUtf8Bytes(s3MediaReference)
            | SetTranscriptionJobName jobName -> UTF8Encoding.UTF8.GetBytes(jobName)
            | SetTranscriptJSON transcriptData -> JsonSerializer.SerializeToUtf8Bytes(transcriptData)
            | SetTranscriptionDefaultTranscript defaultTranscript -> UTF8Encoding.UTF8.GetBytes(defaultTranscript)
            | SetTranscriptURI transcriptURI -> UTF8Encoding.UTF8.GetBytes(transcriptURI)
            | SetTranscriptionJobsModel transcriptionJobsViewModel -> JsonSerializer.SerializeToUtf8Bytes(transcriptionJobsViewModel)
            | SetTranscriptionVocabularyName vocabularyName -> UTF8Encoding.UTF8.GetBytes(vocabularyName)
            | SetLanguage languageCodeDomain -> JsonSerializer.SerializeToUtf8Bytes(languageCodeDomain, serializerOptions)
            | SetTranslationTargetLanguages languages -> JsonSerializer.SerializeToUtf8Bytes(ConsumablePatternMatcher.getAllLanguageCodes languages)
            | SetTranslationTerminologyNames terminologyNames -> JsonSerializer.SerializeToUtf8Bytes(terminologyNames)
            | SetTranslationSegments chunker -> JsonSerializer.SerializeToUtf8Bytes(chunker)    //if chunker.IsJobComplete then UTF8Encoding.UTF8.GetBytes(chunker.CompletedTranslation) else UTF8Encoding.UTF8.GetBytes("Incomplete translation")
            | SetSubtitleFilePath fileInfo -> UTF8Encoding.UTF8.GetBytes(fileInfo.FullName)
        member this.Serialize writer serializerOptions =
            match arg with
            | SetAWSInterface awsInterface -> writer.WritePropertyName("SetAWSInterface"); JsonSerializer.Serialize<AmazonWebServiceInterface>(writer, awsInterface)
            | SetBucket bucket -> writer.WritePropertyName("SetBucket"); JsonSerializer.Serialize<Bucket>(writer, bucket)
            | SetBucketsModel bucketViewModel -> writer.WritePropertyName("SetBucketsModel"); JsonSerializer.Serialize<BucketViewModel>(writer, bucketViewModel)
            | SetS3MediaReference s3MediaReference -> writer.WritePropertyName("SetS3MediaReference"); JsonSerializer.Serialize<S3MediaReference>(writer, s3MediaReference)
            | SetTranscriptionJobName jobName -> writer.WritePropertyName("SetTranscriptionJobName"); JsonSerializer.Serialize<string>(writer, jobName)
            | SetTranscriptJSON transcriptData -> writer.WritePropertyName("SetTranscriptJSON"); JsonSerializer.Serialize<byte[]>(writer, transcriptData)
            | SetTranscriptionDefaultTranscript defaultTranscript -> writer.WritePropertyName("SetTranscriptionDefaultTranscript"); JsonSerializer.Serialize<string>(writer, defaultTranscript)
            | SetTranscriptURI transcriptURI -> writer.WritePropertyName("SetTranscriptURI"); JsonSerializer.Serialize<string>(writer, transcriptURI)
            | SetTranscriptionJobsModel transcriptionJobsViewModel -> writer.WritePropertyName("SetTranscriptionJobsModel"); JsonSerializer.Serialize<TranscriptionJobsViewModel>(writer, transcriptionJobsViewModel)
            | SetTranscriptionVocabularyName vocabularyName -> writer.WritePropertyName("SetTranscriptionVocabularyName"); JsonSerializer.Serialize<VocabularyName>(writer, new VocabularyName(vocabularyName), serializerOptions)
            | SetLanguage languageCodeDomain -> writer.WritePropertyName("SetLanguage"); JsonSerializer.Serialize<LanguageCodeDomain>(writer, languageCodeDomain, serializerOptions)
            | SetTranslationTargetLanguages languages -> writer.WritePropertyName("SetTranslationTargetLanguages"); JsonSerializer.Serialize<RetainingStack>(writer, languages :?> RetainingStack, serializerOptions)
            | SetTranslationTerminologyNames terminologyNames -> writer.WritePropertyName("SetTranslationTerminologyNames"); JsonSerializer.Serialize<IEnumerable<string>>(writer, terminologyNames)
            | SetTranslationSegments chunker -> writer.WritePropertyName("SetTranslationSegments"); JsonSerializer.Serialize<SentenceChunker>(writer, chunker, serializerOptions)
            | SetSubtitleFilePath fileInfo -> writer.WritePropertyName("SetSubtitleFilePath"); JsonSerializer.Serialize<string>(writer, fileInfo.FullName)

    member this.Argument with get() = arg

    static member fromString idString =
        BaseUtilities.fromString<AWSArgument> idString

    static member Deserialize propertyName (jsonString:string) serializerOptions =
        let awsArgument =
            match propertyName with
            | "SetAWSInterface" ->
                let data = JsonSerializer.Deserialize<AmazonWebServiceInterface>(jsonString, serializerOptions)
                AWSArgument.SetAWSInterface data
            | "SetBucket" ->
                let data = JsonSerializer.Deserialize<Bucket>(jsonString, serializerOptions)
                AWSArgument.SetBucket data
            | "SetBucketsModel" ->
                let data = JsonSerializer.Deserialize<BucketViewModel>(jsonString, serializerOptions)
                AWSArgument.SetBucketsModel data
            | "SetS3MediaReference" ->
                let data = JsonSerializer.Deserialize<S3MediaReference>(jsonString, serializerOptions)
                AWSArgument.SetS3MediaReference data
            | "SetTranscriptionJobName" ->
                let data = JsonSerializer.Deserialize<string>(jsonString, serializerOptions)
                AWSArgument.SetTranscriptionJobName data
            | "SetTranscriptJSON" ->
                let data = JsonSerializer.Deserialize<byte[]>(jsonString, serializerOptions)
                AWSArgument.SetTranscriptJSON data
            | "SetTranscriptionDefaultTranscript" ->
                let data = JsonSerializer.Deserialize<string>(jsonString, serializerOptions)
                AWSArgument.SetTranscriptionDefaultTranscript data
            | "SetTranscriptURI" ->
                let data = JsonSerializer.Deserialize<string>(jsonString, serializerOptions)
                AWSArgument.SetTranscriptURI data
            | "SetTranscriptionJobsModel" ->
                let data = JsonSerializer.Deserialize<TranscriptionJobsViewModel>(jsonString, serializerOptions)
                AWSArgument.SetTranscriptionJobsModel data
            | "SetTranscriptionVocabularyName" ->
                let data = JsonSerializer.Deserialize<VocabularyName>(jsonString, serializerOptions)
                AWSArgument.SetTranscriptionVocabularyName (if data.VocabularyName.IsSome then data.VocabularyName.Value else null)
            | "SetLanguage" ->
                let data = JsonSerializer.Deserialize<LanguageCodeDomain>(jsonString, serializerOptions)
                AWSArgument.SetLanguage data
            | "SetTranslationTargetLanguages" ->
                let consumable = JsonSerializer.Deserialize<RetainingStack>(jsonString, serializerOptions)
                AWSArgument.SetTranslationTargetLanguages consumable
            | "SetTranslationTerminologyNames" ->
                let data = JsonSerializer.Deserialize<IEnumerable<string>>(jsonString, serializerOptions)
                AWSArgument.SetTranslationTerminologyNames (new List<string>(data))
            | "SetTranslationSegments" ->
                let data = JsonSerializer.Deserialize<SentenceChunker>(jsonString, serializerOptions)
                AWSArgument.SetTranslationSegments data
            | "SetSubtitleFilePath" ->
                let path = JsonSerializer.Deserialize<string>(jsonString, serializerOptions)
                let fileInfo = new FileInfo(path)
                AWSArgument.SetSubtitleFilePath fileInfo
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" propertyName)

        AWSShareIntraModule(awsArgument)

/// Requests used by this module
type AWSRequest =
    | RequestAWSInterface
    | RequestBucket
    | RequestBucketsModel
    | RequestS3MediaReference
    | RequestTranscriptionJobName
    | RequestTranscriptJSON
    | RequestTranscriptURI
    | RequestTranscriptionLanguageCode              // maps to SetLanguage
    | RequestTranscriptionVocabularyName
    | RequestTranscriptionJobsModel
    | RequestTranscriptionDefaultTranscript
    | RequestTranslationLanguageCodeSource          // maps to SetLanguage
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
    static member fromString(label) =
        let arg = BaseUtilities.fromString<AWSRequest>(label)
        if arg.IsSome then
            arg.Value
        else
            invalidArg "label" "Unknown request label"

/// Wrapper for the requests used by this module
type AWSRequestIntraModule(awsRequest: AWSRequest) =
    
    interface IRequestIntraModule with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> IRequestIntraModule).Identifier.AsString()
            let str2 = (obj :?> IRequestIntraModule).Identifier.AsString()
            System.String.Compare(str1, str2)
        member this.Identifier with get() = Identifier (BaseUtilities.toString awsRequest)
        member this.ToString () = sprintf "AWSRequestIntraModule.%s" (awsRequest.ToString())

    member this.Request with get() = awsRequest
    static member FromString(label: string): IRequestIntraModule = AWSRequestIntraModule(AWSRequest.fromString(label)) :> IRequestIntraModule

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
            | RequestAWSInterface -> AWSArgument.SetAWSInterface(awsInterface).toTaskEvent(request)
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
        member this.Identifier with get() = BaseUtilities.toString awsFlag

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
        member this.ToArray(): string [] = (_set |> Seq.map(fun flag -> sprintf "%s.%s" this.Identifier flag.Identifier) |> Seq.toArray)


/// Helper type for the type resolver
type TypeResolverHelper () =

    static let getRequest (request: IRequestIntraModule) =
        match request with
        | :? AWSRequestIntraModule as awsRequestIntraModule -> awsRequestIntraModule.Request
        | _ -> invalidArg "request" "The request does not belong to this module"

    /// Get the string representation of the argument type that matches this request
    static member GetMatchingArgument(request: IRequestIntraModule) = "AWSShareIntraModule"

    /// Get the string representation of the specified request type
    static member GetRequestAsString(request: IRequestIntraModule) = (getRequest request).ToString()

    /// Construct a request from the specified request type
    static member CreateRequest(requestType: string) = AWSRequestIntraModule(AWSRequest.fromString(requestType)) :> IRequestIntraModule

    /// Generate a serialized representation of the underlying type for a Request
    static member GenerateTypeRepresentation (request: IRequestIntraModule, generator: Func<string, string, string, Action<Utf8JsonWriter>, string, string>): string =
        match (getRequest request) with
        | _ -> null         // return null to the default handler

    /// Return a serialized instance of the argument corresponding to the specified request type
    static member CreateSerializedArgument(requestType: string, arg: obj) =
        match requestType with
        | "RequestAWSInterface" -> (AWSShareIntraModule(AWSArgument.SetAWSInterface (arg :?> AmazonWebServiceInterface)) :> IShareIntraModule).AsBytes(null)
        | "RequestBucket" -> (AWSShareIntraModule(AWSArgument.SetBucket (arg :?> Bucket)) :> IShareIntraModule).AsBytes(null)
        | "RequestBucketsModel" -> (AWSShareIntraModule(AWSArgument.SetBucketsModel (arg :?> BucketViewModel)) :> IShareIntraModule).AsBytes(null)
        | "RequestS3MediaReference" -> (AWSShareIntraModule(AWSArgument.SetS3MediaReference (arg :?> S3MediaReference)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptionJobName" -> (AWSShareIntraModule(AWSArgument.SetTranscriptionJobName (arg :?> string)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptJSON" -> (AWSShareIntraModule(AWSArgument.SetTranscriptJSON (arg :?> byte[])) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptionJobsModel" -> (AWSShareIntraModule(AWSArgument.SetTranscriptionJobsModel (arg :?> TranscriptionJobsViewModel)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptionDefaultTranscript" -> (AWSShareIntraModule(AWSArgument.SetTranscriptionDefaultTranscript (arg :?> string)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptURI" -> (AWSShareIntraModule(AWSArgument.SetTranscriptURI (arg :?> string)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptionLanguageCode" -> (AWSShareIntraModule(AWSArgument.SetLanguage (arg :?> LanguageCodeDomain)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranscriptionVocabularyName" -> (AWSShareIntraModule(AWSArgument.SetTranscriptionVocabularyName (arg :?> string)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranslationLanguageCodeSource" -> (AWSShareIntraModule(AWSArgument.SetLanguage (arg :?> LanguageCodeDomain)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranslationTargetLanguages" -> (AWSShareIntraModule(AWSArgument.SetTranslationTargetLanguages (arg :?> IConsumable)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranslationTerminologyNames" -> (AWSShareIntraModule(AWSArgument.SetTranslationTerminologyNames (arg :?> List<string>)) :> IShareIntraModule).AsBytes(null)
        | "RequestTranslationSegments" -> (AWSShareIntraModule(AWSArgument.SetTranslationSegments (arg :?> SentenceChunker)) :> IShareIntraModule).AsBytes(null)
        | "RequestSubtitleFilePath" -> (AWSShareIntraModule(AWSArgument.SetSubtitleFilePath (arg :?> FileInfo)) :> IShareIntraModule).AsBytes(null)
        | _ -> invalidArg "requestType" (sprintf "Unknown request type or unable to create instance for this request: %s" requestType)

    /// Get the underlying type of an argument
    static member UnwrapInstance (intraModule: IShareIntraModule) =
        match intraModule with
        | :? AWSShareIntraModule as awsRequestIntraModule ->
            match awsRequestIntraModule.Argument with
            | _ -> invalidArg "awsRequestIntraModule.Argument" (sprintf "Unexpected AWS Module Response Argument: %s" (awsRequestIntraModule.Argument.ToString()))
        | _ -> invalidArg "intraModule" "The intraModule type does not belong to this module"

    /// Create a retaining stack that wraps iteration arguments
    static member CreateRetainingStack(identifier: Guid, items: seq<IShareIterationArgument>) =
        AWSIterationStack(identifier, items |> Seq.cast) :> RetainingStack

    /// Add a module-specific flag to the specified flag dictionary
    static member AddFlag(serializedFlagItem: string, source: Dictionary<string, ISaveFlagSet>) =
        if AWSFlagItem.GetNames() |> Seq.contains(serializedFlagItem) then
            let flagItem = AWSFlagItem.Create(serializedFlagItem)
            let awsFlag = AWSFlag(flagItem)
            
            let awsFlagSet =
                if source.ContainsKey("AWSFlagSet") then
                    (source.["AWSFlagSet"]) :?> AWSFlagSet
                else
                    let flagSet = new AWSFlagSet()
                    source.Add("AWSFlagSet", flagSet)
                    flagSet

            awsFlagSet.SetFlag(awsFlag)
        else
            invalidArg "serializedFlagItem" (sprintf "Flag is not a AWSFlagItem: %s" serializedFlagItem)

        source

/// a library of active recognizers (active pattern functions) that retrieve strongly typed values from a map of arguments
// e.g. let (PatternMatchers.AWSInterface awsInterface) = argMap
module public PatternMatchers =

    let lookupArgument (key: AWSRequestIntraModule) (argMap: Map<IRequestIntraModule, IShareIntraModule>) = 
        if argMap.ContainsKey key then
            Some((argMap.[key] :?> AWSShareIntraModule).Argument)
        else
            None    // key not set

    let private (| AWSInterface |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestAWSInterface)
        match (lookupArgument key argMap) with
        | Some(SetAWSInterface arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an AWSInterface"

    let private (| Bucket |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestBucket)
        match (lookupArgument key argMap) with
        | Some(SetBucket arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a Bucket"

    let private (| BucketsModel |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestBucketsModel)
        match (lookupArgument key argMap) with
        | Some(SetBucketsModel arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a BucketsModel"

    let private (| S3MediaReference |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestS3MediaReference)
        match (lookupArgument key argMap) with
        | Some(SetS3MediaReference arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an S3MediaReference"

    let private (| TranscriptionJobName |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobName)
        match (lookupArgument key argMap) with
        | Some(SetTranscriptionJobName arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptionJobName"

    let private (| TranscriptJSON |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON)
        match (lookupArgument key argMap) with
        | Some(SetTranscriptJSON arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptJSON"

    let private (| TranscriptionDefaultTranscript |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript)
        match (lookupArgument key argMap) with
        | Some(SetTranscriptionDefaultTranscript arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptionDefaultTranscript"

    let private (| TranscriptURI |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptURI)
        match (lookupArgument key argMap) with
        | Some(SetTranscriptURI arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptURI"

    let private (| TranscriptionJobsModel |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobsModel)
        match (lookupArgument key argMap) with
        | Some(SetTranscriptionJobsModel arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptionJobsModel"

    let private (| TranscriptionLanguageCode |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode)
        match (lookupArgument key argMap) with
        | Some(SetLanguage arg) -> if arg.LanguageDomain = LanguageDomain.Transcription then Some(arg) else invalidArg "arg" "Expecting a TranscriptionLanguageCode"
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptionLanguageCode"

    let private (| TranscriptionVocabularyName |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName)
        match (lookupArgument key argMap) with
        | Some(SetTranscriptionVocabularyName arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranscriptionVocabularyName"

    let private (| TranslationLanguageCodeSource |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource)
        match (lookupArgument key argMap) with
        | Some(SetLanguage arg) -> if arg.LanguageDomain = LanguageDomain.Translation then Some(arg) else invalidArg "arg" "Expecting a TranslationLanguageCodeSource"
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TranslationLanguageCodeSource"

    let private (| TranslationTargetLanguages |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages)
        match (lookupArgument key argMap) with
        | Some(SetTranslationTargetLanguages arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting TranslationTargetLanguages"

    let private (| TranslationTerminologyNames |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames)
        match (lookupArgument key argMap) with
        | Some(SetTranslationTerminologyNames arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting TranslationTerminologyNames"

    let private (| TranslationSegments |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestTranslationSegments)
        match (lookupArgument key argMap) with
        | Some(SetTranslationSegments arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting TranslationSegments"

    let private (| SubtitleFilePath |) argMap =
        let key = AWSRequestIntraModule(AWSRequest.RequestSubtitleFilePath)
        match (lookupArgument key argMap) with
        | Some(SetSubtitleFilePath arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a SubtitleFilePath"    


    let getAWSInterface argMap = match (argMap) with | AWSInterface arg -> arg

    let getBucket argMap = match (argMap) with | Bucket arg -> arg

    let getBucketsModel argMap = match (argMap) with | BucketsModel arg -> arg

    let getS3MediaReference argMap = match (argMap) with | S3MediaReference arg -> arg

    let getTranscriptionJobName argMap = match (argMap) with | TranscriptionJobName arg -> arg

    let getTranscriptJSON argMap = match (argMap) with | TranscriptJSON arg -> arg

    let getTranscriptionDefaultTranscript argMap = match (argMap) with | TranscriptionDefaultTranscript arg -> arg

    let getTranscriptURI argMap = match (argMap) with | TranscriptURI arg -> arg

    let getTranscriptionJobsModel argMap = match (argMap) with | TranscriptionJobsModel arg -> arg

    let getTranscriptionLanguageCode argMap = match (argMap) with | TranscriptionLanguageCode arg -> arg

    let getTranscriptionVocabularyName argMap = match (argMap) with | TranscriptionVocabularyName arg -> arg

    let getTranslationLanguageCodeSource argMap = match (argMap) with | TranslationLanguageCodeSource arg -> arg

    let getTranslationTargetLanguages argMap = match (argMap) with | TranslationTargetLanguages arg -> arg

    let getTranslationTerminologyNames argMap = match (argMap) with | TranslationTerminologyNames arg -> arg

    let getTranslationSegments argMap = match (argMap) with | TranslationSegments arg -> arg

    let getSubtitleFilePath argMap = match (argMap) with | SubtitleFilePath arg -> arg
