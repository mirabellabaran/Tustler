﻿namespace CloudWeaver.Types

open TustlerServicesLib
open TustlerModels
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open System.Text.Json
open System.IO

// an attribute to mark CloudWeaver modules that contain Task Functions
type CloudWeaverTaskFunctionModule() = inherit System.Attribute()

// an attribute to tell the UI not to show certain task functions (those that are called as sub-tasks)
type HideFromUI() = inherit System.Attribute()

type ModuleIdentifier =
    | Identifier of string
    with
    member x.AsString() = match x with | Identifier str -> str

type IShareIntraModule =
    abstract member Identifier : ModuleIdentifier with get
    abstract member AsBytes : unit -> byte[]
    abstract member Serialize : Utf8JsonWriter -> unit
    abstract member ToString: unit -> string

type IShareInterModule =
    abstract member Identifier : ModuleIdentifier with get
    abstract member AsBytes : unit -> byte[]
    abstract member Serialize : Utf8JsonWriter -> unit
    abstract member ToString: unit -> string

type IRequestIntraModule =
    inherit System.IComparable
    abstract member Identifier: ModuleIdentifier with get
    abstract member ToString: unit -> string

type IShowValue =
    abstract member Identifier: ModuleIdentifier with get
    abstract member ToString: unit -> string

/// A task in the overall task sequence (tasks may be sequentially dependant or independant)
type TaskItem = {
    ModuleName: string          // the fully qualified name of the type containing the task function
    TaskName: string;           // the task function name of the task
    Description: string;
}
with
    override this.ToString() = sprintf "TaskItem: %s %s %s" this.ModuleName this.TaskName this.Description

/// Responses returned by Task Functions
[<RequireQualifiedAccess>]
type TaskResponse =
    | TaskInfo of string
    | TaskComplete of string
    | TaskPrompt of string                  // prompt the user to continue (a single Continue button is displayed along with the prompt message)
    | TaskSelect of string                  // prompt the user to select an item (this is also a truncation point for subsequent reselection)
    | TaskMultiSelect of IEnumerable<TaskItem>       // user selects zero or more sub-tasks to perform
    | TaskSequence of IEnumerable<TaskItem>          // a sequence of tasks that flow from one to the next without any intervening UI
    | TaskContinue of int                               // re-invoke the current function after the specified number of milliseconds
    | TaskArgumentSave                                  // save any arguments set on the event stack for subsequent sessions
    
    | Notification of Notification
    
    // Values for UI display only
    | ShowValue of IShowValue

    | SetArgument of IShareIntraModule
    | SetBoundaryArgument of IShareInterModule
    
    // Values that are sent as requests to the user
    | RequestArgument of IRequestIntraModule
    with
    override this.ToString() =
        let folder items mapper =
            let strings =
                items
                |> Seq.map mapper
            System.String.Join(", ", strings)
        match this with
        | TaskInfo str -> (sprintf "TaskInfo: %s" str)
        | TaskComplete str -> (sprintf "TaskComplete: %s" str)
        | TaskPrompt str -> (sprintf "TaskPrompt: %s" str)
        | TaskSelect str -> (sprintf "TaskSelect: %s" str)
        | TaskMultiSelect taskItems -> (sprintf "TaskMultiSelect: %s" (System.String.Join(", ", taskItems)))    // assume TaskItem.ToString()
        | TaskSequence taskItems -> (sprintf "TaskSequence: %s" (System.String.Join(", ", taskItems)))    // assume TaskItem.ToString()
        | TaskContinue delay -> (sprintf "TaskContinue: %d (ms)" delay)
        | TaskArgumentSave -> "TaskArgumentSave"
        | Notification notification -> (sprintf "Notification: %s" (notification.ToString()))
        | ShowValue showValue -> (sprintf "ShowValue: %s" (showValue.ToString()))
        | SetArgument arg -> (sprintf "SetArgument: %s" (arg.ToString()))
        | SetBoundaryArgument arg -> (sprintf "SetBoundaryArgument: %s" (arg.ToString()))
        | RequestArgument request -> (sprintf "RequestArgument: %s" (request.ToString()))

    
/// The event types allowed on the event stack
[<RequireQualifiedAccess>]
type TaskEvent =
    | InvokingFunction
    | SetArgument of TaskResponse
    | ForEach of RetainingStack<TaskItem>
    | Task of TaskItem     // the name and description of the task
    | SelectArgument
    | ClearArguments
    | FunctionCompleted

