//#r "mscorlib.dll"
//#r "System.IO.dll"
//#r "System.Runtime.dll"
//#r "System.Runtime.Extensions.dll"
//#r "netstandard.dll"
//#r "System.Threading.dll"
//#r "System.Threading.Tasks.dll"
//#I "C:\\Users\\Zev\\Projects\\C#\\Tustler\\TustlerAWSLib\\bin\\Debug\\netcoreapp3.1\\"
//#r "TustlerAWSLib.dll"
//#r "TustlerInterfaces.dll"
#r "FSharp.Core.dll"
#r "System.ObjectModel.dll"
#I "C:\\Users\\Zev\\Projects\\C#\\Tustler\\TustlerFSharpPlatform\\bin\\Debug\\netcoreapp3.1"
#r "TustlerAWSLib.dll"
#r "TustlerInterfaces.dll"
#r "TustlerModels.dll"
#r "TustlerServicesLib.dll"
#r "TustlerFSharpPlatform.dll"

open TustlerFSharpPlatform.AWSInterface
open Microsoft.FSharp.Control

//Microsoft.FSharp.Control.FSharpAsync.RunSynchronously
let buckets, notifications = S3.getBuckets
let bucket = if buckets.Count > 0 then Some(Seq.head buckets) else None
let note = if notifications.Notifications.Count > 0 then Some(downCastNotification (Seq.head notifications.Notifications)) else None

printfn "%s %s" (if Option.isSome bucket then bucket.Value.Name else "Null return") (if note.IsSome then note.Value else "No notifications")
