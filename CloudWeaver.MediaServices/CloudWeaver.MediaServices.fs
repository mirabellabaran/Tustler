namespace CloudWeaver.MediaServices

open System.Text.Json
open TustlerFFMPEG
open CloudWeaver.Types
open TustlerFFMPEG.Types.CodecInfo

/// Arguments used by this module.
type AVArgument =
    | SetAVInterface of FFMPEGServiceInterface
    | SetCodecName of string
    | SetCodecInfo of CodecPair
    with
    member this.toSetArgumentTaskResponse() = TaskResponse.SetArgument (AVShareIntraModule(this))
    member this.toTaskEvent() = TaskEvent.SetArgument(this.toSetArgumentTaskResponse());
    override this.ToString() =
        match this with
        | SetAVInterface avServiceInterface -> (sprintf "SetAVInterface: %s" (avServiceInterface.ToString()))
        | SetCodecName codecName -> (sprintf "SetCodecName: %s" (codecName))
        | SetCodecInfo codecPair -> (sprintf "SetCodecInfo: %s" (codecPair.ToString()))

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
            | SetCodecInfo codecPair -> sprintf "CodecInfo: has encoder: %b; has decoder: %b " (not (isNull codecPair.Encoder)) (not (isNull codecPair.Decoder))
        member this.AsBytes _serializerOptions =
            match arg with
            | SetAVInterface avServiceInterface -> JsonSerializer.SerializeToUtf8Bytes(avServiceInterface)
            | SetCodecName bucket -> JsonSerializer.SerializeToUtf8Bytes(bucket)
            | SetCodecInfo codecPair -> JsonSerializer.SerializeToUtf8Bytes(codecPair)
        member this.Serialize writer _serializerOptions =
            match arg with
            | SetAVInterface avServiceInterface -> writer.WritePropertyName("SetAVInterface"); JsonSerializer.Serialize<FFMPEGServiceInterface>(writer, avServiceInterface)
            | SetCodecName codecName -> writer.WritePropertyName("SetCodecName"); JsonSerializer.Serialize<string>(writer, codecName)
            | SetCodecInfo codecPair -> writer.WritePropertyName("SetCodecInfo"); JsonSerializer.Serialize<CodecPair>(writer, codecPair)

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
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" propertyName)

        AVShareIntraModule(avArgument)

/// Requests used by this module
type AVRequest =
    | RequestAVInterface
    | RequestCodecName
    | RequestCodecInfo
    with
    override this.ToString() =
        match this with
        | RequestAVInterface -> "RequestAVInterface"
        | RequestCodecName -> "RequestCodecName"
        | RequestCodecInfo -> "RequestCodecInfo"

/// Wrapper for the requests used by this module
type AVRequestIntraModule(avRequest: AVRequest) =
    
    interface IRequestIntraModule with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> IRequestIntraModule).Identifier.AsString()
            let str2 = (obj :?> IRequestIntraModule).Identifier.AsString()
            System.String.Compare(str1, str2)
        member this.Identifier with get() = Identifier (BaseUtilities.toString avRequest)
        member this.ToString () = sprintf "AVRequestIntraModule(%s)" (avRequest.ToString())

    member this.Request with get() = avRequest

module public PatternMatchers =

    let lookupArgument (key: AVRequestIntraModule) (argMap: Map<IRequestIntraModule, IShareIntraModule>) = 
        if argMap.ContainsKey key then
            Some((argMap.[key] :?> AVShareIntraModule).Argument)
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

    let getAVInterface argMap = match (argMap) with | AVInterface arg -> arg

    let getCodecName argMap = match (argMap) with | CodecName arg -> arg

    let getCodecInfo argMap = match (argMap) with | CodecInfo arg -> arg
