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
#I "C:\\Users\\Zev\\Projects\\C#\\Tustler\\TustlerFSharpPlatform\\bin\\Debug\\netcoreapp3.1"
#r "TustlerAWSLib.dll"
#r "TustlerInterfaces.dll"
#r "TustlerModels.dll"
#r "TustlerServicesLib.dll"
#r "TustlerFSharpPlatform.dll"

open TustlerFSharpPlatform
open TustlerModels

let bucket = AWSInterface.getBuckets
printfn "%A" bucket