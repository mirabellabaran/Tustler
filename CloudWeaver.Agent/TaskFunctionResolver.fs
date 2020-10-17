namespace CloudWeaver

open System
open System.Collections.Generic
open System.Reflection
open CloudWeaver.Types
open TustlerServicesLib
open System.IO

type ResolverCachedItem(methodInfo: MethodInfo, taskFunctionSpecifier: TaskFunctionSpecifier) =

    let mutable cachedDelegate : Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> option = None

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
                let assemblyNames = AppDomain.CurrentDomain.GetAssemblies() |> Seq.map (fun asm -> asm.FullName)
                new HashSet<string>(assemblyNames)

            let assemblyFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, TaskFunctionModulePrefix, SearchOption.TopDirectoryOnly)

            assemblyFiles
            |> Seq.map (fun assemblyFile -> Path.GetFileNameWithoutExtension(assemblyFile))
            |> Seq.filter (fun baseAssemblyName ->  // skip already loaded assemblies
                let isLoaded = loadedAssemblies |> Seq.exists(fun fullName -> fullName.StartsWith(baseAssemblyName, StringComparison.InvariantCulture))

                not isLoaded
            )
            |> Seq.map (fun (baseAssemblyName: string) ->
                let assembly = Assembly.Load(baseAssemblyName)

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

    member this.CreateDelegate(taskFunctionSpecifier: TaskFunctionSpecifier) =
        
        let taskFullPath = taskFunctionSpecifier.TaskFullPath
        if taskLookup.ContainsKey(taskFullPath) then
            let ri = taskLookup.[taskFullPath]
            if ri.CachedDelegate.IsSome then
                ri.CachedDelegate.Value
            else
                let mi = taskLookup.[taskFullPath].MethodInfo
                let funcType = typeof<Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>>>
                let del = Delegate.CreateDelegate(funcType, mi, true) :?> Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>>
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