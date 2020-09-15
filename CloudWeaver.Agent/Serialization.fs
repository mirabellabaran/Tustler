namespace CloudWeaver

open System.Text.Json
open System.IO
open CloudWeaver.Types
open System
open System.Collections.Generic
open TustlerServicesLib

module public Serialization =

    let private SerializeEvent event (writer:Utf8JsonWriter) =

        writer.WriteStartObject()
        match event with
        | TaskEvent.InvokingFunction -> writer.WritePropertyName("TaskEvent.InvokingFunction"); JsonSerializer.Serialize(writer, "InvokingFunction")
        | TaskEvent.SetArgument arg ->
            match arg with
            | TaskResponse.SetArgument responseArg ->
                writer.WriteString("Tag", JsonEncodedText.Encode(responseArg.ModuleTag.AsString()))     // store the module for the argument type
                writer.WritePropertyName("TaskEvent.SetArgument");
                writer.WriteStartObject()
                responseArg.Serialize(writer)
                writer.WriteEndObject()
            | _ -> invalidArg "event" (sprintf "Unexpected event stack set-argument type: %A" arg)
        | TaskEvent.ForEachTask stack -> writer.WritePropertyName("TaskEvent.ForEachTask"); JsonSerializer.Serialize(writer, stack)
        | TaskEvent.ForEachDataItem consumable -> writer.WritePropertyName("TaskEvent.ForEachDataItem"); JsonSerializer.Serialize(writer, consumable)
            // MG consumable as RetainingStack<T>
        | TaskEvent.Task taskItem -> writer.WritePropertyName("TaskEvent.Task"); JsonSerializer.Serialize(writer, taskItem)
        | TaskEvent.SelectArgument -> writer.WritePropertyName("TaskEvent.SelectArgument"); JsonSerializer.Serialize(writer, "SelectArgument")
        | TaskEvent.ClearArguments -> writer.WritePropertyName("TaskEvent.ClearArguments"); JsonSerializer.Serialize(writer, "ClearArguments")
        | TaskEvent.FunctionCompleted -> writer.WritePropertyName("TaskEvent.FunctionCompleted"); JsonSerializer.Serialize(writer, "FunctionCompleted")
        | _ -> invalidArg "event" (sprintf "Unexpected event stack type: %A" event)

        writer.WriteEndObject()

    let private DeserializeEvent (serializedTaskEvent: JsonElement) (moduleLookup: Func<string, Func<string, string, IShareIntraModule>>) =
        let mutable taskEvent = None

        serializedTaskEvent.EnumerateObject()
        |> Seq.fold (fun (acc: string option) (property: JsonProperty) -> 
            match property.Name with
            | "Tag" ->
                let moduleTag = property.Value.GetString()
                Some(moduleTag)
            | "TaskEvent.InvokingFunction" -> taskEvent <- Some(TaskEvent.InvokingFunction); None
            | "TaskEvent.SetArgument" ->
                if acc.IsSome then
                    let resolveProperty = moduleLookup.Invoke(acc.Value)
                    let wrappedArg =
                        property.Value.EnumerateObject()
                        |> Seq.map (fun property -> resolveProperty.Invoke(property.Name, property.Value.GetRawText()))
                        |> Seq.exactlyOne
                    let event = TaskEvent.SetArgument (TaskResponse.SetArgument wrappedArg)
                    taskEvent <- Some(event); None
                else
                    invalidOp "Error parsing TaskEvent.SetArgument"
            | "TaskEvent.ForEachTask" ->
                let taskItems = JsonSerializer.Deserialize<IEnumerable<TaskItem>>(property.Value.GetRawText())
                let data = RetainingStack(taskItems)
                taskEvent <- Some(TaskEvent.ForEachTask data); None
            | "TaskEvent.ForEachDataItem" ->
                let taskItems = JsonSerializer.Deserialize<IEnumerable<TaskItem>>(property.Value.GetRawText())
                let data = RetainingStack(taskItems)
                taskEvent <- Some(TaskEvent.ForEachTask data); None
            | "TaskEvent.Task" ->
                let data = JsonSerializer.Deserialize<TaskItem>(property.Value.GetRawText())
                taskEvent <- Some(TaskEvent.Task data); None
            | "TaskEvent.SelectArgument" -> taskEvent <- Some(TaskEvent.SelectArgument); None
            | "TaskEvent.ClearArguments" -> taskEvent <- Some(TaskEvent.ClearArguments); None
            | "TaskEvent.FunctionCompleted" -> taskEvent <- Some(TaskEvent.FunctionCompleted); None
            | _ -> invalidArg "property.Name" (sprintf "TaskEvent property %s was not recognized" property.Name)
        ) None
        |> ignore

        taskEvent

    /// Serialize the provided events as a JSON document
    let SerializeEventsAsJSON events =

        let options = JsonWriterOptions(Indented = true)
            
        use stream = new MemoryStream()
        using (new Utf8JsonWriter(stream, options)) (fun writer ->
            writer.WriteStartObject()
            writer.WritePropertyName("Items");
            writer.WriteStartArray();

            events
            |> Seq.iter (fun event ->
                SerializeEvent event writer
            )

            writer.WriteEndArray()
            writer.WriteEndObject()
            writer.Flush()
            stream.ToArray()
        )

    /// Serialize the provided events into blocks of bytes, skipping past the specified number of previously logged events
    /// Note that each block of bytes encodes a standalone JSON document
    let SerializeEventsAsBytes events skipCount =
        
        let options = JsonWriterOptions(Indented = false)

        events
        |> Seq.skip skipCount
        |> Seq.map (fun event ->

            // serialize each event as its own JSON data (not part of the same JSON document)
            // this is so that the log file can be closed without calling WriteEndObject or WriteEndArray
            use stream = new MemoryStream()
            let result = using (new Utf8JsonWriter(stream, options)) (fun writer ->
                SerializeEvent event writer
                writer.Flush()
                stream.ToArray()
            )

            result
        )
        |> Seq.toArray

    /// Serialize the provided events as a JSON document
    let DeserializeEventsFromJSON (document:JsonDocument) moduleLookup =

        document.RootElement.EnumerateObject()
        |> Seq.map (fun childProperty ->
            match childProperty.Name with
            | "Items" ->
                childProperty.Value.EnumerateArray()
                |> Seq.map (fun arrayItem ->
                    DeserializeEvent arrayItem moduleLookup
                )
                |> Seq.choose id
            | _ -> invalidOp "Expecting an Items array as first property of the root object"
        )
        |> Seq.concat
        |> Seq.toArray

    /// Deserialize the provided events using the specified module resolver to locate the correct Derserialization functions
    /// Note that each block of bytes encodes a standalone JSON document
    let DeserializeEventsFromBytes (blocks: List<byte[]>) moduleLookup =
        
        let options = new JsonDocumentOptions(AllowTrailingCommas = true)

        blocks
        |> Seq.map (fun block ->
            let sequence = ReadOnlyMemory<byte>(block)
            use document = JsonDocument.Parse(sequence, options)

            // expecting a single child object
            DeserializeEvent (document.RootElement) moduleLookup
        )
        |> Seq.choose id
        |> Seq.toArray
