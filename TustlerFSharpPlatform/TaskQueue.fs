namespace TustlerFSharpPlatform

open System.Collections.Generic
open TustlerServicesLib
open AWSInterface
open System.Collections.ObjectModel
open System.Collections.Specialized

module public Queue =

    type MailBoxQueueMsg =
        | Exit
        | Init of ICollection<TaskResponse>
        | Store of TaskResponse
    
    type MailBoxQueue() =
        let inner =
            MailboxProcessor.Start(fun inbox ->
                let rec loop (collection: ICollection<TaskResponse>) =
                    async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Exit -> return ()
                        | Init collection -> return! loop(collection)
                        | Store response ->
                            collection.Add(response)
                            return! loop(collection)
                    }
                loop (null)
            )
            
        member this.Initialize(collection) = inner.Post(Init collection)
        member this.Add(x) = inner.Post(Store x)
        member this.Exit() = inner.Post(Exit)

module public TaskQueue =

    let private queueWriter (queue: Queue.MailBoxQueue) task =
        async {
            task
            |> Seq.iter (fun response -> queue.Add(response))
        }

    let Run (responses: seq<TaskResponse>, collection: ICollection<TaskResponse>) =
        let queue = Queue.MailBoxQueue()
        queue.Initialize(collection)
        let writer = queueWriter queue responses
        Async.StartAsTask writer
        //Async.RunSynchronously writer

module Test =
    open AWSInterface

    [<EntryPoint>]
    let main argv =
        let collectionChangedHandler (e: NotifyCollectionChangedEventArgs) =
            Seq.cast<TaskResponse> e.NewItems
            |> Seq.iter (fun response ->
                match response with
                | TaskNotification note ->
                    match note with
                    | :? ApplicationErrorInfo as error -> printfn "%s: %s" error.Context error.Message
                    | :? ApplicationMessageInfo as msg -> printfn "%s: %s" msg.Message msg.Detail
                    | _ -> printfn "Unmatched notification"

                | TaskBucket bucket -> printfn "%s" bucket.Name
                | TaskBucketItem item -> printfn "%s" item.Key
            )

        let task = Tasks.S3FetchItems ()
        let collection = new ObservableCollection<TaskResponse>()
        Event.add (collectionChangedHandler) collection.CollectionChanged

        TaskQueue.Run (task, collection) |> Async.AwaitTask |> Async.RunSynchronously

        System.Threading.Tasks.Task.Delay 5000 |> Async.AwaitTask |> Async.RunSynchronously

        //let notifications = NotificationsList()

        //let buckets = S3.getBuckets notifications |> Async.RunSynchronously
        //let bucket = if buckets.Count > 0 then Some(Seq.head buckets) else None
        //if bucket.IsSome then
        //    let items = S3.getBucketItems notifications bucket.Value.Name |> Async.RunSynchronously
        //    printfn "%s" bucket.Value.Name
        //    if items.Count > 0 then
        //        items
        //        |> Seq.iteri (fun i item -> printfn "%s (%s : %s) [%d]" item.Key item.Extension item.MimeType item.Size)

        //if notifications.Notifications.Count > 0 then
        //    notifications.Notifications
        //    |> Seq.map (fun note -> downCastNotification note)
        //    |> Seq.iteri (fun i desc -> printfn "%d: %s" i desc)

        0 // Return an integer exit code
