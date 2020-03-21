namespace TustlerFSharpPlatform

open TustlerModels
open TustlerServicesLib

module AWSInterface =

//let getBuckets =
//    async {
//        let! result =  TustlerAWSLib.S3.ListBuckets() |> Async.AwaitTask
//        return result
//    }

    let getBuckets =
        async {
            let notifications = NotificationsList()
            let model = BucketViewModel()
            model.Refresh (true, notifications) |> Async.AwaitTask |> ignore
            let buckets = model.Buckets
            let bucket = if buckets.Count > 0 then Some(Seq.head buckets) else None
            let note = if notifications.Notifications.Count > 0 then Some(downcast (Seq.head notifications.Notifications)) else None
            return (bucket, buckets.Count, note)
        }
        |> Async.RunSynchronously

    [<EntryPoint>]
    let main argv =
        let bucket, c, note = getBuckets
        printfn "%A %d %A" (if Option.isSome bucket then bucket.Value.Name else "Null return") c (if note.IsSome then note.Value else "No notifications")
        0 // Return an integer exit code