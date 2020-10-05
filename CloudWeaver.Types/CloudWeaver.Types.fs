﻿namespace CloudWeaver.Types

open TustlerServicesLib
open System.Collections
open System.Collections.Generic
open System.Collections.Immutable
open System.Text.Json
open System.IO
open System
open System.Text

// an attribute to mark CloudWeaver modules that contain Task Functions
type CloudWeaverTaskFunctionModule() = inherit System.Attribute()

// an attribute for decorating a Task Function; tells the UI to enable logging of TaskEvents generated by the attribute-decorated task function
type EnableLogging() = inherit System.Attribute()

// an attribute for decorating a Task Function; tells the UI not to show task functions that are decorated with this attribute (those that are called as sub-tasks)
type HideFromUI() = inherit System.Attribute()

type TaskFunctionQueryMode =
    | Description               // show a description of the task function
    | Inputs                    // show the task function inputs
    | Outputs                   // show the task function outputs
    | Invoke                    // run the task function and generate outputs

type ModuleTag =
    | Tag of string
    with
    member x.AsString() = match x with | Tag str -> str

type WrappedItemIdentifier =
    | Identifier of string
    with
    member x.AsString() = match x with | Identifier str -> str

type IShareIntraModule =
    abstract member ModuleTag : ModuleTag with get
    abstract member Identifier : WrappedItemIdentifier with get
    abstract member Description : unit -> string
    abstract member AsBytes : unit -> byte[]
    abstract member Serialize : Utf8JsonWriter -> JsonSerializerOptions -> unit
    abstract member ToString: unit -> string

//type IShareInterModule =
//    abstract member ModuleTag : ModuleTag with get
//    abstract member Identifier : WrappedItemIdentifier with get
//    abstract member AsBytes : unit -> byte[]
//    abstract member Serialize : Utf8JsonWriter -> unit
//    abstract member ToString: unit -> string

type IRequestIntraModule =
    inherit System.IComparable
    abstract member Identifier: WrappedItemIdentifier with get
    abstract member ToString: unit -> string

type IShowValue =
    abstract member Identifier: WrappedItemIdentifier with get
    abstract member ToString: unit -> string

/// A task in the overall task sequence (tasks may be sequentially dependant or independant)
type TaskItem(moduleName: string, taskName: string, description: string) =
    let mutable _moduleName = moduleName
    let mutable _taskName = taskName
    let mutable _description = description

    member this.ModuleName with get() = _moduleName and set(value) = _moduleName <- value
    member this.TaskName with get() = _taskName and set(value) = _taskName <- value
    member this.Description with get() = _description and set(value) = _description <- value
    member this.FullPath with get() = sprintf "%s.%s" this.ModuleName this.TaskName

    new() = TaskItem(null, null, null)
    override this.ToString() = sprintf "TaskItem: %s %s %s" this.ModuleName this.TaskName this.Description

/// Specifies whether stack items such as tasks are independant or sequentially dependant
type ItemOrdering =
    | Independant       // execution of each item is independant of that of its peers
    | Sequential        // items are executed sequentially with sequential dependancies
    with
    override this.ToString() =
        match this with
        | Independant -> "Independant"
        | Sequential -> "Sequential"
    static member FromString(serializedRepr) =
        match serializedRepr with
        | "Independant" -> Independant
        | "Sequential" -> Sequential
        | _ -> invalidArg "serializedRepr" "Unknown ItemOrdering type"

type StandardIterationArgument =
    | Task of TaskItem

type StandardShareIterationArgument(arg: StandardIterationArgument) =
    interface IShareIterationArgument with
        member this.ToString () =
            match arg with
            | Task task -> sprintf "StandardShareIterationArgument(Task: %s)" (task.TaskName)
        member this.Serialize writer =
            writer.WriteStartObject()
            match arg with
            | Task task -> writer.WritePropertyName("TaskItem"); JsonSerializer.Serialize(writer, task)
            writer.WriteEndObject()

    member this.UnWrap with get() = arg

    static member Deserialize (wrappedObject: JsonElement) =
        let property = wrappedObject.EnumerateObject() |> Seq.exactlyOne

        let iterationArgument =
            match property.Name with
            | "TaskItem" ->
                let data = JsonSerializer.Deserialize<TaskItem>(property.Value.GetRawText())
                StandardIterationArgument.Task data
            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" property.Name)

        StandardShareIterationArgument(iterationArgument) :> IShareIterationArgument

