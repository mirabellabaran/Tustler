//namespace TustlerFSharpPlatform

//open System
//open System.Collections.Generic
//open TustlerServicesLib
//open System.Collections.Concurrent
//open System.Threading.Tasks

//module Queue =

//    type MailBoxQueueMsg =
//        | Exit
//        | Init of ICollection<TaskResponse>
//        | Store of TaskResponse

//    type MailBoxQueue() =
//        let uiResponseEvent = new Event<EventHandler<_>, _>()

//        let inner =
//            MailboxProcessor.Start(fun inbox ->
//                let rec loop (collection: ICollection<TaskResponse>) =
//                    async {
//                        let! msg = inbox.Receive()
//                        match msg with
//                        | Exit -> return ()
//                        | Init collection -> return! loop(collection)
//                        | Store response ->
//                            collection.Add(response)
//                            return! loop(collection)
//                    }
//                loop (null)
//            )
            
//        member this.Initialize(collection) = inner.Post(Init collection)
//        member this.Add(x) = inner.Post(Store x); uiResponseEvent.Trigger(this, x)
//        member this.Exit() = inner.Post(Exit)

//        [<CLIEvent>]
//        member this.UIResponseEvent:IEvent<EventHandler<TaskResponse>, TaskResponse> = uiResponseEvent.Publish

//        member this.AddUIResponseHandler (handler: EventHandler<TaskResponse>) =
//            this.UIResponseEvent.AddHandler handler

//type public Agent(awsInterface, notificationsList) =

//    let taskNameChangedEvent = new Event<EventHandler<_>, _>()
//    let newUIResponseEvent = new Event<EventHandler<_>, _>()
//    let saveArgumentsEvent = new Event<EventHandler<_>, _>()
//    let errorEvent = new Event<EventHandler<_>, _>()

//    // the event stack: ground truth for the events generated in a given session (start task to TaskResponse.TaskComplete)
//    let events = new List<TaskEvent>()
//    do
//        // add common arguments to the event stack
//        events.Add(TaskEvent.SetArgument(TaskResponse.SetNotificationsList(notificationsList)));
//        events.Add(TaskEvent.SetArgument(TaskResponse.SetAWSInterface(awsInterface)));

//    //let queue = Queue.MailBoxQueue()
//    //do queue.Initialize(collection)

//    /// Get the last ForEach RetainingStack on the event stack (if there is one)
//    let getCurrentLoopStack () =

//        // ... first get the last ForEach event
//        let lastForeach =
//            events
//            |> Seq.tryFindBack (fun evt ->
//                match evt with
//                | TaskEvent.ForEach _ -> true
//                | _ -> false
//            )

//        // ... and retrieve the internal RetainingStack
//        match lastForeach with
//        | Some(TaskEvent.ForEach items) -> Some(items)
//        | _ -> None

//    let restartCurrentTask (self:Agent) =
//        // find the last SubTask event
//        let subTaskEvent =
//            events
//            |> Seq.tryFindBack (fun evt ->
//                match evt with
//                | TaskEvent.SubTask _ -> true
//                | _ -> false
//            )

//        // and set the task name
//        let taskName =
//            match subTaskEvent with
//            | Some(TaskEvent.SubTask name) -> name
//            | _ -> invalidOp "Expecting a sub-task event in the events list"

//        taskNameChangedEvent.Trigger(self, taskName)    // receiver should set the TaskName and call RunTask

//    let startNewTask (self:Agent) (stack:RetainingStack<SubTaskItem>) =
//        let independantTasks = stack.Ordering = RetainingStack<SubTaskItem>.ItemOrdering.Independant
//        if independantTasks then
//            // independant tasks cannot share arguments; clear all arguments and add back the common arguments
//            events.Add(TaskEvent.ClearArguments)
//            events.Add(TaskEvent.SetArgument(TaskResponse.SetNotificationsList(notificationsList)))
//            events.Add(TaskEvent.SetArgument(TaskResponse.SetAWSInterface(awsInterface)))

//        let nextTask = stack.Pop();
//        events.Add(TaskEvent.SubTask(nextTask.TaskName))

//        if independantTasks then
//            events.Add(TaskEvent.SetArgument(TaskResponse.SetTaskItem(nextTask)))
        
//        taskNameChangedEvent.Trigger(self, nextTask.TaskName)

//    let nextTask (self:Agent) =
//        events.Add(TaskEvent.FunctionCompleted)
//        let stack = getCurrentLoopStack ()
//        if stack.IsSome && stack.Value.Count > 0 then
//            startNewTask self stack.Value

//    let addArgumentEvent (self:Agent) response =
//        events.Add(TaskEvent.SetArgument(response))
//        newUIResponseEvent.Trigger(self, response)

//    let addSubTaskEvent taskSequence =
//        let subTasks = new RetainingStack<SubTaskItem>(taskSequence, RetainingStack<SubTaskItem>.ItemOrdering.Sequential)
//        events.Add(TaskEvent.ForEach(subTasks))

//    let processResponse self response =
//        match response with
//        | TaskResponse.SetBucket _ -> addArgumentEvent self response
//        | TaskResponse.SetBucketsModel _ -> addArgumentEvent self response
//        | TaskResponse.SetBucketItemsModel _ -> addArgumentEvent self response
//        | TaskResponse.SetFileUpload _ -> addArgumentEvent self response
//        | TaskResponse.SetTranscriptionJobName _ -> addArgumentEvent self response
//        | TaskResponse.SetTranscriptionJobsModel _ -> addArgumentEvent self response

