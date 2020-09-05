namespace CloudWeaver

open System
open System.Collections.Generic
open TustlerServicesLib
open System.Collections.Concurrent
open System.Threading.Tasks
open CloudWeaver.Types
open System.IO

type public Agent(knownArguments:KnownArgumentsCollection, retainResponses: bool) =

    let standardVariables = StandardVariables()
    let callTaskEvent = new Event<EventHandler<_>, _>()
    //let recallTaskEvent = new Event<EventHandler<_>, _>()
    let newUIResponseEvent = new Event<EventHandler<_>, _>()
    let saveArgumentsEvent = new Event<EventHandler<_>, _>()
    let errorEvent = new Event<EventHandler<_>, _>()

    do
        knownArguments.AddModule(standardVariables)

    // the event stack: ground truth for the events generated in a given session (start task to TaskResponse.TaskComplete)
    let events = new List<TaskEvent>()

    // the responses generated from the last task function call (useful for testing purposes)
    let taskResponses = if retainResponses then Some(new List<TaskResponse>()) else None

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
        | TaskResponse.SetBoundaryArgument _ -> addArgumentEvent self response
        
        // resolve requests for common arguments immediately (other requests get passed to the UI)
        | TaskResponse.RequestArgument arg when knownArguments.IsKnownArgument(arg) ->
            events.Add(knownArguments.GetKnownArgument(arg))
            callTaskEvent.Trigger(self, taskInfo)
            //recallTaskEvent.Trigger(self, EventArgs())    // receiver should set the TaskName and call RunTask
        | TaskResponse.TaskSelect _ ->
            events.Add(TaskEvent.SelectArgument)
            newUIResponseEvent.Trigger(self, response)
        | TaskResponse.TaskSequence taskSequence -> addTaskSequenceEvent taskSequence
        | TaskResponse.TaskContinue delayMilliseconds ->
            Async.AwaitTask (Task.Delay(delayMilliseconds)) |> Async.RunSynchronously
            callTaskEvent.Trigger(self, taskInfo)
            //recallTaskEvent.Trigger(self, EventArgs())
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

        | _ -> newUIResponseEvent.Trigger(self, response)    // receiver would typically call Dispatcher.InvokeAsync to invoke a function to add the response to the user interface

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