/// An iteration argument stack (IConsumable) for StandardIterationArgument types
type StandardIterationStack(uid: Guid, items: IEnumerable<IShareIterationArgument>) =
    inherit RetainingStack(uid, items)

    override this.ModuleName with get() = "StandardShareIterationArgument"

type IConsumableTaskSequence =
    inherit IEnumerable<TaskItem>
    abstract member Identifier: Guid with get
    abstract member Total : int with get
    abstract member Remaining : int with get
    abstract member Current : TaskItem option with get
    abstract member Reset : unit -> unit
    abstract member Ordering : ItemOrdering with get
    abstract member ConsumeTask : unit -> unit          // consume the current item

/// Represents a sequence of tasks, including the current task. The sequence must be consumed in order to set the current task.
type TaskSequence(uid: Guid, tasks: IEnumerable<TaskItem>, ordering: ItemOrdering) =

    do
        if (isNull tasks) then invalidArg "tasks" "Expecting a non-null value for tasks"

    let _array = tasks.ToImmutableArray()
    let _stack = Stack<TaskItem>(Seq.rev tasks)  // reversed so that calling Pop() removes items in _array order

    new(uid, items) = TaskSequence(uid, items, ItemOrdering.Sequential)

    /// Get the ordering of items for this instance
    member this.Ordering with get() = ordering

    /// Get the unique identifier for this instance
    member this.Identifier with get() = uid

    /// Get the total count
    member this.Total with get() = _array.Length

    /// Get a count of the remaining (consumable) items
    member this.Remaining with get() = _stack.Count

    /// Get the current item (the last popped item)
    member this.Current with get() =
        let popped = this.Total - this.Remaining - 1
        if popped < 0 then
            None
        else
            let current = _array |> Seq.skip popped |> Seq.take 1 |> Seq.exactlyOne
            Some(current)

    /// Refill the stack with the same items used at construction time
    member this.Reset() =

        _stack.Clear()

        _array
        |> Seq.rev
        |> Seq.iter (fun item -> _stack.Push(item))

    override this.ToString() = sprintf "TaskSequence of TaskItem: total=%d; remaining=%d" (_array.Length) (_stack.Count)

    interface IConsumableTaskSequence with

        member this.Identifier with get() = this.Identifier

        member this.Total: int = this.Total

        member this.Remaining: int = this.Remaining

        member this.Current: TaskItem option = this.Current

        member this.Reset(): unit = this.Reset()

        member this.ConsumeTask(): unit = _stack.Pop() |> ignore

        member this.Ordering: ItemOrdering = this.Ordering

    interface IEnumerable<TaskItem> with

        member this.GetEnumerator(): System.Collections.IEnumerator = 
            (_array :> IEnumerable).GetEnumerator()

        member this.GetEnumerator(): IEnumerator<TaskItem> = 
            (_array :> IEnumerable<TaskItem>).GetEnumerator()

type SaveEventsFilter =
    | AllEvents
    | ArgumentsOnly

/// The event types allowed on the event stack
[<RequireQualifiedAccess>]
type TaskEvent =
    | InvokingFunction
    | SetArgument of TaskResponse
    | ForEachTask of IConsumableTaskSequence            // add a stack frame of tasks (with a guid identifier)
    | ForEachDataItem of IConsumable                    // add a stack frame of data items (with a guid identifier)
    | ConsumedData of Guid                              // data was consumed from the specified IConsumable; the top of stack is the new current value
    | ConsumedTask of Guid                              // a task was consumed from the specified IConsumableTaskSequence; the stack.Current property holds the new current value
    | Task of TaskItem                                  // record the name and description of the current task
    | TaskError of TaskItem                             // record an error on the current task
    | SelectArgument
    | ClearArguments
    | FunctionCompleted

