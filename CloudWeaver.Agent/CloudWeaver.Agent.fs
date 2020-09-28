namespace CloudWeaver

open System
open System.Collections.Generic
open TustlerServicesLib
open System.Threading.Tasks
open CloudWeaver.Types
open System.IO
open System.Text.Json

type ExecutionStackFrameType =
    | Data of IConsumable
    | Tasks of IConsumableTaskSequence

// MG TODO add an Execution stack type that is a RetainingStack<IConsumable> i.e. a stack of either RetainingStack or TaskSequence (or Task)
// This is to support deeper levels of tree execution
type public Agent(knownArguments:KnownArgumentsCollection, retainResponses: bool) =

    let mutable loggedCount = 0
    let mutable uiResponsePending = false
    let mutable errorState = false

    let standardVariables = StandardVariables()

    let callTaskEvent = new Event<EventHandler<_>, _>()
    let newUIResponseEvent = new Event<EventHandler<_>, _>()
    let saveEventsEvent = new Event<EventHandler<_>, _>()       // save some or all events as a JSON document
    let convertToJsonEvent = new Event<EventHandler<_>, _>()    // convert events in binary log format to JSON document format
    let convertToBinaryEvent = new Event<EventHandler<_>, _>()  // convert events in JSON document format to binary log format

    let errorEvent = new Event<EventHandler<_>, _>()

    do
        knownArguments.AddModule(standardVariables)

    // the event stack: ground truth for the events generated in a given session (from start of task to TaskResponse.TaskComplete)
    let events = new List<TaskEvent>()

    let executionStack = new Stack<ExecutionStackFrameType>()

    // the responses generated from the last task function call (useful for testing purposes)
    let taskResponses = if retainResponses then Some(new List<TaskResponse>()) else None

    let startNewTask (self:Agent) (stack:IConsumableTaskSequence) =
        // check if task is independant
        let isIndependantTask = stack.Ordering = ItemOrdering.Independant
        if isIndependantTask then
            // independant tasks cannot share arguments; clear all arguments
            events.Add(TaskEvent.ClearArguments)

        let nextTask = stack.ConsumeTask();
        events.Add(TaskEvent.ConsumedTask stack.Identifier)
        events.Add(TaskEvent.Task(nextTask))

        // update task item variable
        standardVariables.SetValue(StandardRequest.RequestTaskItem, nextTask)
        
        callTaskEvent.Trigger(self, nextTask)

    let rec nextTask (self:Agent) =
        if not errorState then
            // expecting a tasks frame at the top of the execution stack
            match executionStack.TryPeek() with
            | true, Tasks taskSequence ->
                if taskSequence.Remaining > 0 then
                    startNewTask self taskSequence
                else
                    // out of tasks; is there a Data frame on top?
                    let topFrame = executionStack.Pop()
                    match executionStack.TryPeek() with
                    | true, Data consumable ->
                        // there must be at least one data value remaining for the task function to consume
                        if consumable.Remaining > 1 then
                            consumable.Consume() |> ignore
                            events.Add(TaskEvent.ConsumedData consumable.Identifier)
                            taskSequence.Reset()     // start the sequence of tasks from the beginning
                            executionStack.Push(topFrame)
                            startNewTask self taskSequence
                        else
                            executionStack.Pop() |> ignore          // pop the top frame (Data) and try again
                            nextTask self
                    | _ -> nextTask self                            // leave the top frame (Tasks) popped and try again
            | true, Data _ -> invalidOp "Found data frame at top of execution stack"
            | _ -> ()                                               // nothing left to execute

    /// Replace the events stack with the specified logged events and continue execution
    let continueWith self loggedEvents =
            
        let regenerateExecutionStack () =

            let findDataFrame identifier =
                executionStack
                |> Seq.pick (fun frameType ->
                    match frameType with
                    | Data fr when fr.Identifier = identifier -> Some(fr)
                    | Data _ -> None
                    | Tasks _ -> None
                )

            let findTaskFrame identifier =
                executionStack
                |> Seq.pick (fun frameType ->
                    match frameType with
                    | Tasks fr when fr.Identifier = identifier -> Some(fr)
                    | Tasks _ -> None
                    | Data _ -> None
                )

            // replay the events
            events
            |> Seq.iter (fun event ->
                match event with
                | TaskEvent.ForEachDataItem consumable -> executionStack.Push(Data consumable)
                | TaskEvent.ForEachTask tasks -> executionStack.Push(Tasks tasks)
                | TaskEvent.ConsumedData identifier ->
                    let frame = findDataFrame identifier
                    frame.Consume() |> ignore
                | TaskEvent.ConsumedTask identifier ->
                    let frame = findTaskFrame identifier
                    frame.Consume() |> ignore
                | _ -> ()
            )

            if executionStack.Count > 0 then
                nextTask self
                if executionStack.Count = 0 then    // popped last item
                    let response = TaskResponse.TaskInfo "Log Replay: Nothing to show as the previous logged run completed successfully"
                    newUIResponseEvent.Trigger(self, response)
            else
                let response = TaskResponse.TaskInfo "Log Replay: No frames on the execution stack"
                newUIResponseEvent.Trigger(self, response)

        // find the last task and call it
        let callLastTask () =
            let lastTaskItem =
                events
                |> Seq.tryPick (fun evt ->
                    match evt with
                    | TaskEvent.Task taskInfo -> Some(taskInfo)
                    | _ -> None
                )

            if lastTaskItem.IsSome then
                standardVariables.SetValue(StandardRequest.RequestTaskItem, lastTaskItem.Value)   // update task item variable
                callTaskEvent.Trigger(self, lastTaskItem.Value)
            else
                let response = TaskResponse.TaskInfo "Log Replay: No Task event set prior to current SetArgument or InvokingFunction event"
                newUIResponseEvent.Trigger(self, response)
            
        events.Clear()
        executionStack.Clear()
        events.AddRange(loggedEvents)
        //loggedCount <- events.Count   // new log file; start from the beginning

        if events.Count > 0 then
            // match the last event
            // events are saved in blocks so the last event is limited to only the following:
            let lastEvent = events.[events.Count - 1]
            match lastEvent with
            | TaskEvent.SetArgument _ ->
                callLastTask ()
            | TaskEvent.InvokingFunction ->
                // top of stack for functions with delays such as MonitorTranscription
                callLastTask ()
            | TaskEvent.Task taskInfo ->
                callTaskEvent.Trigger(self, taskInfo)
            | TaskEvent.FunctionCompleted ->
                regenerateExecutionStack()
            | _ ->
                let response = TaskResponse.TaskInfo (sprintf "Log Replay: Unexpected event at top of stack: %A" lastEvent)
                newUIResponseEvent.Trigger(self, response)
        else
            let response = TaskResponse.TaskInfo "Log Replay: No events to replay"
            newUIResponseEvent.Trigger(self, response)


    let addArgumentEvent (self:Agent) response =
        events.Add(TaskEvent.SetArgument(response))
        newUIResponseEvent.Trigger(self, response)

    // For each task: add a marker event on the events stack (execute each task in the sequence)
    let addTaskSequenceEvent taskSequence ordering =
        let subTasks = TaskSequence(Guid.NewGuid(), taskSequence, ordering)
        events.Add(TaskEvent.ForEachTask(subTasks))
        executionStack.Push(Tasks subTasks)

    // For each item of data: add a marker event on the events stack (execute tasks in the sequence in the context of the current data item)
    let addDataSequenceEvent (data:IConsumable) taskSequence =
        events.Add(TaskEvent.ForEachDataItem(data))
        let subTasks = TaskSequence(Guid.NewGuid(), taskSequence, ItemOrdering.Sequential)
        events.Add(TaskEvent.ForEachTask(subTasks))
        executionStack.Push(Data data)
        executionStack.Push(Tasks subTasks)

    let processResponse self (taskInfo: TaskItem) response =
        if retainResponses then
            taskResponses.Value.Add(response)
        match response with
        | TaskResponse.SetArgument _ -> addArgumentEvent self response
        
        // resolve requests for common arguments immediately (other requests get passed to the UI)
        | TaskResponse.RequestArgument arg when knownArguments.IsKnownArgument(arg) ->
            events.Add(knownArguments.GetKnownArgument(arg))
            callTaskEvent.Trigger(self, taskInfo)
        | TaskResponse.TaskSequence taskSequence ->
            addTaskSequenceEvent taskSequence ItemOrdering.Sequential
            newUIResponseEvent.Trigger(self, response)
        | TaskResponse.TaskContinue delayMilliseconds ->
            Async.AwaitTask (Task.Delay(delayMilliseconds)) |> Async.RunSynchronously
            callTaskEvent.Trigger(self, taskInfo)
        | TaskResponse.TaskComplete _ ->
            events.Add(TaskEvent.FunctionCompleted)
            newUIResponseEvent.Trigger(self, response)
            nextTask self
        | TaskResponse.BeginLoopSequence (consumable, taskSequence) -> addDataSequenceEvent consumable taskSequence
        | TaskResponse.TaskSaveEvents filter ->
            // the event handler should asynchronously invoke a function to save the supplied event stack arguments
            // (note that by the time this function is invoked, the events stack may be in the process of being modified via new incoming responses
            // therefore a copy is passed to iterate over)
            let eventsCopy =
                match filter with
                | SaveEventsFilter.AllEvents -> events.ToArray()
                | SaveEventsFilter.ArgumentsOnly ->
                    events
                    |> Seq.filter (fun evt ->
                        match evt with
                        | TaskEvent.SetArgument _ -> true
                        | _ -> false
                    )
                    |> Seq.toArray
            saveEventsEvent.Trigger(self, eventsCopy)
        | TaskResponse.TaskConvertToJson data ->
            convertToJsonEvent.Trigger(self, data)
            callTaskEvent.Trigger(self, taskInfo)
        | TaskResponse.TaskConvertToBinary document ->
            convertToBinaryEvent.Trigger(self, document)
            callTaskEvent.Trigger(self, taskInfo)
        | TaskResponse.Notification notification ->
            match notification with
            | :? ApplicationErrorInfo as error ->
                events.Add(TaskEvent.TaskError taskInfo)
                errorState <- true   // stop further processing
            | _ -> ()
            newUIResponseEvent.Trigger(self, response)
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
        if errorState then
            let reportState = async {
                newUIResponseEvent.Trigger(this, TaskResponse.TaskInfo "The agent encountered an error. Start a new task to continue.")
            }
            Async.StartImmediateAsTask reportState
        else
            events.Add(TaskEvent.InvokingFunction)
            uiResponsePending <- false

            // collect responses from just the current call to the task function
            if retainResponses then taskResponses.Value.Clear()

            // The TaskItem parameter is to allow the current task to be queued for recall when specified by the task function
            runTask this taskInfo responses

    /// Start the task at the top of the stack
    member this.StartNextTask () =
        nextTask this

    /// Add a sequence of (independant or dependant) tasks to the execution stack
    member this.PushTasks taskSequence ordering =
        addTaskSequenceEvent taskSequence ordering

    /// A new UI selection has been made
    member this.NewSelection response =
        events.Add(TaskEvent.ClearArguments)
        events.Add(TaskEvent.SelectArgument)
        events.Add(TaskEvent.SetArgument(response))

    member this.IsAwaitingResponse with get () =
        uiResponsePending

    member this.AddArgument response =
        events.Add(TaskEvent.SetArgument(response))

    member this.AddEvents evts =
        events.AddRange(evts)

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

    /// Replace the events stack and continue execution with the new stack
    member this.ContinueWith loggedEvents =
        continueWith this loggedEvents

    member this.SerializeUnloggedEventsAsBytes () =
        let unloggedSerializedData = CloudWeaver.Serialization.SerializeEventsAsBytes events loggedCount
        loggedCount <- events.Count
        unloggedSerializedData

    member this.LastCallResponseList () = if retainResponses then taskResponses.Value else new List<TaskResponse>()

    [<CLIEvent>]
    member this.CallTask:IEvent<EventHandler<TaskItem>, TaskItem> = callTaskEvent.Publish

    [<CLIEvent>]
    member this.NewUIResponse:IEvent<EventHandler<TaskResponse>, TaskResponse> = newUIResponseEvent.Publish

    [<CLIEvent>]
    member this.SaveEvents:IEvent<EventHandler<TaskEvent[]>, TaskEvent[]> = saveEventsEvent.Publish

    [<CLIEvent>]
    member this.ConvertToJson:IEvent<EventHandler<byte[]>, byte[]> = convertToJsonEvent.Publish

    [<CLIEvent>]
    member this.ConvertToBinary:IEvent<EventHandler<JsonDocument>, JsonDocument> = convertToBinaryEvent.Publish

    [<CLIEvent>]
    member this.Error:IEvent<EventHandler<ApplicationErrorInfo>, ApplicationErrorInfo> = errorEvent.Publish
