namespace CloudWeaver.AWS

open System.Runtime.InteropServices
open TustlerServicesLib
open TustlerModels
open AWSInterface
open CloudWeaver.Foundation.Types

/// Tasks within a task (for S3FetchItems task)
module public MiniTasks =

    [<RequireQualifiedAccess>]
    type MiniTaskArgument =
        | Bool of bool
        | String of string
        | Int of int

    type MiniTaskMode =
        | Unknown
        | Delete
        | Download
    
    type MiniTaskContext =
        | BucketItemsModel of BucketItemViewModel

    type MiniTaskArguments () =
        let mutable mode = MiniTaskMode.Unknown
        let mutable arguments: MiniTaskArgument[] = Array.empty
    
        member this.Mode with get () = mode and set _mode = mode <- _mode
        member this.TaskArguments
            with get() = Seq.ofArray arguments
            and set args = arguments <- Seq.toArray args
    
    [<RequireQualifiedAccess>]
    type TaskUpdate =
        | DeleteBucketItem of NotificationsList * bool * string                 // returns success flag and the deleted key
        | DownloadBucketItem of NotificationsList * bool * string * string      // returns success flag, the key of the downloaded item and the download file path
    
    type TaskUpdate with
        member x.Deconstruct([<Out>] notifications : byref<NotificationsList>, [<Out>] success : byref<bool>, [<Out>] key : byref<string>) =
            match x with
            | TaskUpdate.DeleteBucketItem (a, b, c) ->
                notifications <- a
                success <- b
                key <- c
            | _ -> invalidArg "DeleteBucketItem" "TaskUpdate.Deconstruct: unknown type"
    
        member x.Deconstruct([<Out>] notifications : byref<NotificationsList>, [<Out>] success : byref<bool>, [<Out>] key : byref<string>, [<Out>] filePath : byref<string>) =
            match x with
            | TaskUpdate.DownloadBucketItem (a, b, c, d) ->
                notifications <- a
                success <- b
                key <- c
                filePath <- d
            | _ -> invalidArg "DownloadBucketItem" "TaskUpdate.Deconstruct: unknown type"
    
    let checkContextIs requiredContext (context:MiniTaskContext) =
        match context with
        | BucketItemsModel _ -> 
            if context = requiredContext then
                true
            else
                false
        
    let checkContextIsBucketItemsModel (context:MiniTaskContext) = true
        //match context with
        //| BucketItemsModel(_) -> true
        //| _ -> false

    let Delete awsInterface (notifications: NotificationsList) (context:MiniTaskContext) (args:MiniTaskArgument[]) =
        if args.Length >= 2 then
            if checkContextIsBucketItemsModel context then
                let (bucketName, key) =
                    match (args.[0], args.[1]) with
                    | (MiniTaskArgument.String a, MiniTaskArgument.String b) -> (a, b)
                    | _ -> invalidArg "args" "MiniTasks.Delete: Expecting two string arguments"
                let success = S3.deleteBucketItem awsInterface notifications bucketName key |> Async.RunSynchronously
                TaskUpdate.DeleteBucketItem (notifications, success, key)
            else
                invalidArg "context" "MiniTasks.Delete: Unrecognized context"
        else invalidArg "args" "MiniTasks.Delete: Expecting two arguments"

    let Download awsInterface (notifications: NotificationsList) (context:MiniTaskContext) (args:MiniTaskArgument[]) =
        if args.Length >= 3 then
            if checkContextIsBucketItemsModel context then
                let (bucketName, key, filePath) =
                    match (args.[0], args.[1], args.[2]) with
                    | (MiniTaskArgument.String a, MiniTaskArgument.String b, MiniTaskArgument.String c) -> (a, b, c)
                    | _ -> invalidArg "args" "MiniTasks.Download: Expecting three string arguments"
                let success = S3.downloadBucketItemToFile awsInterface notifications bucketName key filePath |> Async.RunSynchronously
                TaskUpdate.DownloadBucketItem (notifications, success, key, filePath)
            else
                invalidArg "context" "MiniTasks.Download: Unrecognized context"
        else invalidArg "args" "MiniTasks.Download: Expecting three arguments"