/// Responses returned by Task Functions
and
    [<RequireQualifiedAccess>]
    TaskResponse =
    | TaskDescription of string             // show a description of the task function
    | TaskInfo of string                    // display arbitrary information to the UI
    | TaskComplete of string * DateTime     // indicate completion of the current (sub)task (with relevant information and a timestamp)
    | TaskPrompt of string                  // prompt the user to continue (a single Continue button is displayed along with the prompt message)
    | TaskSelect of string                  // prompt the user to select an item (this is also a truncation point for subsequent reselection)
    | TaskMultiSelect of IEnumerable<TaskItem>       // user selects zero or more sub-tasks to perform
    | TaskSequence of IEnumerable<TaskItem>          // a sequence of tasks that flow from one to the next without any intervening UI
    | TaskContinue of int                               // re-invoke the current function after the specified number of milliseconds
    | TaskSaveEvents of SaveEventsFilter                // save all events (or all argument events) on the event stack as a JSON document for subsequent sessions
    | TaskConvertToBinary of JsonDocument               // convert a JSON document to binary log format and set an argument (SetLogFormatEvents)
    | TaskConvertToJson of byte[]                       // convert the log format data to JSON document format and set an argument (SetJsonEvents)
    
    | ChooseTask                            // ask the UI to present a list of task functions

    | Notification of Notification
    | BeginLoopSequence of IConsumable * IEnumerable<TaskItem>  // execute the task sequence for each consumable data item (the specified tasks are inside the loop)

    // Values for UI display only
    | ShowValue of IShowValue

    | SetArgument of IShareIntraModule
    //| SetBoundaryArgument of IShareInterModule
    
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
        | TaskDescription str -> (sprintf "TaskDescription: %s" str)
        | TaskInfo str -> (sprintf "TaskInfo: %s" str)
        | TaskComplete (str, dt) -> (sprintf "TaskComplete: %s (%A)" str dt)
        | TaskPrompt str -> (sprintf "TaskPrompt: %s" str)
        | TaskSelect str -> (sprintf "TaskSelect: %s" str)
        | TaskMultiSelect taskItems -> (sprintf "TaskMultiSelect: %s" (System.String.Join(", ", (Seq.map (fun (item: TaskItem) -> item.TaskName) taskItems))))
        | TaskSequence taskItems -> (sprintf "TaskSequence: %s" (System.String.Join(", ", (Seq.map (fun (item: TaskItem) -> item.TaskName) taskItems))))
        | TaskContinue delay -> (sprintf "TaskContinue: %d (ms)" delay)
        | TaskSaveEvents filter -> (sprintf "TaskSaveEvents: %A" filter)
        | TaskConvertToBinary _jsonDocument -> "TaskConvertToBinary {document}"
        | TaskConvertToJson data -> (sprintf "TaskConvertToJson: (%d bytes)" data.Length)
        | ChooseTask -> "ChooseTask"
        | Notification notification -> (sprintf "Notification: %s" (notification.ToString()))
        | BeginLoopSequence (consumable, taskItems) -> (sprintf "BeginLoopSequence (%d items): %s" consumable.Total (System.String.Join(", ", (Seq.map (fun (item: TaskItem) -> item.TaskName) taskItems))))
        | ShowValue showValue -> (sprintf "ShowValue: %s" (showValue.ToString()))
        | SetArgument arg -> (sprintf "SetArgument: %s" (arg.ToString()))
        //| SetBoundaryArgument arg -> (sprintf "SetBoundaryArgument: %s" (arg.ToString()))
        | RequestArgument request -> (sprintf "RequestArgument: %s" (request.ToString()))

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
    | RequestTaskIdentifier     // the identifier for a task (usually a GUID; used as filename for intermediate and final results)
    | RequestTaskItem           // the current task function name and description (one of the user-selected items from the MultiSelect list)
    | RequestWorkingDirectory   // the filesystem folder where values specific to the current task function can be written and read
    | RequestSaveFlags          // a set of flags controlling the saving of intermediate results
    | RequestJsonEvents         // a byte array representing a collection of TaskEvents in JSON document format
    | RequestLogFormatEvents    // a byte array representing a collection of TaskEvents in binary log format
    | RequestOpenJsonFilePath       // the path to a file that stores TaskEvents in JSON document format for open/read
    | RequestSaveJsonFilePath       // the path to a file that stores TaskEvents in JSON document format for save/write
    | RequestOpenLogFormatFilePath  // the path to a file that stores TaskEvents in binary log format for open/read
    | RequestSaveLogFormatFilePath  // the path to a file that stores TaskEvents in binary log format for save/write
    with
    override this.ToString() =
        match this with
        | RequestNotifications -> "RequestNotifications"
        | RequestTaskIdentifier -> "RequestTaskIdentifier"
        | RequestTaskItem -> "RequestTaskItem"
        | RequestWorkingDirectory -> "RequestWorkingDirectory"
        | RequestSaveFlags -> "RequestSaveFlags"
        | RequestJsonEvents -> "RequestJsonEvents"
        | RequestLogFormatEvents -> "RequestLogFormatEvents"
        | RequestOpenJsonFilePath -> "RequestOpenJsonFilePath"
        | RequestSaveJsonFilePath -> "RequestSaveJsonFilePath"
        | RequestOpenLogFormatFilePath -> "RequestOpenLogFormatFilePath"
        | RequestSaveLogFormatFilePath -> "RequestSaveLogFormatFilePath"

