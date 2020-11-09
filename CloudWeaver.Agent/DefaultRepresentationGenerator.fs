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

        let generateTypeRepresentation (request: string) (response: string) (valueWriter: Utf8JsonWriter -> unit) =
            use stream = new MemoryStream()
            use writer = new Utf8JsonWriter(stream)

            writer.WriteStartObject()
            writer.WriteString("request", request)
            writer.WriteString("response", response)
            valueWriter writer            
            writer.WriteEndObject()
            writer.Flush()
            Encoding.UTF8.GetString(stream.ToArray())

        // get Request, SetArgument, underlying type
        match wrappedRequest with
        | :? AVRequestIntraModule as avRequestIntraModule ->
            match avRequestIntraModule.Request with
            | AVRequest.RequestCodecName -> generateTypeRepresentation "RequestCodecName" "SetCodecName" (fun writer -> writer.WriteString("value", ""))
            | _ -> null
        | _ -> null