/// A simpler option type for use in C# space
[<RequireQualifiedAccess>]
type MaybeResponse =
    | Just of TaskResponse
    | Nothing
type MaybeResponse with
    member x.IsSet = match x with MaybeResponse.Just _ -> true | MaybeResponse.Nothing -> false
    member x.IsNotSet = match x with MaybeResponse.Nothing -> true | MaybeResponse.Just _ -> false
    member x.Value = match x with MaybeResponse.Nothing -> invalidArg "MaybeResponse.Value" "Value not set" | MaybeResponse.Just tr -> tr

/// Interface for arguments that have assigned values prior to executing a task function
// examples: the TaskItem for the current task function; the notifications list; the backend interface such as AWSInterface
type IKnownArguments =
    abstract member KnownRequests : seq<IRequestIntraModule> with get            // the module-specific requests that this module can respond to
    abstract member GetKnownArgument : request:IRequestIntraModule -> TaskEvent

// the collection of types supplied by different modules that are containers for known arguments
type KnownArgumentsCollection () =
    let mutable knownArgumentsMap = Map.empty

    member x.AddModule (knownArgumentsContainer: IKnownArguments) =
        knownArgumentsMap <-
            knownArgumentsContainer.KnownRequests
            |> Seq.fold (fun (knownArgumentsMap:Map<_, _>) request -> knownArgumentsMap.Add (request.Identifier, knownArgumentsContainer)) knownArgumentsMap
    member x.IsKnownArgument (request: IRequestIntraModule) = knownArgumentsMap.ContainsKey request.Identifier
    member x.GetKnownArgument (request: IRequestIntraModule) =
        let knownArgumentsContainer = knownArgumentsMap.[request.Identifier]
        knownArgumentsContainer.GetKnownArgument(request)

/// Requests common to all modules
type StandardRequest =
    | RequestNotifications      // the location for writing any generated notifications
    | RequestTaskName           // the name of a task (usually a GUID; used as filename for intermediate and final results)
    | RequestTaskItem           // the current task function name and description (one of the user-selected items from the MultiSelect list)
    | RequestWorkingDirectory   // the filesystem folder where values specific to the current task function can be written and read
    | RequestSaveFlags          // a set of flags controlling the saving of intermediate results
    with
    override this.ToString() =
        match this with
        | RequestNotifications -> "RequestNotifications"
        | RequestTaskName -> "RequestTaskName"
        | RequestTaskItem -> "RequestTaskItem"
        | RequestWorkingDirectory -> "RequestWorkingDirectory"
        | RequestSaveFlags -> "RequestSaveFlags"

/// Arguments common to all modules
type StandardArgument =
    | SetNotificationsList of NotificationsList
    | SetTaskName of string option
    | SetTaskItem of TaskItem option
    | SetWorkingDirectory of DirectoryInfo option
    | SetSaveFlags of SaveFlags option
    with
    override this.ToString() =
        match this with
        | SetNotificationsList notifications -> (sprintf "SetNotificationsList: %s" (System.String.Join(", ", notifications.Notifications)))
        | SetTaskName taskName -> (sprintf "SetTaskName: %s" (if taskName.IsSome then taskName.Value else "None"))
        | SetTaskItem taskItem -> (sprintf "SetTaskItem: %s" (if taskItem.IsSome then taskItem.Value.ToString() else "None"))
        | SetWorkingDirectory dirInfo -> (sprintf "SetWorkingDirectory: %s" (if dirInfo.IsSome then dirInfo.Value.ToString() else "None"))
        | SetSaveFlags saveFlgs -> (sprintf "SetSaveFlags: %s" (if saveFlgs.IsSome then saveFlgs.Value.ToString() else "None"))

    member this.toTaskResponse() = TaskResponse.SetArgument (StandardShareIntraModule(this))
    member this.toTaskEvent() = TaskEvent.SetArgument(this.toTaskResponse());

/// Wraps standard argument types
and StandardShareIntraModule(arg: StandardArgument) =
    interface IShareIntraModule with
        member this.Identifier with get() = Identifier (CommonUtilities.toString arg)
        member this.ToString () = sprintf "StandardShareIntraModule(%s)" (arg.ToString())
        member this.AsBytes () =
            JsonSerializer.SerializeToUtf8Bytes(arg)
        member this.Serialize writer =
            match arg with
            | SetNotificationsList _notificationsList -> ()     // don't serialize
            | SetTaskName _taskName -> ()                       // don't serialize
            | SetTaskItem _taskItem -> ()                       // don't serialize
            | SetWorkingDirectory _dir -> ()                    // don't serialize
            | SetSaveFlags _flags -> ()                         // don't serialize

    member this.Argument with get() = arg

