namespace CloudWeaver

open System
open System.Collections.Generic
open System.Reflection
open CloudWeaver.Types
open TustlerServicesLib
open System.IO

type TaskFunctionResolver(pairs: seq<KeyValuePair<string, (MethodInfo * TaskFunctionSpecifier)>>) =

    let taskLookup = new Dictionary<_,_>(pairs)

    // MG TODO make const (see below)
    let TaskFunctionModulePrefix = "CloudWeaver*.dll"     // the name prefix of assemblies which can be searched for Task Function modules

    // MG TODO make singleton instance

    static member Create() =

        let scanModules () =

            let getTaskFunctions (assembly: Assembly) (currentModule: Type) =
                let methods = currentModule.GetMethods(BindingFlags.Public ||| BindingFlags.Static)

                methods
                |> Seq.map (fun mi ->
                    let enableLogging = Attribute.IsDefined(mi, typeof<EnableLogging>)
                    let specifier = new TaskFunctionSpecifier(assembly.GetName().Name, currentModule.FullName, mi.Name, enableLogging)
                    let key = specifier.TaskFullPath
                    new KeyValuePair<string, (MethodInfo * TaskFunctionSpecifier)>(key, (mi, specifier))
                )

            let loadedAssemblies =
                let assemblyNames = AppDomain.CurrentDomain.GetAssemblies() |> Seq.map (fun asm -> asm.FullName)
                new HashSet<string>(assemblyNames)

            let TaskFunctionModulePrefix = "CloudWeaver*.dll"     // the name prefix of assemblies which can be searched for Task Function modules
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

        async { return TaskFunctionResolver(scanModules ()) } |> Async.StartImmediateAsTask

    member this.GetAllTaskSpecifiers() : (struct (TaskFunctionSpecifier * bool))[] =

            taskLookup.Values
            |> Seq.map (fun (mi, specifier) ->
                let hideFromUI = Attribute.IsDefined(mi, typeof<HideFromUI>)
                struct (specifier, hideFromUI)
            )
            |> Seq.toArray
