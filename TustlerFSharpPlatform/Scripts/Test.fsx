#r "mscorlib.dll"
//#r "System.Runtime.dll"
#r "netstandard.dll"
#r "System.Threading.dll"
#r "System.Threading.Tasks.dll"
#I "C:\\Users\\Zev\\Projects\\C#\\Tustler\\TustlerAWSLib\\bin\\Debug\\netcoreapp3.1\\"
#r "TustlerAWSLib.dll"
#r "TustlerInterfaces.dll"

open System.IO
open System.Threading.Tasks
open TustlerAWSLib

let getValueFromLibrary param =
    async {
        let! value = TustlerAWSLib.S3.ListBuckets() |> Async.AwaitTask
        return value
    }

let getValueFromLibrary param =
    TustlerAWSLib.S3.ListBuckets()
    |> Async.AwaitTask
    |> Async.RunSynchronously