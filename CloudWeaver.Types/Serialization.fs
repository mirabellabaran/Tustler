namespace CloudWeaver.Types

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Collections.Generic

/// A type that supports the standard converter in CloudWeaver.Converters (Agent)
type JsonSerializedValue =
    | Identifier of Guid
    | Int of int
    | String of string
    | DateTime of DateTime
    | Array of IShareIterationArgument[]
    | Tasks of TaskItem[]
    | Requests of IRequestIntraModule[]
with
    static member getGuid value =
        match value with
        | JsonSerializedValue.Identifier guid -> guid
        | _ -> raise (JsonException("Expecting the name of a Guid value"))

    static member getInteger value =
        match value with
        | JsonSerializedValue.Int i -> i
        | _ -> raise (JsonException("Expecting the name of an integer value"))

    static member getString value =
        match value with
        | JsonSerializedValue.String str -> str
        | _ -> raise (JsonException("Expecting the name of a string value"))

    static member getDatetime value =
        match value with
        | JsonSerializedValue.DateTime dt -> dt
        | _ -> raise (JsonException("Expecting the name of a DateTime value"))

    static member getArguments value =
        match value with
        | JsonSerializedValue.Array arr -> arr
        | _ -> raise (JsonException("Expecting the name of an array value"))

    static member getTasks value =
        match value with
        | JsonSerializedValue.Tasks tasks -> tasks
        | _ -> raise (JsonException("Expecting the name of an array of TaskItems"))

    static member getRequests value =
        match value with
        | JsonSerializedValue.Requests requests -> requests
        | _ -> raise (JsonException("Expecting the name of an array of sub-task requests (IRequestIntraModule)"))


module public Converters =

    /// Find the standard converter in a sequence of converters
    // The standard converter produces Dictionary<string, JsonSerializedValue> (see above)
    let getStandardConverter (converters: seq<JsonConverter>) =
        let stdOption =
            converters
            |> Seq.tryFind (fun converter ->
                converter.GetType().FullName = "CloudWeaver.Converters+StandardConverter"
                && converter.CanConvert(typeof<Dictionary<string, JsonSerializedValue>>)
            )

        match stdOption with
        | Some(std) ->
            match std with
            | :? JsonConverter<Dictionary<string, JsonSerializedValue>> as standardConverter -> standardConverter
            | _ -> raise (JsonException("The standard converter was not of the correct type"))
        | _ -> raise (JsonException("Unable to find the standard converter"))