/// Arguments common to all modules
type StandardArgument =
    | SetNotificationsList of NotificationsList
    | SetTaskIdentifier of string option
    | SetTaskItem of TaskItem option
    | SetWorkingDirectory of DirectoryInfo option
    | SetSaveFlags of SaveFlags option
    | SetJsonEvents of byte[]
    | SetLogFormatEvents of byte[]
    | SetOpenJsonFilePath of FileInfo
    | SetSaveJsonFilePath of FileInfo
    | SetOpenLogFormatFilePath of FileInfo
    | SetSaveLogFormatFilePath of FileInfo
    with
    override this.ToString() =
        match this with
        | SetNotificationsList notifications -> (sprintf "SetNotificationsList: %s" (System.String.Join(", ", notifications.Notifications)))
        | SetTaskIdentifier taskId -> (sprintf "SetTaskName: %s" (if taskId.IsSome then taskId.Value else "None"))
        | SetTaskItem taskItem -> (sprintf "SetTaskItem: %s" (if taskItem.IsSome then taskItem.Value.ToString() else "None"))
        | SetWorkingDirectory dirInfo -> (sprintf "SetWorkingDirectory: %s" (if dirInfo.IsSome then dirInfo.Value.ToString() else "None"))
        | SetSaveFlags saveFlgs -> (sprintf "SetSaveFlags: %s" (if saveFlgs.IsSome then saveFlgs.Value.ToString() else "None"))
        | SetJsonEvents data -> (sprintf "SetJsonEvents: (%d bytes)" data.Length)
        | SetLogFormatEvents data -> (sprintf "SetLogFormatEvents: (%d bytes)" data.Length)
        | SetOpenJsonFilePath fileInfo -> (sprintf "SetOpenJsonFilePath: %s" fileInfo.FullName)
        | SetSaveJsonFilePath fileInfo -> (sprintf "SetSaveJsonFilePath: %s" fileInfo.FullName)
        | SetOpenLogFormatFilePath fileInfo -> (sprintf "SetOpenLogFormatFilePath: %s" fileInfo.FullName)
        | SetSaveLogFormatFilePath fileInfo -> (sprintf "SetSaveLogFormatFilePath: %s" fileInfo.FullName)

    member this.toTaskResponse() = TaskResponse.SetArgument (StandardShareIntraModule(this))
    member this.toTaskEvent() = TaskEvent.SetArgument(this.toTaskResponse());

