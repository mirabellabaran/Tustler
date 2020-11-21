namespace CloudWeaver.MediaServices

open System.Text.Json
open TustlerFFMPEG
open CloudWeaver.Types
open TustlerFFMPEG.Types.CodecInfo
open TustlerFFMPEG.Types.MediaInfo

/// Arguments used by this module.
type AVArgument =
    | SetAVInterface of FFMPEGServiceInterface
    | SetCodecName of string
    | SetCodecInfo of CodecPair
    | SetMediaInfo of MediaInfo
    with
    override this.ToString() =
        match this with
        | SetAVInterface avServiceInterface -> (sprintf "SetAVInterface: %s" (avServiceInterface.ToString()))
        | SetCodecName codecName -> (sprintf "SetCodecName: %s" (codecName))
        | SetCodecInfo codecPair -> (sprintf "SetCodecInfo: %s" (codecPair.ToString()))
        | SetMediaInfo mediaInfo -> (sprintf "SetMediaInfo: %s" (mediaInfo.ToString()))

    member this.toTaskResponse(request) = TaskResponse.SetArgument (request, AVShareIntraModule(this))
    member this.toTaskEvent(request) = TaskEvent.SetArgument(this.toTaskResponse(request));

/// Wrapper for the arguments used by this module
and AVShareIntraModule(arg: AVArgument) =
    interface IShareIntraModule with
        member this.ModuleTag with get() = Tag "AVShareIntraModule"
        member this.Identifier with get() = Identifier (BaseUtilities.toString arg)
        member this.ToString () = sprintf "AVShareIntraModule(%s)" (arg.ToString())
        member this.Description () =
            match arg with
            | SetAVInterface avServiceInterface -> sprintf "AVInterface: mocking mode %s" (if avServiceInterface.RuntimeOptions.IsMocked then "enabled" else "disabled")
            | SetCodecName codecName -> sprintf "CodecName: %s" (codecName)
            | SetCodecInfo codecPair -> sprintf "CodecInfo: has encoder: %b; has decoder: %b" (not (isNull codecPair.Encoder)) (not (isNull codecPair.Decoder))
            | SetMediaInfo mediaInfo -> sprintf "MediaInfo: (%d streams)" (mediaInfo.Streams.Count)
        member this.AsBytes _serializerOptions =
            match arg with
            | SetAVInterface avServiceInterface -> JsonSerializer.SerializeToUtf8Bytes(avServiceInterface)
            | SetCodecName bucket -> JsonSerializer.SerializeToUtf8Bytes(bucket)
            | SetCodecInfo codecPair -> JsonSerializer.SerializeToUtf8Bytes(codecPair)
            | SetMediaInfo mediaInfo -> JsonSerializer.SerializeToUtf8Bytes(mediaInfo)
        member this.Serialize writer _serializerOptions =
            match arg with
            | SetAVInterface avServiceInterface -> writer.WritePropertyName("SetAVInterface"); JsonSerializer.Serialize<FFMPEGServiceInterface>(writer, avServiceInterface)
            | SetCodecName codecName -> writer.WritePropertyName("SetCodecName"); JsonSerializer.Serialize<string>(writer, codecName)
            | SetCodecInfo codecPair -> writer.WritePropertyName("SetCodecInfo"); JsonSerializer.Serialize<CodecPair>(writer, codecPair)
            | SetMediaInfo mediaInfo -> writer.WritePropertyName("SetMediaInfo"); JsonSerializer.Serialize<MediaInfo>(writer, mediaInfo)

    member this.Argument with get() = arg

    static member fromString idString =
        BaseUtilities.fromString<AVArgument> idString

    static member Deserialize propertyName (jsonString:string) serializerOptions =
        let avArgument =
            match propertyName with
            | "SetAVInterface" ->
                let data = JsonSerializer.Deserialize<FFMPEGServiceInterface>(jsonString, serializerOptions)
                AVArgument.SetAVInterface data
            | "SetCodecName" ->
                let data = JsonSerializer.Deserialize<string>(jsonString, serializerOptions)
                AVArgument.SetCodecName data
            | "SetCodecInfo" ->
                let data = JsonSerializer.Deserialize<CodecPair>(jsonString, serializerOptions)
                AVArgument.SetCodecInfo data
            | "SetMediaInfo" ->
                let data = JsonSerializer.Deserialize<MediaInfo>(jsonString, serializerOptions)
                AVArgument.SetMediaInfo data
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" propertyName)

        AVShareIntraModule(avArgument)

/// Requests used by this module
type AVRequest =
    | RequestAVInterface
    | RequestCodecName
    | RequestCodecInfo
    | RequestMediaInfo
    | RequestOpenMediaFilePath      // maps to StandardArgument.SetFilePath (see StandardShareIntraModule)
    | RequestSaveMediaFilePath      // maps to StandardArgument.SetFilePath (see StandardShareIntraModule)
    with
    override this.ToString() =
        match this with
        | RequestAVInterface -> "RequestAVInterface"
        | RequestCodecName -> "RequestCodecName"
        | RequestCodecInfo -> "RequestCodecInfo"
        | RequestMediaInfo -> "RequestMediaInfo"
        | RequestOpenMediaFilePath -> "RequestOpenMediaFilePath"
        | RequestSaveMediaFilePath -> "RequestSaveMediaFilePath"
    static member fromString(label) =
        let arg = BaseUtilities.fromString<AVRequest>(label)
        if arg.IsSome then
            arg.Value
        else
            invalidArg "label" "Unknown request label"

