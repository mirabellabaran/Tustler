namespace CloudWeaver

open System
open System.Collections.Generic
open TustlerServicesLib
open System.Collections.Concurrent
open System.Threading.Tasks
open CloudWeaver.Types
open System.IO
open System.Text.Json

type public Agent(knownArguments:KnownArgumentsCollection, retainResponses: bool) =

    let mutable loggedCount = 0
    let mutable uiResponsePending = false

    let standardVariables = StandardVariables()

    let callTaskEvent = new Event<EventHandler<_>, _>()
    let newUIResponseEvent = new Event<EventHandler<_>, _>()
    let saveArgumentsEvent = new Event<EventHandler<_>, _>()
    let errorEvent = new Event<EventHandler<_>, _>()

    do
        knownArguments.AddModule(standardVariables)

    // the event stack: ground truth for the events generated in a given session (start task to TaskResponse.TaskComplete)
    let events = new List<TaskEvent>()

    // the responses generated from the last task function call (useful for testing purposes)
    let taskResponses = if retainResponses then Some(new List<TaskResponse>()) else None

    let getUnloggedEvents () =
        
        let options = JsonWriterOptions(Indented = false)

        let unloggedSerializedData =
            events
            |> Seq.skip loggedCount
            |> Seq.map (fun event ->

                // serialize each event as its own JSON data (not part of the same JSON document)
                // this is so that the log file can be closed without calling WriteEndObject or WriteEndArray
                use stream = new MemoryStream()
                let result = using (new Utf8JsonWriter(stream, options)) (fun writer ->

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
                    writer.Flush()
                    stream.ToArray()
                )

                result
            )
            |> Seq.toArray

        loggedCount <- events.Count
        unloggedSerializedData

    let setLoggedEvents (blocks: List<byte[]>) (moduleLookup: Func<string, Func<string, string, IShareIntraModule>>) =
        
        let options = new JsonDocumentOptions(AllowTrailingCommas = true)

        let loggedEvents =
            blocks
            |> Seq.map (fun block ->
                let mutable taskEvent = None
                let sequence = ReadOnlyMemory<byte>(block)
                use document = JsonDocument.Parse(sequence, options)

                // expecting a single child object
                document.RootElement.EnumerateObject()
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
            )
            |> Seq.choose id
            |> Seq.toArray

        events.Clear()
        events.AddRange(loggedEvents)

    /// Get the last ForEachTask RetainingStack on the event stack (if there is one)
    let getCurrentTaskLoopStack () =

        // ... first get the last ForEachTask event
        let lastForEach =
            events
            |> Seq.tryFindBack (fun evt ->
                match evt with
                | TaskEvent.ForEachTask _ -> true
                | _ -> false
            )

        // ... and retrieve the internal RetainingStack
        match lastForEach with
        | Some(TaskEvent.ForEachTask items) -> Some(items)
        | _ -> None

    /// Get the last ForEachTask RetainingStack on the event stack (if there is one)
    let getCurrentDataLoopStack () =

        // ... first get the last ForEachTask event
        let lastForEach =
            events
            |> Seq.tryFindBack (fun evt ->
                match evt with
                | TaskEvent.ForEachDataItem _ -> true
                | _ -> false
            )

        // ... and retrieve the internal RetainingStack
        match lastForEach with
        | Some(TaskEvent.ForEachDataItem items) -> Some(items)
        | _ -> None

    let startNewTask (self:Agent) (stack:RetainingStack<TaskItem>) =
        // check if task is independant
        let isIndependantTask = stack.Ordering = RetainingStack<TaskItem>.ItemOrdering.Independant
        if isIndependantTask then
            // independant tasks cannot share arguments; clear all arguments
            events.Add(TaskEvent.ClearArguments)

        let nextTask = stack.Pop();
        events.Add(TaskEvent.Task(nextTask))

        // update task item variable
        standardVariables.SetValue(StandardRequest.RequestTaskItem, nextTask);
        
        callTaskEvent.Trigger(self, nextTask)

    let nextTask (self:Agent) =
        events.Add(TaskEvent.FunctionCompleted)
        let taskStack = getCurrentTaskLoopStack ()
        if taskStack.IsSome then
            if taskStack.Value.Count > 0 then
                startNewTask self taskStack.Value
            else
                // check for a data loop (ForEachDataItem)
                let dataStack = getCurrentDataLoopStack ()
                // there must be at least one data value remaining for the task function to consume
                if dataStack.IsSome && dataStack.Value.Count > 1 then
                    dataStack.Value.Consume()
                    taskStack.Value.Reset()     // start the sequence of tasks from the beginning
                    startNewTask self taskStack.Value

    let addArgumentEvent (self:Agent) response =
        events.Add(TaskEvent.SetArgument(response))
        newUIResponseEvent.Trigger(self, response)

    // For each task: add a marker event on the events stack (execute each task in the sequence)
    let addTaskSequenceEvent taskSequence =
        let subTasks = new RetainingStack<TaskItem>(taskSequence, RetainingStack<TaskItem>.ItemOrdering.Sequential)
        events.Add(TaskEvent.ForEachTask(subTasks))

    // For each item of data: add a marker event on the events stack (execute tasks in the sequence in the context of the current data item)
    let addDataSequenceEvent (data:IConsumable) taskSequence =
        events.Add(TaskEvent.ForEachDataItem(data))
        let subTasks = new RetainingStack<TaskItem>(taskSequence, RetainingStack<TaskItem>.ItemOrdering.Sequential)
        events.Add(TaskEvent.ForEachTask(subTasks))

    let processResponse self (taskInfo: TaskItem) response =
        if retainResponses then
            taskResponses.Value.Add(response)
        match response with
        | TaskResponse.SetArgument _ -> addArgumentEvent self response
        //| TaskResponse.SetBoundaryArgument _ -> addArgumentEvent self response
        
        // resolve requests for common arguments immediately (other requests get passed to the UI)
        | TaskResponse.RequestArgument arg when knownArguments.IsKnownArgument(arg) ->
            events.Add(knownArguments.GetKnownArgument(arg))
            callTaskEvent.Trigger(self, taskInfo)
        | TaskResponse.TaskSequence taskSequence -> addTaskSequenceEvent taskSequence
        | TaskResponse.TaskContinue delayMilliseconds ->
            Async.AwaitTask (Task.Delay(delayMilliseconds)) |> Async.RunSynchronously
            callTaskEvent.Trigger(self, taskInfo)
        | TaskResponse.TaskComplete _ ->
            newUIResponseEvent.Trigger(self, response)
            nextTask self
        | TaskResponse.BeginLoopSequence (consumable, taskSequence) -> addDataSequenceEvent consumable taskSequence
        | TaskResponse.TaskArgumentSave ->
            // the event handler should asynchronously invoke a function to save the supplied event stack arguments
            // (note that by the time this function is invoked, the events stack may be in the process of being modified via new incoming responses
            // therefore a copy is passed to iterate over)
            let eventsCopy = events.ToArray()
            saveArgumentsEvent.Trigger(self, eventsCopy)
        | _ ->
            let pendingUIResponse =
                match response with
                | TaskResponse.RequestArgument _ -> true
                | TaskResponse.TaskMultiSelect _ -> true
                | TaskResponse.TaskPrompt _ -> true
                | TaskResponse.TaskSelect _ -> events.Add(TaskEvent.SelectArgument); true
                | _ -> false
            uiResponsePending <- pendingUIResponse
            newUIResponseEvent.Trigger(self, response)    // receiver would typically call Dispatcher.InvokeAsync to invoke a function to add the response to the user interface

    let processor self taskInfo responses =
        async {
            try
                responses
                |> Seq.iter (fun response -> processResponse self taskInfo response)
            with
                | :? AggregateException as ex ->
                    let errorInfo = NotificationsList.CreateErrorNotification ("TaskQueue: queueWriter", ex.InnerException.Message, ex.InnerException)
                    errorEvent.Trigger(self, errorInfo)
        }

    let runTask self taskInfo responses =
        let writer = processor self taskInfo responses
        Async.StartAsTask writer

    member this.PrepareFunctionArguments (args: InfiniteList<MaybeResponse>) =
        // replay the observed events, adding the arguments that have been set
        events
        |> Seq.iter (fun evt ->
            match evt with
            | TaskEvent.SetArgument response -> args.Add(MaybeResponse.Just(response))
            | TaskEvent.ClearArguments -> args.Clear()
            | _ -> ()
        )

    /// Process the responses from the current task function
    member this.RunTask taskInfo (responses: seq<TaskResponse>) =
        events.Add(TaskEvent.InvokingFunction)
        uiResponsePending <- false

        // collect responses from just the current call to the task function
        if retainResponses then taskResponses.Value.Clear()

        // The TaskItem parameter is to allow the current task to be queued for recall when specified by the task function
        runTask this taskInfo responses

    /// Start the task at the top of the stack
    member this.StartNewTask stack =
        startNewTask this stack

    /// A new UI selection has been made
    member this.NewSelection response =
        events.Add(TaskEvent.ClearArguments)
        events.Add(TaskEvent.SelectArgument)
        events.Add(TaskEvent.SetArgument(response))

    member this.IsAwaitingResponse with get () =
        uiResponsePending

    member this.AddArgument response =
        events.Add(TaskEvent.SetArgument(response))

    member this.AddEvent evt =
        events.Add(evt)

    member this.SetWorkingDirectory (directoryPath: DirectoryInfo) =
        standardVariables.SetValue(StandardRequest.RequestWorkingDirectory, directoryPath);

    member this.SetTaskIdentifier (taskId: string) =
        standardVariables.SetValue(StandardRequest.RequestTaskIdentifier, taskId);

    member this.SetSaveFlags (saveFlags: SaveFlags) =
        standardVariables.SetValue(StandardRequest.RequestSaveFlags, saveFlags)

    member this.HasFunctionCompleted () =
        match (Seq.last events) with
        | TaskEvent.FunctionCompleted -> true
        | _ -> false

    member this.GetUnloggedEvents () =
        getUnloggedEvents ()

    member this.SetLoggedEvents blocks moduleLookup =
        setLoggedEvents blocks moduleLookup

    member this.LastCallResponseList () = if retainResponses then taskResponses.Value else new List<TaskResponse>()

    [<CLIEvent>]
    member this.CallTask:IEvent<EventHandler<TaskItem>, TaskItem> = callTaskEvent.Publish

    //[<CLIEvent>]
    //member this.RecallTask:IEvent<EventHandler<EventArgs>, EventArgs> = recallTaskEvent.Publish

    [<CLIEvent>]
    member this.NewUIResponse:IEvent<EventHandler<TaskResponse>, TaskResponse> = newUIResponseEvent.Publish

    [<CLIEvent>]
    member this.SaveArguments:IEvent<EventHandler<TaskEvent[]>, TaskEvent[]> = saveArgumentsEvent.Publish

    [<CLIEvent>]
    member this.Error:IEvent<EventHandler<ApplicationErrorInfo>, ApplicationErrorInfo> = errorEvent.Publish
