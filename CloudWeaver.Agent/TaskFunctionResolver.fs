namespace CloudWeaver

open System
open System.Collections.Generic
open System.Reflection
open CloudWeaver.Types
open TustlerServicesLib
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
                    let enableLogging = Attribute.IsDefined(mi, typeof<EnableLogging>)
                    let specifier = new TaskFunctionSpecifier(assembly.GetName().Name, currentModule.FullName, mi.Name, enableLogging)
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
        let taskFunctionSpecifier = new TaskFunctionSpecifier(assembly.FullName, functionModule.FullName, methodinfo.Name, false)

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
