﻿namespace CloudWeaver

open System
open System.Collections.Generic
open TustlerServicesLib
open System.Threading.Tasks
open CloudWeaver.Types
open System.IO
open System.Text.Json
open System.Text
open CloudWeaver.Foundation.Types

type ExecutionStackFrameType =
    | Data of IConsumable
    | Tasks of IConsumableTaskSequence

type public Agent(knownArguments:KnownArgumentsCollection, taskFunctionResolver: TaskFunctionResolver, taskLogger: TaskLogger, retainResponses: bool) =

    let mutable isEnabled = true
    let mutable rootTaskSpecifier = None
    let mutable currentTaskSpecifier = None
    let mutable loggedCount = 0
    let mutable uiResponsePending = false
    let mutable errorState = false

    let standardVariables = StandardVariables()

    //let callTaskEvent = new Event<EventHandler<_>, _>()
    let taskCompleteEvent = new Event<EventHandler<_>, _>()     // called when the root task and any associated tasks have completed
    let newUIResponseEvent = new Event<EventHandler<_>, _>()
    let saveEventsEvent = new Event<EventHandler<_>, _>()               // save some or all events as a JSON document
    let convertToJsonEvent = new Event<EventHandler<_>, _>()    // convert events in binary log format to JSON document format
    let convertToBinaryEvent = new Event<EventHandler<_>, _>()  // convert events in JSON document format to binary log format

    let errorEvent = new Event<EventHandler<_>, _>()

    // Note that each call to the agent must run to completion for the system to work correctly (ie the agent must run just one task function at a time)
    // Therefore enqueue the next task specifier and run the task later
    let taskQueue = new Queue<TaskFunctionSpecifier>()
    let taskFunctionLookup = taskFunctionResolver.GetTaskFunctionDictionary()
    let notificationsList = new NotificationsList()

    do
        knownArguments.AddModule(new StandardKnownArguments(notificationsList));
        knownArguments.AddModule(standardVariables)

    // the event stack: ground truth for the events generated in a given session (from start of task to TaskResponse.TaskComplete)
    let events = new List<TaskEvent>()

    let executionStack = new Stack<ExecutionStackFrameType>()

    // the responses generated from the last task function call (useful for testing purposes)
    let taskResponses = if retainResponses then Some(new List<TaskResponse>()) else None

    let startNewTask (self:Agent) (stack:IConsumableTaskSequence) =
        
        stack.ConsumeTask();
        events.Add(TaskEvent.ConsumedTask stack.Identifier)
        
        if stack.Current.IsNone then
            invalidOp "startNewTask: no current task"
        else
            let nextTask = stack.Current.Value

            // check if task is independant
            let isIndependantTask = stack.Ordering = ItemOrdering.Independant
            if isIndependantTask then
                // independant tasks cannot share arguments; clear all arguments
                events.Add(TaskEvent.ClearArguments)

            events.Add(TaskEvent.Task(nextTask))

            // update task item variable
            standardVariables.SetValue(StandardRequest.RequestTaskItem, nextTask)
        
            //callTaskEvent.Trigger(self, nextTask)
            taskQueue.Enqueue(taskFunctionLookup.[nextTask.FullPath])   // will throw if task path is unknown

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
                            consumable.Consume()
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

    let getCurrentTask () =
        match executionStack.TryPeek() with
        | true, Tasks taskSequence ->
            match taskSequence.Current with
            | Some(task) -> task
            | None -> invalidOp "getCurrentTask: current task not set"
        | true, Data _  -> invalidOp "getCurrentTask: found data frame at top of execution stack"
        | _ -> invalidOp "getCurrentTask: execution stack is empty"

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
                    frame.Consume()
                | TaskEvent.ConsumedTask identifier ->
                    let frame = findTaskFrame identifier
                    frame.ConsumeTask()
                | _ -> ()
            )

        // find the current task and call it
        let callLastTask () =
            let currentTask = getCurrentTask ()
            standardVariables.SetValue(StandardRequest.RequestTaskItem, currentTask)   // update task item variable
            //callTaskEvent.Trigger(self, currentTask)
            taskQueue.Enqueue(taskFunctionLookup.[currentTask.FullPath])            // will throw if task path is unknown
            
        events.Clear()
        executionStack.Clear()
        events.AddRange(loggedEvents)
        //loggedCount <- events.Count   // new log file; start from the beginning

        if events.Count > 0 then
            regenerateExecutionStack()

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
                //callTaskEvent.Trigger(self, taskInfo)
                taskQueue.Enqueue(taskFunctionLookup.[taskInfo.FullPath])       // will throw if task path is unknown
            | TaskEvent.FunctionCompleted ->
                if executionStack.Count > 0 then
                    nextTask self
                    if executionStack.Count = 0 then    // popped last item
                        let response = TaskResponse.TaskInfo "Log Replay: Nothing to show as the previous logged run completed successfully"
                        newUIResponseEvent.Trigger(self, response)
                else
                    let response = TaskResponse.TaskInfo "Log Replay: No frames on the execution stack"
                    newUIResponseEvent.Trigger(self, response)
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

    let enqueueCurrentTask () =
        let currentTask = getCurrentTask ()
        taskQueue.Enqueue(taskFunctionLookup.[currentTask.FullPath])        // will throw if task path is unknown

    let processResponse self response =
        if taskResponses.IsSome then
            taskResponses.Value.Add(response)

        match response with
        | TaskResponse.SetArgument _ -> addArgumentEvent self response
        
        // resolve requests for common arguments immediately (other requests get passed to the UI)
        | TaskResponse.RequestArgument arg when knownArguments.IsKnownArgument(arg) ->
            events.Add(knownArguments.GetKnownArgument(arg))
            //callTaskEvent.Trigger(self, getCurrentTask ())
            enqueueCurrentTask ()
        | TaskResponse.TaskSequence taskSequence ->
            addTaskSequenceEvent taskSequence ItemOrdering.Sequential
            newUIResponseEvent.Trigger(self, response)
        | TaskResponse.TaskContinue delayMilliseconds ->
            Async.AwaitTask (Task.Delay(delayMilliseconds)) |> Async.RunSynchronously
            //callTaskEvent.Trigger(self, getCurrentTask ())
            enqueueCurrentTask ()
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
            //callTaskEvent.Trigger(self, getCurrentTask ())
            enqueueCurrentTask ()
        | TaskResponse.TaskConvertToBinary document ->
            convertToBinaryEvent.Trigger(self, document)
            //callTaskEvent.Trigger(self, getCurrentTask ())
            enqueueCurrentTask ()
        | TaskResponse.Notification notification ->
            match notification with
            | :? ApplicationErrorInfo as error ->
                events.Add(TaskEvent.TaskError (getCurrentTask ()))
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

    /// log any unlogged events
    let logEvents () =
        if taskLogger.IsLoggingEnabled then
            let unloggedSerializedData = CloudWeaver.Serialization.SerializeEventsAsBytes events loggedCount
            loggedCount <- events.Count
            let data = EventLoggingUtilities.BlockArrayToByteArray(unloggedSerializedData)
            taskLogger.AddToLog(data);

    let checkQueue () =
        match taskQueue.TryDequeue() with
        | true, taskFunctionSpecifier -> Some(taskFunctionSpecifier)
        | _ -> None

    let checkComplete () =
        if executionStack.Count = 0 then    // && not uiResponsePending
            taskLogger.StopLogging ()                

    let processor self responses =
        async {
            try
                responses
                |> Seq.iter (fun response -> processResponse self response)

                logEvents ()
                checkComplete ()
            with
                | :? AggregateException as ex ->
                    let errorInfo = NotificationsList.CreateErrorNotification ("TaskQueue: queueWriter", ex.InnerException.Message, ex.InnerException)
                    errorEvent.Trigger(self, errorInfo)
        }

    //let runTask self responses =
    //    let writer = processor self responses
    //    Async.StartAsTask writer

    let rec run self (taskSpecifier) =
        if isEnabled then
            if errorState then
                newUIResponseEvent.Trigger(self, TaskResponse.TaskInfo "The agent encountered an error. Start a new task to continue.")
            else
                currentTaskSpecifier <- Some(taskSpecifier)
                uiResponsePending <- false

                let args = new InfiniteList<MaybeResponse>(MaybeResponse.Nothing)

                // replay the observed events, adding the arguments that have been set
                events
                |> Seq.iter (fun evt ->
                    match evt with
                    | TaskEvent.SetArgument response -> args.Add(MaybeResponse.Just(response))
                    | TaskEvent.ClearArguments -> args.Clear()
                    | _ -> ()
                )

                let argMap: Map<IRequestIntraModule, IShareIntraModule> = ArgumentResolver.integrateUIRequestArguments args

                notificationsList.Clear();      // cleared for each function invocation

                events.Add(TaskEvent.InvokingFunction)

                let taskFunction = taskFunctionResolver.CreateDelegate(taskSpecifier)
                let responseStream = taskFunction.Invoke(TaskFunctionQueryMode.Invoke, argMap)

                //// collect responses from just the current call to the task function
                //if retainResponses then taskResponses.Value.Clear()

                processor self responseStream |> Async.StartAsTask |> Async.AwaitTask |> Async.RunSynchronously

                // once the previous call has been processed start the next task (if any)
                let next = checkQueue ()
                if next.IsSome then
                    run self next.Value    
                else
                    taskCompleteEvent.Trigger(self, EventArgs())

    let pushTask self (taskFunctionSpecifier: TaskFunctionSpecifier) =
        let taskItem = new TaskItem(taskFunctionSpecifier.ModuleName, taskFunctionSpecifier.TaskName, System.String.Empty);
        let singleTaskSequence = TaskSequence(Guid.NewGuid(), [| taskItem |], ItemOrdering.Sequential)
        events.Add(TaskEvent.ForEachTask(singleTaskSequence))
        executionStack.Push(Tasks singleTaskSequence)
        nextTask self

    //member this.PrepareFunctionArguments (args: InfiniteList<MaybeResponse>) =
    //    // replay the observed events, adding the arguments that have been set
    //    events
    //    |> Seq.iter (fun evt ->
    //        match evt with
    //        | TaskEvent.SetArgument response -> args.Add(MaybeResponse.Just(response))
    //        | TaskEvent.ClearArguments -> args.Clear()
    //        | _ -> ()
    //    )

    ///// Process the responses from the current task function
    //member this.RunTask (responses: seq<TaskResponse>) =
    //    if errorState then
    //        let reportState = async {
    //            newUIResponseEvent.Trigger(this, TaskResponse.TaskInfo "The agent encountered an error. Start a new task to continue.")
    //        }
    //        Async.StartImmediateAsTask reportState
    //    else
    //        events.Add(TaskEvent.InvokingFunction)
    //        uiResponsePending <- false

    //        // collect responses from just the current call to the task function
    //        if retainResponses then taskResponses.Value.Clear()

    //        // The TaskItem parameter is to allow the current task to be queued for recall when specified by the task function
    //        runTask this responses

    /// Start and stop task function execution
    member this.Enabled with get() = isEnabled and set(value) = isEnabled <- value

    /// Return true if there is a task in the task queue
    member this.TaskAvailable with get() = taskQueue.Count > 0

    /// If the retainResponses flag has been set then clear the response list
    member this.ClearResponses () = if taskResponses.IsSome then taskResponses.Value.Clear()

    ///// Run the specified root task function (and any other task functions that serve this root task)
    //member this.RunTask taskSpecifier =
    //    async {
    //        rootTaskSpecifier <- Some(taskSpecifier)
    //        taskLogger.StartLogging (taskSpecifier) |> ignore
    //        run this taskSpecifier
    //    }
    //    |> Async.StartAsTask

    /// Continue running the current task
    member this.RunCurrent () =
        async {
            if currentTaskSpecifier.IsSome then
                run this currentTaskSpecifier.Value
        }
        |> Async.StartAsTask

    /// Run the next task in the task queue (if any)
    member this.RunNext () =
        async {
            let next = checkQueue ()
            if next.IsSome then
                run this next.Value                    
        }
        |> Async.StartAsTask

    /// add the specified function to the task function resolver
    member this.AddFunction(methodinfo: System.Reflection.MethodInfo) =
        taskFunctionResolver.AddFunction(methodinfo)
        let ri = taskFunctionResolver.FindFunction(methodinfo)
        if ri.IsSome then
            let specifier = ri.Value.TaskFunctionSpecifier
            taskFunctionLookup.Add(specifier.TaskFullPath, specifier)

    /// Write any unlogged events and close the log file (typically called when stopping the task)
    member this.CloseLog () =
        logEvents ()
        taskLogger.StopLogging ()

    /// Returns the full path of the log file (this will only exist if IsLoggingEnabled is true and StartLogging() has been called)
    member this.LogFilePath with get() = taskLogger.LogFilePath

    /// Returns true if the root task has the EnableLogging attribute set
    member this.IsLoggingEnabled with get() = taskLogger.IsLoggingEnabled

    /// Returns the task argument specified in RunTask (taskSpecifier)
    member this.RootTask with get() = if rootTaskSpecifier.IsSome then rootTaskSpecifier.Value else null

    member this.TaskFunctions with get() : IEnumerable<TaskFunctionSpecifier> = Seq.cast<TaskFunctionSpecifier> taskFunctionLookup.Values

    ///// Returns the next task specifier in the queue or null
    //member this.NextTask () =
    //    match taskQueue.TryDequeue() with
    //    | true, taskFunctionSpecifier -> taskFunctionSpecifier
    //    | _ -> null

    /// Return the path of all queued task functions (used for testing only)
    member this.QueuedTasks () : IEnumerable<string> = taskQueue |> Seq.map (fun spec -> spec.TaskFullPath)

    /// Get the current task from the exection stack
    member this.CurrentTask () = getCurrentTask ()

    /// Add a root task to the execution stack. A root task may be logged (if enabled) and may generate additional sub tasks.
    member this.PushRootTask (rootFolder: string) (taskFunctionSpecifier: TaskFunctionSpecifier) =
        rootTaskSpecifier <- Some(taskFunctionSpecifier)
        taskLogger.StartLogging(rootFolder, taskFunctionSpecifier) |> ignore
        pushTask this taskFunctionSpecifier

    /// Add a single-task task sequence to the execution stack
    member this.PushTask (taskFunctionSpecifier: TaskFunctionSpecifier) = pushTask this taskFunctionSpecifier

    /// Add a sequence of (independant or dependant) tasks to the execution stack
    member this.PushTasks taskSequence ordering =
        addTaskSequenceEvent taskSequence ordering
        nextTask this

    /// Insert a task into the IConsumableTaskSequence at the top of the stack
    member this.InsertTaskBeforeCurrent taskPath =
        let taskSpecifier = taskFunctionLookup.[taskPath]   // MG could throw
        let taskItem = TaskItem(taskSpecifier.ModuleName, taskSpecifier.TaskName, System.String.Empty)

        match executionStack.TryPeek() with
        | true, Tasks taskSequence ->
            executionStack.Pop() |> ignore
            let newSequence = taskSequence.InsertBeforeCurrent taskItem
            events.Add(TaskEvent.ForEachTask(newSequence))
            executionStack.Push(Tasks newSequence)
            nextTask this
        | _ -> ()

    /// A new UI selection has been made
    member this.NewSelection response =
        events.Add(TaskEvent.ClearArguments)
        events.Add(TaskEvent.SelectArgument)
        events.Add(TaskEvent.SetArgument(response))

    //member this.IsAwaitingResponse with get () =
    //    uiResponsePending

    member this.AddArgument(response) =
        events.Add(TaskEvent.SetArgument(response))

    member this.AddArgument(moduleName, propertyName, data: byte[]) =
        let jsonString = UTF8Encoding.UTF8.GetString(data)
        let resolveProperty = ModuleResolver.ModuleLookup(moduleName)
        let shareIntraModule = resolveProperty.Invoke(propertyName, jsonString)
        let response = TaskResponse.SetArgument shareIntraModule
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

    member this.LastCallResponseList () = if retainResponses then taskResponses.Value else new List<TaskResponse>()

    //[<CLIEvent>]
    //member this.CallTask:IEvent<EventHandler<TaskItem>, TaskItem> = callTaskEvent.Publish

    [<CLIEvent>]
    member this.TaskComplete:IEvent<EventHandler<EventArgs>, EventArgs> = taskCompleteEvent.Publish

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