/// Wraps standard argument types
and StandardShareIntraModule(arg: StandardArgument) =
    let getAsBytes (sim: IShareIntraModule) = UTF8Encoding.UTF8.GetString(sim.AsBytes())
    interface IShareIntraModule with
        member this.ModuleTag with get() = Tag "StandardShareIntraModule"
        member this.Identifier with get() = Identifier (CommonUtilities.toString arg)
        member this.ToString () = sprintf "StandardShareIntraModule(%s)" (arg.ToString())
        member this.Description () =
            match arg with
            | SetNotificationsList notificationsList -> sprintf "NotificationsList: %d notifications" notificationsList.Notifications.Count
            | SetTaskIdentifier taskId -> sprintf "TaskIdentifier: %s" (if taskId.IsSome then taskId.Value else "[None]")
            | SetTaskItem taskItem -> if taskItem.IsSome then taskItem.Value.ToString() else "[None]"
            | SetWorkingDirectory dir -> sprintf "WorkingDirectory: %s" (if dir.IsSome then dir.Value.FullName else "[None]")
            | SetSaveFlags flags -> sprintf "SaveFlags: %s" (if flags.IsSome then flags.Value.ToString() else "[None]")
            | SetJsonEvents data -> sprintf "JsonEvents: %d bytes" (data.Length)
            | SetLogFormatEvents data -> sprintf "LogFormatEvents: %d bytes" (data.Length)
            | SetOpenJsonFilePath fileInfo -> sprintf "OpenJsonFilePath: %s" (fileInfo.FullName)
            | SetSaveJsonFilePath fileInfo -> sprintf "SaveJsonFilePath: %s" (fileInfo.FullName)
            | SetOpenLogFormatFilePath fileInfo -> sprintf "OpenLogFormatFilePath: %s" (fileInfo.FullName)
            | SetSaveLogFormatFilePath fileInfo -> sprintf "SaveLogFormatFilePath: %s" (fileInfo.FullName)
        member this.AsBytes () =    // returns either a UTF8-encoded string or a UTF8-encoded Json document as a byte array
            match arg with
            | SetNotificationsList notificationsList -> JsonSerializer.SerializeToUtf8Bytes(notificationsList)
            | SetTaskIdentifier taskId -> if taskId.IsSome then UTF8Encoding.UTF8.GetBytes(taskId.Value) else Array.Empty<byte>()
            | SetTaskItem taskItem -> if taskItem.IsSome then JsonSerializer.SerializeToUtf8Bytes(taskItem.Value) else Array.Empty<byte>()
            | SetWorkingDirectory dir -> if dir.IsSome then UTF8Encoding.UTF8.GetBytes(dir.Value.FullName) else Array.Empty<byte>()
            | SetSaveFlags flags -> if flags.IsSome then JsonSerializer.SerializeToUtf8Bytes(flags.Value.ToArray()) else Array.Empty<byte>()
            | SetJsonEvents data -> data    // note that this UTF8-encoded Json is deliberately pretty-printed
            | SetLogFormatEvents data -> JsonSerializer.SerializeToUtf8Bytes(data)
            | SetOpenJsonFilePath fileInfo -> UTF8Encoding.UTF8.GetBytes(fileInfo.FullName)
            | SetSaveJsonFilePath fileInfo -> UTF8Encoding.UTF8.GetBytes(fileInfo.FullName)
            | SetOpenLogFormatFilePath fileInfo -> UTF8Encoding.UTF8.GetBytes(fileInfo.FullName)
            | SetSaveLogFormatFilePath fileInfo -> UTF8Encoding.UTF8.GetBytes(fileInfo.FullName)
        member this.Serialize writer _serializerOptions =
            match arg with
            | SetNotificationsList _notificationsList -> writer.WritePropertyName("SetNotificationsList"); JsonSerializer.Serialize(writer, "") // don't serialize the value
            | SetTaskIdentifier taskId -> writer.WritePropertyName("SetTaskIdentifier"); JsonSerializer.Serialize(writer, if taskId.IsSome then taskId.Value else "")
            | SetTaskItem taskItem -> writer.WritePropertyName("SetTaskItem"); JsonSerializer.Serialize(writer, taskItem)
            | SetWorkingDirectory dir -> writer.WritePropertyName("SetWorkingDirectory"); JsonSerializer.Serialize(writer, if dir.IsSome then dir.Value.FullName else "")
            | SetSaveFlags flags -> writer.WritePropertyName("SetSaveFlags"); JsonSerializer.Serialize(writer, if flags.IsSome then flags.Value.ToString() else "")
            | SetJsonEvents data -> writer.WritePropertyName("SetJsonEvents"); JsonSerializer.Serialize<byte[]>(writer, data)
            | SetLogFormatEvents data -> writer.WritePropertyName("SetLogFormatEvents"); JsonSerializer.Serialize<byte[]>(writer, data)
            | SetOpenJsonFilePath fileInfo -> writer.WritePropertyName("SetOpenJsonFilePath"); JsonSerializer.Serialize<string>(writer, fileInfo.FullName)
            | SetSaveJsonFilePath fileInfo -> writer.WritePropertyName("SetSaveJsonFilePath"); JsonSerializer.Serialize<string>(writer, fileInfo.FullName)
            | SetOpenLogFormatFilePath fileInfo -> writer.WritePropertyName("SetOpenLogFormatFilePath"); JsonSerializer.Serialize<string>(writer, fileInfo.FullName)
            | SetSaveLogFormatFilePath fileInfo -> writer.WritePropertyName("SetSaveLogFormatFilePath"); JsonSerializer.Serialize<string>(writer, fileInfo.FullName)

    member this.Argument with get() = arg

    static member Deserialize propertyName (jsonString:string) (flagResolverDictionary: Dictionary<string, Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>>) =
        let standardArgument =
            match propertyName with
            | "SetNotificationsList" ->
                StandardArgument.SetNotificationsList (NotificationsList())
            | "SetTaskIdentifier" ->
                let data = 
                    if jsonString.Length > 0 then
                        Some(JsonSerializer.Deserialize<string>(jsonString))
                    else
                        None
                StandardArgument.SetTaskIdentifier data
            | "SetTaskItem" ->
                let data = JsonSerializer.Deserialize<TaskItem option>(jsonString)
                StandardArgument.SetTaskItem data
            | "SetWorkingDirectory" ->
                let data = 
                    if jsonString.Length > 0 then
                        let dirPath = JsonSerializer.Deserialize<string>(jsonString)
                        Some(DirectoryInfo(dirPath))
                    else
                        None
                StandardArgument.SetWorkingDirectory data
            | "SetSaveFlags" ->
                let flags =
                    if jsonString.Length > 0 then
                        let serializedData = JsonSerializer.Deserialize<string>(jsonString)
                        Some(SaveFlags(serializedData, flagResolverDictionary))
                    else
                        None
                StandardArgument.SetSaveFlags flags
            | "SetJsonEvents" ->
                let data = JsonSerializer.Deserialize<byte[]>(jsonString)
                StandardArgument.SetJsonEvents data
            | "SetLogFormatEvents" ->
                let data = JsonSerializer.Deserialize<byte[]>(jsonString)
                StandardArgument.SetLogFormatEvents data
            | "SetOpenJsonFilePath" ->
                let path = JsonSerializer.Deserialize<string>(jsonString)
                let fileInfo = new FileInfo(path)
                StandardArgument.SetOpenJsonFilePath fileInfo
            | "SetSaveJsonFilePath" ->
                let path = JsonSerializer.Deserialize<string>(jsonString)
                let fileInfo = new FileInfo(path)
                StandardArgument.SetSaveJsonFilePath fileInfo
            | "SetOpenLogFormatFilePath" ->
                let path = JsonSerializer.Deserialize<string>(jsonString)
                let fileInfo = new FileInfo(path)
                StandardArgument.SetOpenLogFormatFilePath fileInfo
            | "SetSaveLogFormatFilePath" ->
                let path = JsonSerializer.Deserialize<string>(jsonString)
                let fileInfo = new FileInfo(path)
                StandardArgument.SetSaveLogFormatFilePath fileInfo

            | _ -> invalidArg "propertyName" (sprintf "Property %s was not recognized" propertyName)

        StandardShareIntraModule(standardArgument)

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
    let mutable taskIdentifierArgument = StandardArgument.SetTaskIdentifier(None)
    let mutable taskItemArgument = StandardArgument.SetTaskItem(None)
    let mutable workingDirectoryArgument = StandardArgument.SetWorkingDirectory(None)
    let mutable saveFlagsArgument = StandardArgument.SetSaveFlags(None)

    interface IKnownArguments with
        member this.KnownRequests with get() =
            seq {
                StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier);
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
            | RequestTaskIdentifier -> taskIdentifierArgument.toTaskEvent()
            | RequestTaskItem -> taskItemArgument.toTaskEvent()
            | RequestWorkingDirectory -> workingDirectoryArgument.toTaskEvent()
            | RequestSaveFlags -> saveFlagsArgument.toTaskEvent()
            | _ -> invalidArg "request" "Unexpected request type (do you mean to use StandardKnownArguments?)"

    member this.SetValue(request, value:obj) =
        match request with
        | RequestTaskIdentifier ->
            match value with
            | :? string -> taskIdentifierArgument <- StandardArgument.SetTaskIdentifier(Some(value :?> string))
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

