namespace CloudWeaver

open CloudWeaver.Types
open CloudWeaver.AWS
open CloudWeaver.MediaServices
open System.Text.Json
open System.IO
open System.Text

type DefaultRepresentationGenerator () =

    /// Returns a serialized representation of the underlying type for a Request
    // Selected representations only for now
    static member GetRepresentationFor(wrappedRequest: IRequestIntraModule) =

        // include:
        //  the name of the Argument module (not the Request module)
        //  the original request name
        //  the response name that the request maps to
        //  the Json type that is being requested (a JsonDocument that parses this representation can return the type as a JsonValueKind)
        //  the label that accompanies the control that represents this request
        //  any additional type-specific metadata
        let generateTypeRepresentation (moduleName: string) (request: string) (response: string) (valueWriter: Utf8JsonWriter -> unit) (label: string) =
            use stream = new MemoryStream()
            use writer = new Utf8JsonWriter(stream)

            writer.WriteStartObject()
            writer.WriteString("module", moduleName)
            writer.WriteString("request", request)
            writer.WriteString("response", response)
            valueWriter writer            
            writer.WriteString("label", label)
            writer.WriteEndObject()
            writer.Flush()
            Encoding.UTF8.GetString(stream.ToArray())

        // get Request, SetArgument, underlying type
        match wrappedRequest with
        | :? AVRequestIntraModule as avRequestIntraModule ->
            match avRequestIntraModule.Request with
            | AVRequest.RequestCodecName -> generateTypeRepresentation "AVShareIntraModule" "RequestCodecName" "SetCodecName" (fun writer -> writer.WriteString("value", "")) "Enter a codec name (e.g. flac):"
            | _ -> null
        | _ -> null
