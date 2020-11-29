namespace CloudWeaver

open CloudWeaver.Types
open System.Text.Json
open System.IO
open System.Text
open System

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
        let generateTypeRepresentation (moduleName: string) (request: string) (response: string) (valueWriter: Action<Utf8JsonWriter>) (label: string) =
            use stream = new MemoryStream()
            use writer = new Utf8JsonWriter(stream)

            writer.WriteStartObject()
            writer.WriteString("module", moduleName)
            writer.WriteString("request", request)
            writer.WriteString("response", response)
            valueWriter.Invoke(writer)            
            writer.WriteString("label", label)
            writer.WriteEndObject()
            writer.Flush()
            Encoding.UTF8.GetString(stream.ToArray())

        // use the TypeResolver to map the request to the right type instance
        // e.g. the one belonging to CloudWeaver.MediaServices (see TypeResolverHelper in CloudWeaver.MediaServices)
        let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
        let generator = new Func<string, string, string, Action<Utf8JsonWriter>, string, string>(generateTypeRepresentation)
        typeResolver.GenerateTypeRepresentation(wrappedRequest, generator)
