namespace CloudWeaver

open System
open System.Text.Json
open System.Text.Json.Serialization
open CloudWeaver.Types
//open CloudWeaver.AWS
//open CloudWeaver.MediaServices
open System.Collections.Generic
open TustlerModels
open System.Globalization

module public Converters =

    let CreateSerializerOptions (typeResolver: TypeResolver) =
        let addConverters (options: JsonSerializerOptions) =
            typeResolver.GetAllConverters()
            //let assembly = System.Reflection.Assembly.GetExecutingAssembly();
            //assembly.GetExportedTypes()
            //|> Seq.filter (fun exportedType ->
            //    exportedType.BaseType.Name = "JsonConverter`1"
            //)
            |> Seq.map (fun converter ->
                Activator.CreateInstance(converter) :?> JsonConverter
            )
            |> Seq.iter (fun converter ->
                options.Converters.Add(converter)
            )
        let serializerOptions = JsonSerializerOptions()
        addConverters serializerOptions
        serializerOptions

    [<RequireQualifiedAccess>]
    type JsonSerializedArrayKind =
    | None
    | IterationArgument
    | TaskItem
    | Requests

    let private read (reader: byref<Utf8JsonReader>) (typeResolver: TypeResolver) (arrayKind: JsonSerializedArrayKind) =
        let dict = System.Collections.Generic.Dictionary<string, JsonSerializedValue>()
        if reader.TokenType = JsonTokenType.StartObject then
            while reader.Read() && reader.TokenType <> JsonTokenType.EndObject do
                match reader.TokenType with
                | JsonTokenType.PropertyName ->
                    let propertyName = reader.GetString()
                    if reader.Read() then
                        let data =
                            match reader.TokenType with
                            | JsonTokenType.Null -> JsonSerializedValue.String null
                            | JsonTokenType.Number -> JsonSerializedValue.Int (reader.GetInt32())
                            | JsonTokenType.String -> // could be Guid, Datetime or string
                                let str = reader.GetString()
                                match Guid.TryParse str with
                                | (true, identifier) -> JsonSerializedValue.Identifier identifier
                                | (_, _) ->
                                    match DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind) with
                                    | (true, dt) -> JsonSerializedValue.DateTime dt
                                    | (_, _) -> JsonSerializedValue.String str
                            | JsonTokenType.StartArray ->
                                match arrayKind with
                                | JsonSerializedArrayKind.None -> raise (JsonException("Unexpected array"))
                                | JsonSerializedArrayKind.IterationArgument ->
                                    if dict.ContainsKey "ModuleName" then
                                        let data =
                                            let moduleName = JsonSerializedValue.getString (dict.["ModuleName"])
                                            let jsonDocument = JsonDocument.ParseValue (&reader)
                                            jsonDocument.RootElement.EnumerateArray()
                                            |> Seq.map (fun arrayItem ->
                                                let typeName =
                                                    match moduleName with
                                                    | "StandardShareIterationArgument" -> "CloudWeaver.Types.StandardShareIterationArgument"
                                                    | "AWSShareIterationArgument" -> "CloudWeaver.AWS.AWSShareIterationArgument"
                                                    | _ -> raise (JsonException(sprintf "Unexpected ModuleName: %s" moduleName))

                                                let deserialize = typeResolver.ResolveStaticCall(typeName, "Deserialize") :?> Func<JsonElement, IShareIterationArgument>
                                                deserialize.Invoke(arrayItem)
                                            )
                                            |> Seq.toArray
                                        JsonSerializedValue.Array data
                                    else
                                        raise (JsonException("Expecting the property ModuleName to be defined"))
                                | JsonSerializedArrayKind.TaskItem ->
                                    let data =
                                        JsonSerializer.Deserialize<IEnumerable<TaskItem>>(&reader)
                                        |> Seq.toArray
                                    JsonSerializedValue.Tasks data
                                | JsonSerializedArrayKind.Requests ->
                                    let data =
                                        JsonSerializer.Deserialize<IEnumerable<string>>(&reader)
                                        |> Seq.map (fun label ->
                                            let moduleName, request = BaseUtilities.deStringifyRequest label
                                            let typeName =
                                                match moduleName with
                                                | "StandardRequestIntraModule" -> "CloudWeaver.Types.StandardRequestIntraModule"
                                                | "AWSRequestIntraModule" -> "CloudWeaver.AWS.AWSRequestIntraModule"
                                                | "AVRequestIntraModule" -> "CloudWeaver.MediaServices.AVRequestIntraModule"
                                                | _ -> raise (JsonException(sprintf "Unknown request label: %s" label))

                                            let fromString = typeResolver.ResolveStaticCall(typeName, "FromString") :?> Func<string, IRequestIntraModule>
                                            fromString.Invoke(request)
                                        )
                                        |> Seq.toArray
                                    JsonSerializedValue.Requests data
                            | _ -> raise (JsonException())
                        dict.Add(propertyName, data)
                | _ -> ()
        dict

    /// Returns a standard conversion dictionary that other converters can make use of without needing a type converter
    type StandardConverter() =
        inherit JsonConverter<Dictionary<string, JsonSerializedValue>>()

        /// Deserialize a RetainingStack (which is an IEnumerable<IShareIterationArgument> with additional attributes)
        override this.Read(reader, typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.None
            dict

        override this.Write(writer, instance, _options) = ()


    type RetainingStackConverter() =
        inherit JsonConverter<RetainingStack>()

        /// Deserialize a RetainingStack (which is an IEnumerable<IShareIterationArgument> with additional attributes)
        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.IterationArgument

            if (dict.ContainsKey "ModuleName") && (dict.ContainsKey "Identifier") && (dict.ContainsKey "Items") then
                let moduleName = JsonSerializedValue.getString (dict.["ModuleName"])
                let identifier = JsonSerializedValue.getGuid (dict.["Identifier"])
                let items = JsonSerializedValue.getArguments (dict.["Items"])
                typeResolver.CreateRetainingStack(moduleName, identifier, items)
            else                
                raise (JsonException("Error parsing RetainingStack type"))

        /// Serialize a RetainingStack (which is an IEnumerable<IShareIterationArgument> with additional attributes)
        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("Identifier", instance.Identifier.ToString())
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
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.TaskItem

            if (dict.ContainsKey "Identifier") && (dict.ContainsKey "Ordering") && (dict.ContainsKey "Tasks") then
                let identifier = JsonSerializedValue.getGuid (dict.["Identifier"])
                let ordering =
                    let str = JsonSerializedValue.getString (dict.["Ordering"])
                    ItemOrdering.FromString str
                let tasks = JsonSerializedValue.getTasks (dict.["Tasks"])
                TaskSequence(identifier, tasks, ordering)
            else                
                raise (JsonException("Error parsing TaskSequence"))

        /// Serialize a TaskSequence (which is an IEnumerable<TaskItem> with additional attributes)
        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("Ordering", instance.Ordering.ToString())
            writer.WriteString("Identifier", instance.Identifier.ToString())
            writer.WriteNumber("Total", instance.Total)
            writer.WriteNumber("Remaining", instance.Remaining)
            writer.WritePropertyName("Tasks")
            JsonSerializer.Serialize(writer, (instance :> IEnumerable<TaskItem>))
            writer.WriteEndObject()


    /// Json converter for FilePickerPath (contains an enum-like union type)
    type FilePickerPathConverter() =
        inherit JsonConverter<FilePickerPath>()

        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.None

            if (dict.ContainsKey "Path") && (dict.ContainsKey "Extension") && (dict.ContainsKey "Mode") then
                let path = JsonSerializedValue.getString (dict.["Path"])
                let extension = JsonSerializedValue.getString (dict.["Extension"])
                let mode =
                    let strValue = JsonSerializedValue.getString (dict.["Mode"])
                    match strValue with
                    | "Open" -> FilePickerMode.Open
                    | "Save" -> FilePickerMode.Save
                    | _ -> invalidArg "FilePickerMode" "Value not set"
                FilePickerPath(path, extension, mode)
            else                
                raise (JsonException("Error parsing FilePickerPath"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("Path", instance.Path)
            writer.WriteString("Extension", instance.Extension)
            writer.WriteString("Mode", instance.Mode.ToString())
            writer.WriteEndObject()

    /// Json converter for Bucket
    type BucketConverter() =
        inherit JsonConverter<Bucket>()

        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.None

            if (dict.ContainsKey "Name") && (dict.ContainsKey "CreationDate") then
                let name = JsonSerializedValue.getString (dict.["Name"])
                let creationDate = JsonSerializedValue.getDatetime (dict.["CreationDate"])
                Bucket( Name = name, CreationDate = creationDate )
            else                
                raise (JsonException("Error parsing Bucket"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("Name", instance.Name)
            writer.WriteString("CreationDate", instance.CreationDate)
            writer.WriteEndObject()

    /// Json converter for SubTaskInputs
    type SubTaskInputsConverter() =
        inherit JsonConverter<SubTaskInputs>()

        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.Requests

            if (dict.ContainsKey "Requests") then
                let requests = JsonSerializedValue.getRequests (dict.["Requests"])
                SubTaskInputs(requests)
            else
                raise (JsonException("Error parsing SubTaskInputs"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteStartArray("Requests")
            if not (isNull instance.Requests) then
                instance.Requests
                |> Seq.iter (fun request -> writer.WriteStringValue(request.ToString()))
            writer.WriteEndArray()
            writer.WriteEndObject()
