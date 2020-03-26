namespace TustlerFSharpPlatform

open TustlerServicesLib
open AWSInterface
open TustlerModels

type TaskResponse =
    | TaskNotification of Notification
    | TaskBucket of Bucket
    | TaskBucketItem of BucketItem

module public Tasks =            

    let private getNotificationResponse (notifications: NotificationsList) =
        Seq.map (fun note -> TaskNotification note) notifications.Notifications

    let S3FetchItems () =
        let notifications = NotificationsList()

        seq {
            let buckets = S3.getBuckets notifications |> Async.RunSynchronously
            yield! getNotificationResponse notifications

            if buckets.Count > 0 then
                let bucket = Seq.head buckets
                yield TaskBucket bucket

                let items = S3.getBucketItems notifications bucket.Name |> Async.RunSynchronously
                yield! getNotificationResponse notifications
                yield! Seq.map (fun item -> TaskBucketItem item) items
        }