/// Wrapper for the requests used by this module
type AVRequestIntraModule(avRequest: AVRequest) =
    
    interface IRequestIntraModule with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> IRequestIntraModule).Identifier.AsString()
            let str2 = (obj :?> IRequestIntraModule).Identifier.AsString()
            System.String.Compare(str1, str2)
        member this.Identifier with get() = Identifier (BaseUtilities.toString avRequest)
        member this.ToString () = sprintf "AVRequestIntraModule.%s" (avRequest.ToString())

    member this.Request with get() = avRequest
    static member FromString(label: string): IRequestIntraModule = AVRequestIntraModule(AVRequest.fromString(label)) :> IRequestIntraModule

/// Wrapper for the pre-assigned values used by this module
/// (values that are known in advance by the user interface layer)
type AVKnownArguments(avInterface) =
    interface IKnownArguments with
        member this.KnownRequests with get() =
            seq {
                AVRequestIntraModule(AVRequest.RequestAVInterface);
            }
        member this.GetKnownArgument(request: IRequestIntraModule) =
            let unWrapRequest (request:IRequestIntraModule) =
                match request with
                | :? AVRequestIntraModule as avRequestIntraModule -> avRequestIntraModule.Request
                | _ -> invalidArg "request" "The request is not of type AVRequestIntraModule"
            match (unWrapRequest request) with
            | RequestAVInterface -> AVArgument.SetAVInterface(avInterface).toTaskEvent(request)
            | _ -> invalidArg "request" "The request is not a known argument"

module public PatternMatchers =

    let lookupArgument (key: AVRequestIntraModule) (argMap: Map<IRequestIntraModule, IShareIntraModule>) = 
        if argMap.ContainsKey key then
            Some((argMap.[key] :?> AVShareIntraModule).Argument)
        else
            None    // key not set

    let lookupStandardArgument (key: AVRequestIntraModule) (argMap: Map<IRequestIntraModule, IShareIntraModule>) = 
        if argMap.ContainsKey key then
            Some((argMap.[key] :?> StandardShareIntraModule).Argument)
        else
            None    // key not set

    let private (| AVInterface |) argMap =
        let key = AVRequestIntraModule(AVRequest.RequestAVInterface)
        match (lookupArgument key argMap) with
        | Some(SetAVInterface arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an AVInterface"

    let private (| CodecName |) argMap =
        let key = AVRequestIntraModule(AVRequest.RequestCodecName)
        match (lookupArgument key argMap) with
        | Some(SetCodecName arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a CodecName"

    let private (| CodecInfo |) argMap =
        let key = AVRequestIntraModule(AVRequest.RequestCodecInfo)
        match (lookupArgument key argMap) with
        | Some(SetCodecInfo arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a CodecPair"

    let private (| MediaInfo |) argMap =
        let key = AVRequestIntraModule(AVRequest.RequestMediaInfo)
        match (lookupArgument key argMap) with
        | Some(SetMediaInfo arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting MediaInfo"

    let private (| OpenMediaFilePath |) argMap =
        let key = AVRequestIntraModule(AVRequest.RequestOpenMediaFilePath)
        match (lookupStandardArgument key argMap) with
        | Some(StandardArgument.SetFilePath arg) -> if arg.Mode = FilePickerMode.Open then Some(arg) else invalidArg "arg" "Expecting a media FilePath for 'Open Media File'"
        | None -> None
        | _ -> invalidArg "arg" "Expecting a FilePath for 'Open Log Format File'"

    let private (| SaveMediaFilePath |) argMap =
        let key = AVRequestIntraModule(AVRequest.RequestSaveMediaFilePath)
        match (lookupStandardArgument key argMap) with
        | Some(SetFilePath arg) -> if arg.Mode = FilePickerMode.Save then Some(arg) else invalidArg "arg" "Expecting a media FilePath for 'Save Media File'"
        | None -> None
        | _ -> invalidArg "arg" "Expecting a FilePath for 'Save Log Format File'"

    let getAVInterface argMap = match (argMap) with | AVInterface arg -> arg

    let getCodecName argMap = match (argMap) with | CodecName arg -> arg

    let getCodecInfo argMap = match (argMap) with | CodecInfo arg -> arg

    let getMediaInfo argMap = match (argMap) with | MediaInfo arg -> arg

    let getOpenMediaFilePath argMap = match (argMap) with | OpenMediaFilePath arg -> arg

    let getSaveMediaFilePath argMap = match (argMap) with | SaveMediaFilePath arg -> arg
