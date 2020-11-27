namespace CloudWeaver

open System
open System.Text.Json
open System.Text.Json.Serialization
open CloudWeaver.Types
open CloudWeaver.AWS
//open CloudWeaver.MediaServices
open System.Collections.Generic
open TustlerModels
open System.Globalization

module public Converters =

    let CreateSerializerOptions () =
        let addConverters (options: JsonSerializerOptions) =
            let assembly = System.Reflection.Assembly.GetExecutingAssembly();
            assembly.GetExportedTypes()
            |> Seq.filter (fun exportedType ->
                exportedType.BaseType.Name = "JsonConverter`1"
            )
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
                                                match moduleName with
                                                | "StandardShareIterationArgument" -> StandardShareIterationArgument.Deserialize arrayItem
                                                | "AWSShareIterationArgument" -> AWSShareIterationArgument.Deserialize arrayItem
                                                | _ -> raise (JsonException(sprintf "Unexpected ModuleName: %s" moduleName))
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
                                                | _ -> invalidArg "label" (sprintf "Unknown request label: %s" label)
                                            let fromString = typeResolver.ResolveStaticCall(typeName, "FromString") :?> Func<string, IRequestIntraModule>
                                            fromString.Invoke(request)
                                        )
                                        |> Seq.toArray
                                    JsonSerializedValue.Requests data
                            | _ -> raise (JsonException())
                        dict.Add(propertyName, data)
                | _ -> ()
        dict

    type RetainingStackConverter() =
        inherit JsonConverter<RetainingStack>()

        /// Deserialize a RetainingStack (which is an IEnumerable<IShareIterationArgument> with additional attributes)
        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.IterationArgument

            if (dict.ContainsKey "Identifier") && (dict.ContainsKey "ModuleName") && (dict.ContainsKey "Items") then
                let identifier = JsonSerializedValue.getGuid (dict.["Identifier"])
                let items = JsonSerializedValue.getArguments (dict.["Items"])
                match (JsonSerializedValue.getString (dict.["ModuleName"])) with
                | "StandardShareIterationArgument" -> StandardIterationStack(identifier, items) :> RetainingStack
                | "AWSShareIterationArgument" -> AWSIterationStack(identifier, items) :> RetainingStack
                | _ -> raise (JsonException("Expecting a ModuleName property"))
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

    /// Json converter for LanguageCodeDomain (contains an enum-like union type)
    type LanguageCodeDomainConverter() =
        inherit JsonConverter<LanguageCodeDomain>()

        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.None

            if (dict.ContainsKey "LanguageDomain") && (dict.ContainsKey "Name") && (dict.ContainsKey "Code") then
                let languageDomain =
                    let strValue = JsonSerializedValue.getString (dict.["LanguageDomain"])
                    match strValue with
                    | "Transcription" -> LanguageDomain.Transcription
                    | "Translation" -> LanguageDomain.Translation
                    | _ -> invalidArg "LanguageDomain" "Value not set"
                let name = JsonSerializedValue.getString (dict.["Name"])
                let code = JsonSerializedValue.getString (dict.["Code"])
                LanguageCodeDomain(languageDomain, name, code)
            else                
                raise (JsonException("Error parsing LanguageCodeDomain"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("LanguageDomain", instance.LanguageDomain.ToString())
            writer.WriteString("Name", instance.Name)
            writer.WriteString("Code", instance.Code)
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

    /// Json converter for VocabularyName
    type VocabularyNameConverter() =
        inherit JsonConverter<VocabularyName>()

        override this.Read(reader, _typeToConvert, _options) =
            let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
            let dict = read &reader typeResolver JsonSerializedArrayKind.None

            if (dict.ContainsKey "VocabularyName") then
                let name = JsonSerializedValue.getString (dict.["VocabularyName"])
                VocabularyName(name)
            else
                raise (JsonException("Error parsing VocabularyName"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            if instance.VocabularyName.IsSome then
                writer.WriteString("VocabularyName", instance.VocabularyName.Value)
            else
                writer.WriteNull("VocabularyName")
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
