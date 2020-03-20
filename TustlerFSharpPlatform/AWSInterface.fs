module AWSInterface

open TustlerAWSLib

let getBuckets =
    async {
        let! value = TustlerAWSLib.S3.ListBuckets() |> Async.AwaitTask
        return value
    }

printfn "%A" getBuckets