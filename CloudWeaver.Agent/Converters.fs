namespace CloudWeaver

open System
open System.Text.Json
open System.Text.Json.Serialization
open CloudWeaver.Types
open CloudWeaver.AWS
open System.Collections.Generic

module public Converters =

    type JsonValue =
    | Int of int
    | String of string
    | Array of IShareIterationArgument[]
    | Tasks of TaskItem[]
    with
        static member getString value =
            match value with
            | JsonValue.String str -> str
            | _ -> raise (JsonException("Expecting the name of a string value"))

        static member getInteger value =
            match value with
            | JsonValue.Int i -> i
            | _ -> raise (JsonException("Expecting the name of an integer value"))

        static member getArray value =
            match value with
            | JsonValue.Array arr -> arr
            | _ -> raise (JsonException("Expecting the name of an array value"))

        static member getTasks value =
            match value with
            | JsonValue.Tasks tasks -> tasks
            | _ -> raise (JsonException("Expecting the name of an array of TaskItems"))

    type RetainingStackConverter() =
        inherit JsonConverter<RetainingStack>()

        /// Deserialize a RetainingStack (which is an IEnumerable<IShareIterationArgument> with additional attributes)
        override this.Read(reader, _typeToConvert, _options) =
            let dict = System.Collections.Generic.Dictionary<string, JsonValue>()
            if reader.TokenType = JsonTokenType.StartObject then
                while reader.Read() && reader.TokenType <> JsonTokenType.EndObject do
                    match reader.TokenType with
                    | JsonTokenType.PropertyName ->
                        let propertyName = reader.GetString()
                        if reader.Read() then
                            let data =
                                match reader.TokenType with
                                | JsonTokenType.Number -> JsonValue.Int (reader.GetInt32())
                                | JsonTokenType.String -> JsonValue.String (reader.GetString())
                                | JsonTokenType.StartArray ->
                                    if dict.ContainsKey "ModuleName" then
                                        let data =
                                            let moduleName = JsonValue.getString (dict.["ModuleName"])
                                            let jsonDocument = JsonDocument.ParseValue (&reader)
                                            jsonDocument.RootElement.EnumerateArray()
                                            |> Seq.map (fun arrayItem ->
                                                match moduleName with
                                                | "StandardShareIterationArgument" -> StandardShareIterationArgument.Deserialize arrayItem
                                                | "AWSShareIterationArgument" -> AWSShareIterationArgument.Deserialize arrayItem
                                                | _ -> raise (JsonException(sprintf "Unexpected ModuleName: %s" moduleName))
                                            )
                                            |> Seq.toArray
                                        JsonValue.Array data
                                    else
                                        raise (JsonException("Expecting the property ModuleName to be defined"))
                                | _ -> raise (JsonException())
                            dict.Add(propertyName, data)
                    | _ -> ()

            if (dict.ContainsKey "ModuleName") && (dict.ContainsKey "Items") then
                let items = JsonValue.getArray (dict.["Items"])
                match (JsonValue.getString (dict.["ModuleName"])) with
                | "StandardShareIterationArgument" -> StandardIterationStack(items) :> RetainingStack
                | "AWSShareIterationArgument" -> AWSIterationStack(items) :> RetainingStack
                | _ -> raise (JsonException("Expecting a ModuleName property"))
            else                
                raise (JsonException("Error parsing RetainingStack type"))

        /// Serialize a RetainingStack (which is an IEnumerable<IShareIterationArgument> with additional attributes)
        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("ModuleName", instance.ModuleName)
            writer.WriteNumber("Total", instance.Total)
            writer.WriteNumber("Remaining", instance.Remaining)
            writer.WritePropertyName("Items")
            writer.WriteStartArray()
            instance |> Seq.iter (fun iterationArgument -> iterationArgument.Serialize(writer))
            writer.WriteEndArray()
            writer.WriteEndObject()

    type TaskSequenceConverter() =
        inherit JsonConverter<TaskSequence>()

        /// Deserialize a TaskSequence (which is an IEnumerable<TaskItem> with additional attributes)
        override this.Read(reader, _typeToConvert, _options) =
            let dict = System.Collections.Generic.Dictionary<string, JsonValue>()
            if reader.TokenType = JsonTokenType.StartObject then
                while reader.Read() && reader.TokenType <> JsonTokenType.EndObject do
                    match reader.TokenType with
                    | JsonTokenType.PropertyName ->
                        let propertyName = reader.GetString()
                        if reader.Read() then
                            let data =
                                match reader.TokenType with
                                | JsonTokenType.Number -> JsonValue.Int (reader.GetInt32())
                                | JsonTokenType.String -> JsonValue.String (reader.GetString())
                                | JsonTokenType.StartArray ->
                                    let data =
                                        JsonSerializer.Deserialize<IEnumerable<TaskItem>>(&reader)
                                        |> Seq.toArray
                                    JsonValue.Tasks data
                                | _ -> raise (JsonException())
                            dict.Add(propertyName, data)
                    | _ -> ()

            if (dict.ContainsKey "Ordering") && (dict.ContainsKey "Tasks") then
                let ordering =
                    let str = JsonValue.getString (dict.["Ordering"])
                    ItemOrdering.FromString str
                let tasks = JsonValue.getTasks (dict.["Tasks"])
                TaskSequence(tasks, ordering)
            else                
                raise (JsonException("Error parsing TaskSequence"))

        /// Serialize a TaskSequence (which is an IEnumerable<TaskItem> with additional attributes)
        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("Ordering", instance.Ordering.ToString())
            writer.WriteNumber("Total", instance.Total)
            writer.WriteNumber("Remaining", instance.Remaining)
            writer.WritePropertyName("Tasks")
            JsonSerializer.Serialize(writer, (instance :> IEnumerable<TaskItem>))
            writer.WriteEndObject()