//        | TaskResponse.TaskSelect _ ->
//            events.Add(TaskEvent.SelectArgument)
//            newUIResponseEvent.Trigger(self, response)
//        | TaskResponse.TaskSequence taskSequence -> addSubTaskEvent taskSequence
//        | TaskResponse.TaskContinue delayMilliseconds ->
//            Async.AwaitTask (Task.Delay(delayMilliseconds)) |> Async.RunSynchronously
//            restartCurrentTask self
//        | TaskResponse.TaskComplete _ ->
//            newUIResponseEvent.Trigger(self, response)
//            nextTask self
//        | TaskResponse.TaskArgumentSave ->
//            // receiver should call Dispatcher.InvokeAsync to invoke a function to save the supplied event stack arguments
//            // by the time this function is invoked, the events stack may be in the process of being modified via new incoming responses
//            // therefore pass a copy to iterate over
//            let eventsCopy = events.ToArray()
//            saveArgumentsEvent.Trigger(self, eventsCopy)

//        | _ -> newUIResponseEvent.Trigger(self, response)    // receiver would typically call Dispatcher.InvokeAsync to invoke a function to add the response to the user interface

//    let queueWriter self responses =
//        async {
//            try
//                responses
//                |> Seq.iter (fun response -> processResponse self response)
//            with
//                | :? AggregateException as ex ->
//                    let errorInfo = NotificationsList.CreateErrorNotification ("TaskQueue: queueWriter", ex.InnerException.Message, ex.InnerException)
//                    errorEvent.Trigger(self, errorInfo)
//        }

//    let runTask self responses =
//        let writer = queueWriter self responses
//        Async.Start writer

//    member this.PrepareFunctionArguments (args: InfiniteList<MaybeResponse>) =
//        // replay the observed events, adding the arguments that have been set
//        events
//        |> Seq.iter (fun evt ->
//            match evt with
//            | TaskEvent.SetArgument response -> args.Add(MaybeResponse.Just(response))
//            | TaskEvent.ClearArguments -> args.Clear()
//            | _ -> ()
//        )

//    member this.RunTask (responses: seq<TaskResponse>) =
//        events.Add(TaskEvent.InvokingFunction)
//        runTask this responses

//    //member this.RestartCurrentTask () =
//    //    restartCurrentTask this

//    /// start the task at the top of the stack
//    member this.StartNewTask stack =
//        startNewTask this stack

//    /// a new UI selection has been made
//    member this.NewSelection response =
//        events.Add(TaskEvent.ClearArguments)
//        events.Add(TaskEvent.SetArgument(TaskResponse.SetNotificationsList(notificationsList)))
//        events.Add(TaskEvent.SetArgument(TaskResponse.SetAWSInterface(awsInterface)))
//        events.Add(TaskEvent.SelectArgument)
//        events.Add(TaskEvent.SetArgument(response))

//    member this.AddArgument response =
//        events.Add(TaskEvent.SetArgument(response))

//    member this.AddEvent evt =
//        events.Add(evt)

//    member this.ClearArguments () =
//        events.Add(TaskEvent.ClearArguments)

//    member this.HasFunctionCompleted () =
//        match (Seq.last events) with
//        | TaskEvent.FunctionCompleted -> true
//        | _ -> false

//    [<CLIEvent>]
//    member this.TaskNameChanged:IEvent<EventHandler<string>, string> = taskNameChangedEvent.Publish

//    [<CLIEvent>]
//    member this.NewUIResponse:IEvent<EventHandler<TaskResponse>, TaskResponse> = newUIResponseEvent.Publish

//    [<CLIEvent>]
//    member this.SaveArguments:IEvent<EventHandler<TaskEvent[]>, TaskEvent[]> = saveArgumentsEvent.Publish

//    [<CLIEvent>]
//    member this.Error:IEvent<EventHandler<ApplicationErrorInfo>, ApplicationErrorInfo> = errorEvent.Publish

////module Test =
////    open AWSInterface

////    [<EntryPoint>]
////    let main argv =
////        let collectionChangedHandler (e: NotifyCollectionChangedEventArgs) =
////            Seq.cast<TaskResponse> e.NewItems
////            |> Seq.iter (fun response ->
////                match response with
////                | TaskNotification note ->
////                    match note with
////                    | :? ApplicationErrorInfo as error -> printfn "%s: %s" error.Context error.Message
////                    | :? ApplicationMessageInfo as msg -> printfn "%s: %s" msg.Message msg.Detail
////                    | _ -> printfn "Unmatched notification"

////                | TaskBucket bucket -> printfn "%s" bucket.Name
////                | TaskBucketItem item -> printfn "%s" item.Key
////            )

////        let task = Tasks.S3FetchItems ()
////        let collection = new ObservableCollection<TaskResponse>()
////        Event.add (collectionChangedHandler) collection.CollectionChanged

////        TaskQueue.Run (task, collection) |> Async.AwaitTask |> Async.RunSynchronously

////        System.Threading.Tasks.Task.Delay 5000 |> Async.AwaitTask |> Async.RunSynchronously

////        //let notifications = NotificationsList()

////        //let buckets = S3.getBuckets notifications |> Async.RunSynchronously
////        //let bucket = if buckets.Count > 0 then Some(Seq.head buckets) else None
////        //if bucket.IsSome then
////        //    let items = S3.getBucketItems notifications bucket.Value.Name |> Async.RunSynchronously
////        //    printfn "%s" bucket.Value.Name
////        //    if items.Count > 0 then
////        //        items
////        //        |> Seq.iteri (fun i item -> printfn "%s (%s : %s) [%d]" item.Key item.Extension item.MimeType item.Size)

////        //if notifications.Notifications.Count > 0 then
////        //    notifications.Notifications
////        //    |> Seq.map (fun note -> downCastNotification note)
////        //    |> Seq.iteri (fun i desc -> printfn "%d: %s" i desc)

////        0 // Return an integer exit code
