namespace CloudWeaver

open System
open System.Collections.Generic
open System.Reflection
open CloudWeaver.Types
open System.IO
open CloudWeaver.Foundation.Types

type ResolverCachedItem(methodInfo: MethodInfo, taskFunctionSpecifier: TaskFunctionSpecifier) =

    let mutable cachedOutputs : Set<IRequestIntraModule> option = None

    let mutable cachedDelegate : Func<TaskFunctionQueryMode, Map<IRequestIntraModule, IShareIntraModule>, IEnumerable<TaskResponse>> option = None

    member this.CachedOutputs with get() = cachedOutputs and set(value) = cachedOutputs <- value

    member this.CachedDelegate with get() = cachedDelegate and set(value) = cachedDelegate <- value

    member this.MethodInfo with get() = methodInfo

    member this.TaskFunctionSpecifier with get() = taskFunctionSpecifier


type TaskFunctionResolver private (pairs: seq<KeyValuePair<string, ResolverCachedItem>>) =

    [<Literal>]
    static let TaskFunctionModulePrefix = "CloudWeaver*.dll"     // the name prefix of assemblies which can be searched for Task Function modules

    let taskLookup = new Dictionary<_,_>(pairs)

    static let instance = lazy (
        let scanModules () =

            let getTaskFunctions (assembly: Assembly) (currentModule: Type) =
                let methods = currentModule.GetMethods(BindingFlags.Public ||| BindingFlags.Static)

                methods
                |> Seq.map (fun mi ->
                    let isRootTask = Attribute.IsDefined(mi, typeof<RootTask>)
                    let enableLogging = Attribute.IsDefined(mi, typeof<EnableLogging>)
                    let specifier = new TaskFunctionSpecifier(assembly.GetName().Name, currentModule.FullName, mi.Name, isRootTask, enableLogging)
                    let key = specifier.TaskFullPath
                    new KeyValuePair<string, ResolverCachedItem>(key, ResolverCachedItem(mi, specifier))
                )

            let loadedAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                |> Seq.fold (fun accum asm ->
                    Map.add asm.FullName asm accum
                ) Map.empty

            // task functions must be found in one of these assembly files
            let assemblyFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, TaskFunctionModulePrefix, SearchOption.TopDirectoryOnly)

            // search each assembly for the attribute that marks a module containing task functions
            assemblyFiles
            |> Seq.map (fun assemblyFile -> Path.GetFileNameWithoutExtension(assemblyFile))
            |> Seq.map (fun baseAssemblyName ->  // load assembly if not already loaded
                let key = loadedAssemblies |> Map.tryFindKey (fun fullName _ -> fullName.StartsWith(baseAssemblyName, StringComparison.InvariantCulture))
                if key.IsSome then
                    loadedAssemblies.[key.Value]
                else
                    Assembly.Load(baseAssemblyName)
            )
            |> Seq.map (fun assembly ->
                assembly.GetExportedTypes()
                |> Seq.map (fun exportedType ->
                    if Attribute.IsDefined(exportedType, typeof<CloudWeaverTaskFunctionModule>) then
                        Some(getTaskFunctions assembly exportedType)
                    else
                        None
                )
                |> Seq.choose id
                |> Seq.concat
            )
            |> Seq.concat

        let pairs = scanModules ()
        TaskFunctionResolver(pairs)
    )

    static member Create() =

        async { return instance.Force() } |> Async.StartImmediateAsTask

    member this.AddFunction(methodinfo: MethodInfo) =

        let functionModule = methodinfo.DeclaringType
        let assembly = functionModule.Assembly
        let taskFunctionSpecifier = new TaskFunctionSpecifier(assembly.FullName, functionModule.FullName, methodinfo.Name, false, false)

        let ri = new ResolverCachedItem(methodinfo, taskFunctionSpecifier)
        taskLookup.Add(taskFunctionSpecifier.TaskFullPath, ri)
        
    member this.FindFunction(methodinfo: MethodInfo) =

        taskLookup.Values
        |> Seq.tryFind (fun ri ->
            ri.MethodInfo = methodinfo
        )

    //member this.AddFunction(taskFunctionSpecifier: TaskFunctionSpecifier) =

    //    let assembly = Assembly.Load(taskFunctionSpecifier.AssemblyName)
    //    let mi =
    //        assembly.GetExportedTypes()
    //        |> Seq.tryFind (fun exportedType -> exportedType.FullName = taskFunctionSpecifier.ModuleName)
    //        |> Option.map (fun exportedType ->
    //            exportedType.GetMethods(BindingFlags.Public ||| BindingFlags.Static)
    //            |> Seq.tryFind (fun mi -> mi.Name = taskFunctionSpecifier.TaskName)
    //        )
    //        |> Option.flatten

    //    if mi.IsSome then
    //        let ri = new ResolverCachedItem(mi.Value, taskFunctionSpecifier)
    //        taskLookup.Add(taskFunctionSpecifier.TaskFullPath, ri)
    //    else
    //        invalidArg "taskFunctionSpecifier" "The specified public static method was not found"

    member this.CreateDelegate(taskFunctionSpecifier: TaskFunctionSpecifier) =
        
        let taskFullPath = taskFunctionSpecifier.TaskFullPath
        if taskLookup.ContainsKey(taskFullPath) then
            let ri = taskLookup.[taskFullPath]
            if ri.CachedDelegate.IsSome then
                ri.CachedDelegate.Value
            else
                let mi = taskLookup.[taskFullPath].MethodInfo
                let funcType = typeof<Func<TaskFunctionQueryMode, Map<IRequestIntraModule, IShareIntraModule>, IEnumerable<TaskResponse>>>
                let del = Delegate.CreateDelegate(funcType, mi, true) :?> Func<TaskFunctionQueryMode, Map<IRequestIntraModule, IShareIntraModule>, IEnumerable<TaskResponse>>
                ri.CachedDelegate <- Some(del)
                del
        else
            invalidArg "taskFunctionSpecifier" "Unknown TaskFunctionSpecifier"

    /// Return an array of TaskFunctionSpecifier (including the HideFromUI attribute)
    member this.GetAllTaskSpecifiers() : (struct (TaskFunctionSpecifier * bool))[] =

            taskLookup.Values
            |> Seq.map (fun cachedItem ->
                let hideFromUI = Attribute.IsDefined(cachedItem.MethodInfo, typeof<HideFromUI>)
                struct (cachedItem.TaskFunctionSpecifier, hideFromUI)
            )
            |> Seq.toArray

    /// Return a dictionary that maps a TaskFunctionSpecifier.TaskFullPath to the associated TaskFunctionSpecifier
    member this.GetTaskFunctionDictionary() =

        let pairs =
            taskLookup
            |> Seq.map (fun kvp ->
                let key, specifier = kvp.Key, kvp.Value.TaskFunctionSpecifier
                KeyValuePair.Create(key, specifier)
            )
        new Dictionary<string, TaskFunctionSpecifier>(pairs)

    /// Return the sequence of request responses needed to satisfy the task functions invoked by a root task
    /// excluding internally resolvable tasks (note that this sequence is normally serialized to disk as default arguments to the root task)
    /// This is used to pre-evaluate the arguments required for the sequence of tasks defined by a root task
    member this.GetRootTaskInputs(rootTask: TaskItem, knownArguments: KnownArgumentsCollection) : seq<IRequestIntraModule> =

        let getFunc (task: TaskItem) =
            let key = task.FullPath
            if taskLookup.ContainsKey key then
                let ri = taskLookup.[key]
                let specifier = ri.TaskFunctionSpecifier
                Some(this.CreateDelegate(specifier))
            else
                None

        let rootFunc = getFunc rootTask
        if rootFunc.IsNone then
            invalidArg "rootTask" "Lookup of root task failed"
        else
            let tasks =
                // expecting a single response (a TaskSequence)
                let taskSequenceResponse =
                    rootFunc.Value.Invoke(TaskFunctionQueryMode.SubTasks, Map.empty)
                    |> Seq.exactlyOne
                match taskSequenceResponse with
                | TaskResponse.TaskSequence tasks -> tasks
                | _ -> invalidArg "taskSequenceResponse" "TaskFunctionQueryMode.SubTasks should return a single TaskResponse.TaskSequence"

            let inputs, _ =
                tasks
                |> Seq.fold (fun (inputs: IRequestIntraModule list, outputs: Set<IRequestIntraModule>) task ->
                
                    let key = task.FullPath
                    if taskLookup.ContainsKey key then
                        let ri = taskLookup.[key]
                        let specifier = ri.TaskFunctionSpecifier
                        let func = this.CreateDelegate(specifier)
                        // outputs accumulate in the Agent events list over the session...
                        let taskInputs =
                            func.Invoke(TaskFunctionQueryMode.Inputs, Map.empty)
                            |> Seq.map (fun response ->
                                match response with
                                | TaskResponse.RequestArgument arg when knownArguments.IsKnownArgument(arg) -> None
                                | TaskResponse.RequestArgument arg -> Some(arg)
                                | _ -> None
                            )
                            |> Seq.choose id
                            |> Seq.filter (fun request -> not (outputs.Contains request))   // ...therefore remove all inputs that match the outputs as seen so far
                            |> Seq.toList
                        let accumlatedInputs =
                            taskInputs
                            |> Seq.rev
                            |> Seq.fold (fun accum request -> request :: accum) inputs
                        let taskOutputs =
                            func.Invoke(TaskFunctionQueryMode.Outputs, Map.empty)
                            |> Seq.map (fun response ->
                                match response with
                                | TaskResponse.RequestArgument arg -> Some(arg)
                                | _ -> None
                            )
                            |> Seq.choose id
                            |> Seq.fold (fun accum request -> Set.add request accum) outputs

                        (accumlatedInputs, taskOutputs)
                    else
                        (inputs, outputs)
                ) (List.empty, Set.empty)

            inputs |> List.toSeq

    /// Return the TaskFullPath of task functions that produce the ouput specified by the request argument
    member this.FindTaskFunctionsWithOutput(output: IRequestIntraModule) =

        taskLookup.Values
        |> Seq.map (fun ri ->
            if ri.CachedOutputs.IsNone then
                let outputs =
                    let specifier = ri.TaskFunctionSpecifier
                    let func = this.CreateDelegate(specifier)
                    func.Invoke(TaskFunctionQueryMode.Outputs, Map.empty)
                    |> Seq.map (fun response ->
                        match response with
                        | TaskResponse.RequestArgument arg -> Some(arg)
                        | _ -> None
                    )
                    |> Seq.choose id
                    |> Set.ofSeq
                ri.CachedOutputs <- Some(outputs)

            if Set.contains output ri.CachedOutputs.Value then
                Some(ri.TaskFunctionSpecifier.TaskFullPath)
            else
                None
        )
        |> Seq.choose id
