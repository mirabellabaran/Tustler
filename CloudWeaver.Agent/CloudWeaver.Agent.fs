namespace CloudWeaver.Agent

open System
open System.Collections.Generic
open TustlerServicesLib
open System.Collections.Concurrent
open System.Threading.Tasks
open CloudWeaver.Types

type public Agent(knownArguments:KnownArgumentsCollection) =

    let standardVariables = StandardVariables()
    let callTaskEvent = new Event<EventHandler<_>, _>()
    let recallTaskEvent = new Event<EventHandler<_>, _>()
    let newUIResponseEvent = new Event<EventHandler<_>, _>()
    let saveArgumentsEvent = new Event<EventHandler<_>, _>()
    let errorEvent = new Event<EventHandler<_>, _>()

    do
        knownArguments.AddModule(standardVariables)

    // the event stack: ground truth for the events generated in a given session (start task to TaskResponse.TaskComplete)
    let events = new List<TaskEvent>()

    /// Get the last ForEach RetainingStack on the event stack (if there is one)
    let getCurrentLoopStack () =

        // ... first get the last ForEach event
        let lastForeach =
            events
            |> Seq.tryFindBack (fun evt ->
                match evt with
                | TaskEvent.ForEach _ -> true
                | _ -> false
            )

        // ... and retrieve the internal RetainingStack
        match lastForeach with
        | Some(TaskEvent.ForEach items) -> Some(items)
        | _ -> None

    let restartCurrentTask (self:Agent) =
        // find the last SubTask event
        let taskEvent =
            events
            |> Seq.tryFindBack (fun evt ->
                match evt with
                | TaskEvent.Task _ -> true
                | _ -> false
            )

        // and set the task name
        let task =
            match taskEvent with
            | Some(TaskEvent.Task task) -> task
            | _ -> invalidOp "Expecting a sub-task event in the events list"

        callTaskEvent.Trigger(self, task)    // receiver should set the TaskName and call RunTask

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
        let stack = getCurrentLoopStack ()
        if stack.IsSome && stack.Value.Count > 0 then
            startNewTask self stack.Value

    let addArgumentEvent (self:Agent) response =
        events.Add(TaskEvent.SetArgument(response))
        newUIResponseEvent.Trigger(self, response)

    let addSubTaskEvent taskSequence =
        let subTasks = new RetainingStack<TaskItem>(taskSequence, RetainingStack<TaskItem>.ItemOrdering.Sequential)
        events.Add(TaskEvent.ForEach(subTasks))

    let processResponse self response =
        match response with
        | TaskResponse.SetArgument _ -> addArgumentEvent self response
        | TaskResponse.SetBoundaryArgument _ -> addArgumentEvent self response
        
        // resolve requests for common arguments immediately (other requests get passed to the UI)
        | TaskResponse.RequestArgument arg when knownArguments.IsKnownArgument(arg) ->
            events.Add(knownArguments.GetKnownArgument(arg))
            recallTaskEvent.Trigger(self, EventArgs())    // receiver should set the TaskName and call RunTask

        | TaskResponse.TaskSelect _ ->
            events.Add(TaskEvent.SelectArgument)
            newUIResponseEvent.Trigger(self, response)
        | TaskResponse.TaskSequence taskSequence -> addSubTaskEvent taskSequence
        | TaskResponse.TaskContinue delayMilliseconds ->
            Async.AwaitTask (Task.Delay(delayMilliseconds)) |> Async.RunSynchronously
            restartCurrentTask self
        | TaskResponse.TaskComplete _ ->
            newUIResponseEvent.Trigger(self, response)
            nextTask self
        | TaskResponse.TaskArgumentSave ->
            // the event handler should asynchronously invoke a function to save the supplied event stack arguments
            // (note that by the time this function is invoked, the events stack may be in the process of being modified via new incoming responses
            // therefore a copy is passed to iterate over)
            let eventsCopy = events.ToArray()
            saveArgumentsEvent.Trigger(self, eventsCopy)

        | _ -> newUIResponseEvent.Trigger(self, response)    // receiver would typically call Dispatcher.InvokeAsync to invoke a function to add the response to the user interface

    let processor self responses =
        async {
            try
                responses
                |> Seq.iter (fun response -> processResponse self response)
            with
                | :? AggregateException as ex ->
                    let errorInfo = NotificationsList.CreateErrorNotification ("TaskQueue: queueWriter", ex.InnerException.Message, ex.InnerException)
                    errorEvent.Trigger(self, errorInfo)
        }

    let runTask self responses =
        let writer = processor self responses
        Async.Start writer

    member this.PrepareFunctionArguments (args: InfiniteList<MaybeResponse>) =
        // replay the observed events, adding the arguments that have been set
        events
        |> Seq.iter (fun evt ->
            match evt with
            | TaskEvent.SetArgument response -> args.Add(MaybeResponse.Just(response))
            | TaskEvent.ClearArguments -> args.Clear()
            | _ -> ()
        )

    member this.RunTask (responses: seq<TaskResponse>) =
        events.Add(TaskEvent.InvokingFunction)
        runTask this responses

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

    member this.HasFunctionCompleted () =
        match (Seq.last events) with
        | TaskEvent.FunctionCompleted -> true
        | _ -> false

    [<CLIEvent>]
    member this.CallTask:IEvent<EventHandler<TaskItem>, TaskItem> = callTaskEvent.Publish

    [<CLIEvent>]
    member this.RecallTask:IEvent<EventHandler<EventArgs>, EventArgs> = recallTaskEvent.Publish

    [<CLIEvent>]
    member this.NewUIResponse:IEvent<EventHandler<TaskResponse>, TaskResponse> = newUIResponseEvent.Publish

    [<CLIEvent>]
    member this.SaveArguments:IEvent<EventHandler<TaskEvent[]>, TaskEvent[]> = saveArgumentsEvent.Publish

    [<CLIEvent>]
    member this.Error:IEvent<EventHandler<ApplicationErrorInfo>, ApplicationErrorInfo> = errorEvent.Publish
