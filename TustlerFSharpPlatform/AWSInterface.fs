namespace TustlerFSharpPlatform

open TustlerModels
open TustlerServicesLib

module public AWSInterface =

    let downCastNotification (note:Notification) : string =
        match note with
        | :? ApplicationErrorInfo as error -> sprintf "%s: %s: %s" error.Context error.Message error.Exception.InnerException.Message
        | :? ApplicationMessageInfo as message -> sprintf "%s: %s" message.Message message.Detail
        | _ -> "Unknown type"

    module S3 =

        let getBuckets notifications =
            async {
                let model = BucketViewModel()
                do! model.Refresh (true, notifications) |> Async.AwaitTask
                return model.Buckets
            }

        let getBucketItems notifications bucketName =
            async {
                let model = BucketItemViewModel()
                do! model.Refresh(notifications, bucketName) |> Async.AwaitTask
                return model.BucketItems
            }
