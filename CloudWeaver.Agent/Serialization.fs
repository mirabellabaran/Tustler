namespace CloudWeaver

open System.Text.Json
open System.IO
open CloudWeaver.Types
open System
open System.Collections.Generic
open TustlerServicesLib

module public Serialization =

    let private SerializeEvent event (writer:Utf8JsonWriter) serializerOptions =

        writer.WriteStartObject()
        match event with
        | TaskEvent.InvokingFunction -> writer.WritePropertyName("TaskEvent.InvokingFunction"); JsonSerializer.Serialize(writer, "InvokingFunction")
        | TaskEvent.SetArgument arg ->
            match arg with
            | TaskResponse.SetArgument (req, arg) ->
                writer.WriteString("Tag", JsonEncodedText.Encode(arg.ModuleTag.AsString()))     // store the module for the argument type
                writer.WritePropertyName("TaskEvent.SetArgument");
                writer.WriteStartObject()
                writer.WritePropertyName("Request"); JsonSerializer.Serialize(writer, req.ToString())
                arg.Serialize writer serializerOptions
                writer.WriteEndObject()
            | _ -> invalidArg "event" (sprintf "Unexpected event stack set-argument type: %A" arg)
        | TaskEvent.ForEachTask consumableTaskSequence -> writer.WritePropertyName("TaskEvent.ForEachTask"); JsonSerializer.Serialize(writer, consumableTaskSequence :?> TaskSequence, serializerOptions)
        | TaskEvent.ForEachDataItem consumable -> writer.WritePropertyName("TaskEvent.ForEachDataItem"); JsonSerializer.Serialize(writer, consumable :?> RetainingStack, serializerOptions)
        | TaskEvent.ConsumedData identifier -> writer.WritePropertyName("TaskEvent.ConsumedData"); JsonSerializer.Serialize(writer, identifier)
        | TaskEvent.ConsumedTask identifier -> writer.WritePropertyName("TaskEvent.ConsumedTask"); JsonSerializer.Serialize(writer, identifier)
        | TaskEvent.Task taskItem -> writer.WritePropertyName("TaskEvent.Task"); JsonSerializer.Serialize(writer, taskItem)
        | TaskEvent.TaskError taskItem -> writer.WritePropertyName("TaskEvent.TaskError"); JsonSerializer.Serialize(writer, taskItem)
        | TaskEvent.SelectArgument -> writer.WritePropertyName("TaskEvent.SelectArgument"); JsonSerializer.Serialize(writer, "SelectArgument")
        | TaskEvent.ClearArguments -> writer.WritePropertyName("TaskEvent.ClearArguments"); JsonSerializer.Serialize(writer, "ClearArguments")
        | TaskEvent.FunctionCompleted -> writer.WritePropertyName("TaskEvent.FunctionCompleted"); JsonSerializer.Serialize(writer, "FunctionCompleted")

        writer.WriteEndObject()

    let private DeserializeEvent (typeResolver: TypeResolver) (serializedTaskEvent: JsonElement) serializerOptions =
        let mutable taskEvent: TaskEvent option = None

        serializedTaskEvent.EnumerateObject()
        |> Seq.fold (fun (acc: string option) (property: JsonProperty) -> 
            match property.Name with
            | "Tag" ->
                let moduleTag = property.Value.GetString()
                Some(moduleTag)
            | "TaskEvent.InvokingFunction" -> taskEvent <- Some(TaskEvent.InvokingFunction); None
            | "TaskEvent.SetArgument" ->
                if acc.IsSome then
                    let resolveProperty = ModuleResolver.ModuleLookup(acc.Value)
                    let wrappedArg =
                        let request, response =
                            property.Value.EnumerateObject()
                            |> Seq.fold (fun (request: IRequestIntraModule option, responseArg: IShareIntraModule option) property ->
                                if property.Name = "Request" then
                                    let request =
                                        let label = property.Value.GetString()
                                        let moduleName, requestName = BaseUtilities.deStringifyRequest label
                                        let typeName =
                                            match moduleName with
                                            | "StandardRequestIntraModule" -> "CloudWeaver.Types.StandardRequestIntraModule"
                                            | "AWSRequestIntraModule" -> "CloudWeaver.AWS.AWSRequestIntraModule"
                                            | "AVRequestIntraModule" -> "CloudWeaver.MediaServices.AVRequestIntraModule"
                                            | _ -> invalidArg "label" (sprintf "Unknown request label: %s" label)

                                        let fromString = typeResolver.ResolveStaticCall(typeName, "FromString") :?> Func<string, IRequestIntraModule>
                                        fromString.Invoke(requestName)
                                    (Some(request), None)
                                else
                                    let arg = resolveProperty.Invoke(property.Name, property.Value.GetRawText())
                                    (request, Some(arg))
                            ) (None, None)
                        if request.IsNone then invalidOp "Error parsing TaskEvent.SetArgument: Request not found"
                        if response.IsNone then invalidOp "Error parsing TaskEvent.SetArgument: Response not found"
                        request.Value, response.Value
                    let event = TaskEvent.SetArgument (TaskResponse.SetArgument wrappedArg)
                    taskEvent <- Some(event); None
                else
                    invalidOp "Error parsing TaskEvent.SetArgument"
            | "TaskEvent.ForEachTask" ->
                let taskSequence = JsonSerializer.Deserialize<TaskSequence>(property.Value.GetRawText(), serializerOptions)
                taskEvent <- Some(TaskEvent.ForEachTask taskSequence); None
            | "TaskEvent.ForEachDataItem" ->
                let stack = JsonSerializer.Deserialize<RetainingStack>(property.Value.GetRawText(), serializerOptions)
                taskEvent <- Some(TaskEvent.ForEachDataItem stack); None
            | "TaskEvent.ConsumedData" ->
                let identifier = JsonSerializer.Deserialize<Guid>(property.Value.GetRawText(), serializerOptions)
                taskEvent <- Some(TaskEvent.ConsumedData identifier); None
            | "TaskEvent.ConsumedTask" ->
                let identifier = JsonSerializer.Deserialize<Guid>(property.Value.GetRawText(), serializerOptions)
                taskEvent <- Some(TaskEvent.ConsumedTask identifier); None
            | "TaskEvent.Task" ->
                let data = JsonSerializer.Deserialize<TaskItem>(property.Value.GetRawText())
                taskEvent <- Some(TaskEvent.Task data); None
            | "TaskEvent.TaskError" ->
                let data = JsonSerializer.Deserialize<TaskItem>(property.Value.GetRawText())
                taskEvent <- Some(TaskEvent.TaskError data); None
            | "TaskEvent.SelectArgument" -> taskEvent <- Some(TaskEvent.SelectArgument); None
            | "TaskEvent.ClearArguments" -> taskEvent <- Some(TaskEvent.ClearArguments); None
            | "TaskEvent.FunctionCompleted" -> taskEvent <- Some(TaskEvent.FunctionCompleted); None
            | _ -> invalidArg "property.Name" (sprintf "TaskEvent property %s was not recognized" property.Name)
        ) None
        |> ignore

        taskEvent

    /// Serialize the provided events as a JSON document
    let SerializeEventsAsJSON events typeResolver =

        let writerOptions = JsonWriterOptions(Indented = true)
        let serializerOptions = Converters.CreateSerializerOptions(typeResolver)
        serializerOptions.Converters.Add(SentenceChunkerConverter())

        use stream = new MemoryStream()
        using (new Utf8JsonWriter(stream, writerOptions)) (fun writer ->
            writer.WriteStartObject()
            writer.WritePropertyName("Items");
            writer.WriteStartArray();

            events
            |> Seq.iter (fun event ->
                SerializeEvent event writer serializerOptions
            )

            writer.WriteEndArray()
            writer.WriteEndObject()
            writer.Flush()
            stream.ToArray()
        )

    /// Serialize the provided events into blocks of bytes, skipping past the specified number of previously logged events
    /// Note that each block of bytes encodes a standalone JSON document
    let SerializeEventsAsBytes events skipCount typeResolver =
        
        let writerOptions = JsonWriterOptions(Indented = false)
        let serializerOptions = Converters.CreateSerializerOptions(typeResolver)
        serializerOptions.Converters.Add(SentenceChunkerConverter())

        events
        |> Seq.skip skipCount
        |> Seq.map (fun event ->

            // serialize each event as its own JSON data (not part of the same JSON document)
            // this is so that the log file can be closed without calling WriteEndObject or WriteEndArray
            use stream = new MemoryStream()
            let result = using (new Utf8JsonWriter(stream, writerOptions)) (fun writer ->
                SerializeEvent event writer serializerOptions
                writer.Flush()
                stream.ToArray()
            )

            result
        )
        |> Seq.toArray

    /// Serialize the provided events as a JSON document
    let DeserializeEventsFromJSON (document:JsonDocument) typeResolver =

        let serializerOptions = Converters.CreateSerializerOptions(typeResolver)
        serializerOptions.Converters.Add(SentenceChunkerConverter())

        document.RootElement.EnumerateObject()
        |> Seq.map (fun childProperty ->
            match childProperty.Name with
            | "Items" ->
                childProperty.Value.EnumerateArray()
                |> Seq.map (fun arrayItem ->
                    DeserializeEvent typeResolver arrayItem serializerOptions
                )
                |> Seq.choose id
            | _ -> invalidOp "Expecting an Items array as first property of the root object"
        )
        |> Seq.concat
        |> Seq.toArray

    /// Deserialize the provided events using the specified module resolver to locate the correct Derserialization functions
    /// Note that each block of bytes encodes a standalone JSON document
    let DeserializeEventsFromBytes (blocks: List<byte[]>) typeResolver =
        
        let documentOptions = new JsonDocumentOptions(AllowTrailingCommas = true)
        let serializerOptions = Converters.CreateSerializerOptions(typeResolver)
        serializerOptions.Converters.Add(SentenceChunkerConverter())

        blocks
        |> Seq.map (fun block ->
            let sequence = ReadOnlyMemory<byte>(block)
            use document = JsonDocument.Parse(sequence, documentOptions)

            // expecting a single child object
            DeserializeEvent typeResolver (document.RootElement) serializerOptions
        )
        |> Seq.choose id
        |> Seq.toArray