module public PatternMatchers =

    let lookupArgument (key: StandardRequestIntraModule) (argMap: Map<IRequestIntraModule, IShareIntraModule>) = 
        if argMap.ContainsKey key then
            Some((argMap.[key] :?> StandardShareIntraModule).Argument)
        else
            None    // key not set

    let private (| Notifications |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestNotifications)
        match (lookupArgument key argMap) with
        | Some(SetNotificationsList arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a NotificationsList"

    let private (| TaskItem |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestTaskItem)
        match (lookupArgument key argMap) with
        | Some(SetTaskItem arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TaskItem"

    let private (| TaskIdentifier |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestTaskIdentifier)
        match (lookupArgument key argMap) with
        | Some(SetTaskIdentifier arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a TaskIdentifier"

    let private (| WorkingDirectory |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestWorkingDirectory)
        match (lookupArgument key argMap) with
        | Some(SetWorkingDirectory arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting a WorkingDirectory"
    
    let private (| SaveFlags |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestSaveFlags)
        match (lookupArgument key argMap) with
        | Some(SetSaveFlags arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting SaveFlags"

    let private (| JsonEvents |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestJsonEvents)
        match (lookupArgument key argMap) with
        | Some(SetJsonEvents arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting JsonEvents"

    let private (| LogFormatEvents |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestLogFormatEvents)
        match (lookupArgument key argMap) with
        | Some(SetLogFormatEvents arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting LogFormatEvents"

    let private (| OpenJsonFilePath |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestOpenJsonFilePath)
        match (lookupArgument key argMap) with
        | Some(SetOpenJsonFilePath arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an OpenJsonFilePath"

    let private (| SaveJsonFilePath |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestSaveJsonFilePath)
        match (lookupArgument key argMap) with
        | Some(SetSaveJsonFilePath arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an SaveJsonFilePath"

    let private (| OpenLogFormatFilePath |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestOpenLogFormatFilePath)
        match (lookupArgument key argMap) with
        | Some(SetOpenLogFormatFilePath arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an OpenLogFormatFilePath"

    let private (| SaveLogFormatFilePath |) argMap =
        let key = StandardRequestIntraModule(StandardRequest.RequestSaveLogFormatFilePath)
        match (lookupArgument key argMap) with
        | Some(SetSaveLogFormatFilePath arg) -> Some(arg)
        | None -> None
        | _ -> invalidArg "arg" "Expecting an SaveLogFormatFilePath"
    
    let getNotifications argMap = match (argMap) with | Notifications arg -> arg

    let getTaskItem argMap = match (argMap) with | TaskItem arg -> arg

    let getTaskIdentifier argMap = match (argMap) with | TaskIdentifier arg -> arg

    let getWorkingDirectory argMap = match (argMap) with | WorkingDirectory arg -> arg
    
    let getSaveFlags argMap = match (argMap) with | SaveFlags arg -> arg

    let getJsonEvents argMap = match (argMap) with | JsonEvents arg -> arg

    let getLogFormatEvents argMap = match (argMap) with | LogFormatEvents arg -> arg

    let getOpenJsonFilePath argMap = match (argMap) with | OpenJsonFilePath arg -> arg

    let getSaveJsonFilePath argMap = match (argMap) with | SaveJsonFilePath arg -> arg

    let getOpenLogFormatFilePath argMap = match (argMap) with | OpenLogFormatFilePath arg -> arg

    let getSaveLogFormatFilePath argMap = match (argMap) with | SaveLogFormatFilePath arg -> arg