/// Wraps standard request types
type StandardRequestIntraModule(stdRequest: StandardRequest) =
    
    interface IRequestIntraModule with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> IRequestIntraModule).Identifier.AsString()
            let str2 = (obj :?> IRequestIntraModule).Identifier.AsString()
            System.String.Compare(str1, str2)
        member this.Identifier with get() = Identifier (CommonUtilities.toString stdRequest)
        member this.ToString () = sprintf "StandardRequestIntraModule(%s)" (stdRequest.ToString())

    member this.Request with get() = stdRequest

/// Arguments whose values are known in advance (and are shared across task function modules)
type StandardKnownArguments(notificationsList) =

    interface IKnownArguments with
        member this.KnownRequests with get() =
            seq {
                StandardRequestIntraModule(StandardRequest.RequestNotifications)
            }
        member this.GetKnownArgument(request: IRequestIntraModule) =
            let unWrapRequest (request:IRequestIntraModule) =
                match request with
                | :? StandardRequestIntraModule as stdRequestIntraModule -> stdRequestIntraModule.Request
                | _ -> invalidArg "request" "The request is not of type StandardRequestIntraModule"
            match (unWrapRequest request) with
            | RequestNotifications -> StandardArgument.SetNotificationsList(notificationsList).toTaskEvent()
            | _ -> invalidArg "request" "Unexpected request type (do you mean to use StandardVariables?)"

/// Runtime modifiable values
type StandardVariables() =
    let mutable taskNameArgument = StandardArgument.SetTaskName(None)
    let mutable taskItemArgument = StandardArgument.SetTaskItem(None)
    let mutable workingDirectoryArgument = StandardArgument.SetWorkingDirectory(None)
    let mutable saveFlagsArgument = StandardArgument.SetSaveFlags(None)

    interface IKnownArguments with
        member this.KnownRequests with get() =
            seq {
                StandardRequestIntraModule(StandardRequest.RequestTaskName);
                StandardRequestIntraModule(StandardRequest.RequestTaskItem);
                StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory);
                StandardRequestIntraModule(StandardRequest.RequestSaveFlags);
            }
        member this.GetKnownArgument(request: IRequestIntraModule) =
            let unWrapRequest (request:IRequestIntraModule) =
                match request with
                | :? StandardRequestIntraModule as stdRequestIntraModule -> stdRequestIntraModule.Request
                | _ -> invalidArg "request" "The request is not of type StandardRequestIntraModule"
            match (unWrapRequest request) with
            | RequestTaskName -> taskNameArgument.toTaskEvent()
            | RequestTaskItem -> taskItemArgument.toTaskEvent()
            | RequestWorkingDirectory -> workingDirectoryArgument.toTaskEvent()
            | RequestSaveFlags -> saveFlagsArgument.toTaskEvent()
            | _ -> invalidArg "request" "Unexpected request type (do you mean to use StandardKnownArguments?)"

    member this.SetValue(request, value:obj) =
        match request with
        | RequestTaskName ->
            match value with
            | :? string -> taskNameArgument <- StandardArgument.SetTaskName(Some(value :?> string))
            | _ -> invalidArg "value" (sprintf "%A is not an expected value" value)
        | RequestTaskItem ->
            match value with
            | :? TaskItem -> taskItemArgument <- StandardArgument.SetTaskItem(Some(value :?> TaskItem))
            | _ -> invalidArg "value" (sprintf "%A is not an expected value" value)
        | RequestWorkingDirectory ->
            match value with
            | :? DirectoryInfo -> workingDirectoryArgument <- StandardArgument.SetWorkingDirectory(Some(value :?> DirectoryInfo))
            | _ -> invalidArg "value" (sprintf "%A is not an expected value" value)
        | RequestSaveFlags ->
            match value with
            | :? SaveFlags -> saveFlagsArgument <- StandardArgument.SetSaveFlags(Some(value :?> SaveFlags))
            | _ -> invalidArg "value" (sprintf "%A is not an expected value" value)
        | _ -> invalidArg "request" (sprintf "%A is not a runtime modifiable value" request)