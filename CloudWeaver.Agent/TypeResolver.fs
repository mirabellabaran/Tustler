namespace CloudWeaver

open System.Collections.Generic
open System.Reflection
open System.IO
open System
open System.Text.Json
open CloudWeaver.Types
open System.Text.Json.Serialization

type TypeResolverKnownMethod(delegateType: Type) =

    member this.DelegateType with get() = delegateType

type TypeResolverCachedMethod(methodInfo: MethodInfo, cachedDelegate: Delegate) =

    member this.MethodInfo with get() = methodInfo

    member this.CachedDelegate with get() = cachedDelegate

type TypeResolverCachedItem(cachedType: Type, interfaceType: Type) =

    static let knownMethods : Lazy<Map<string, Map<string, TypeResolverKnownMethod>>> = lazy (
        seq {
            (
                "IRequestIntraModule",
                [|
                    ("FromString", typeof<Func<string, IRequestIntraModule>>)
                |]
            );
            (
                "IShareIntraModule",
                [|
                    ("Deserialize", typeof<Func<string, string, JsonSerializerOptions, IShareIntraModule>>)
                |]
            );
            (
                "IShareIterationArgument",
                [|
                    ("Deserialize", typeof<Func<JsonElement, IShareIterationArgument>>)
                |]
            );
            (
                "TypeResolverHelper",
                [|
                    ("GetMatchingArgument", typeof<Func<IRequestIntraModule, string>>);
                    ("GetRequestAsString", typeof<Func<IRequestIntraModule, string>>);
                    ("CreateRequest", typeof<Func<string, IRequestIntraModule>>)
                    ("GenerateTypeRepresentation", typeof<Func<IRequestIntraModule, Func<string, string, string, Action<Utf8JsonWriter>, string, string>, string>>);
                    ("CreateSerializedArgument", typeof<Func<string, obj, byte[]>>)
                    ("UnwrapInstance", typeof<Func<IShareIntraModule, obj>>)
                    ("CreateRetainingStack", typeof<Func<Guid, seq<IShareIterationArgument>, RetainingStack>>)
                    ("AddFlag", typeof<Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>>)
                |]
            )
        }
        |> Seq.map (fun (interfaceName, methods) ->
            let methodMap =
                methods
                |> Seq.map (fun (name, delegateType) -> name, TypeResolverKnownMethod(delegateType))
                |> Map.ofSeq
            interfaceName, methodMap
        )
        |> Map.ofSeq
    )

    let mutable cachedMethods: Map<string, TypeResolverCachedMethod> = Map.empty

    /// Map of delegate types by known method name
    member this.KnownMethods with get() =
        let key = if isNull interfaceType then cachedType.Name else interfaceType.Name
        knownMethods.Value.[key]

    member this.CachedMethods with get() = cachedMethods and set(value) = cachedMethods <- value

    member this.CachedType with get() = cachedType

    member this.IsConverter with get() =
        match cachedType.BaseType.Name with
        | "JsonConverter`1" -> true
        | _ -> false

type TypeResolver private (typeMap: KeyValuePair<string, TypeResolverCachedItem> list) =

    [<Literal>]
    static let TaskFunctionModulePrefix = "CloudWeaver*.dll"     // the name prefix of assemblies which can be searched for Task Function modules

    [<Literal>]
    static let TypeResolverHelper = "TypeResolverHelper"        // the name of the helper type in each module

    [<Literal>]
    static let MethodNameGetMatchingArgument = "GetMatchingArgument"
    
    [<Literal>]
    static let MethodNameGetRequestAsString = "GetRequestAsString"
    
    [<Literal>]
    static let MethodNameCreateRequest= "CreateRequest"

    [<Literal>]
    static let MethodNameGenerateTypeRepresentation = "GenerateTypeRepresentation"
    
    [<Literal>]
    static let MethodNameCreateSerializedArgument = "CreateSerializedArgument"

    [<Literal>]
    static let MethodNameUnwrapInstance = "UnwrapInstance"

    [<Literal>]
    static let MethodNameCreateRetainingStack = "CreateRetainingStack"

    let typeLookup = new Dictionary<string, TypeResolverCachedItem>(typeMap)

    let typeKeyAssemblyLookup =                 // keyed by type full name -> assembly name
        let pairs =
            typeMap
            |> Seq.map (fun kvp -> KeyValuePair<string, string>(kvp.Key, kvp.Value.CachedType.Assembly.GetName().Name))
        Dictionary<string, string>(pairs)

    // load CloudWeaver assemblies
    static let loadAssemblies () =

        let loadedAssemblies =
            AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.fold (fun accum asm ->
                Map.add asm.FullName asm accum
            ) Map.empty

        let assemblyFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, TaskFunctionModulePrefix, SearchOption.TopDirectoryOnly)

        assemblyFiles
        |> Seq.map (fun assemblyFile -> Path.GetFileNameWithoutExtension(assemblyFile))
        |> Seq.map (fun baseAssemblyName ->  // load assembly if not already loaded
            let key = loadedAssemblies |> Map.tryFindKey (fun fullName _ -> fullName.StartsWith(baseAssemblyName, StringComparison.InvariantCulture))
            if key.IsSome then
                loadedAssemblies.[key.Value]
            else
                Assembly.Load(baseAssemblyName)
        )

    static let instance = lazy (

        let isImplementationOf (interfaceType: Type) (exportedType: Type) =
            if interfaceType.IsAssignableFrom(exportedType) then
                if not (isNull (exportedType.GetInterface(interfaceType.FullName))) then
                    Some(exportedType, interfaceType)
                else
                    None
            else
                None

        let (|Request|_|) (exportedType: Type) =
            let interfaceType = typeof<CloudWeaver.Types.IRequestIntraModule>
            isImplementationOf interfaceType exportedType

        let (|Argument|_|) (exportedType: Type) =
            let interfaceType = typeof<CloudWeaver.Types.IShareIntraModule>
            isImplementationOf interfaceType exportedType

        let (|IterationArgument|_|) (exportedType: Type) =
            let interfaceType = typeof<CloudWeaver.Types.IShareIterationArgument>
            isImplementationOf interfaceType exportedType

        let (|SaveFlag|_|) (exportedType: Type) =
            let interfaceType = typeof<CloudWeaver.Types.ISaveFlag>
            isImplementationOf interfaceType exportedType

        let (|Converter|_|) (exportedType: Type) =
            if isNull exportedType.BaseType then
                None
            else
                if exportedType.BaseType.Name = "JsonConverter`1" then
                    Some(exportedType, typeof<JsonConverter>)
                else
                    None

        let (|Helper|_|) (exportedType: Type) =
            if exportedType.Name = TypeResolverHelper then
                Some(exportedType, null)
            else
                None

        // get the mapping between exported types and their assemblies
        let typeMap =
            loadAssemblies ()
            //|> Seq.filter (fun assembly -> assembly.GetName().Name = "CloudWeaver.MediaServices")
            |> Seq.map (fun assembly ->
                assembly.GetExportedTypes()
                |> Seq.map (fun exportedType ->
                    match exportedType with
                    | Request ti -> Some(ti)
                    | Argument ti -> Some(ti)
                    | IterationArgument ti -> Some(ti)
                    | SaveFlag ti -> Some(ti)
                    | Converter ti -> Some(ti)
                    | Helper ti -> Some(ti)
                    | _ -> None
                )
                |> Seq.choose id
                |> Seq.map (fun (exportedType, interfaceType) ->
                    KeyValuePair<string, TypeResolverCachedItem>(exportedType.FullName, TypeResolverCachedItem(exportedType, interfaceType))
                )
            )
            |> Seq.concat
            |> Seq.toList

        TypeResolver(typeMap)
    )

    /// Create an instance of the TypeResolver
    static member Create() =

        async { return instance.Force() } |> Async.StartImmediateAsTask

    /// Get CloudWeaver assemblies
    member this.GetAssemblies() = loadAssemblies ()

    /// Get all of the cached types of type JsonConverter
    member this.GetAllConverters() =
        typeLookup
        |> Seq.filter (fun kvp -> kvp.Value.IsConverter)
        |> Seq.map (fun kvp -> kvp.Value.CachedType)

    member this.GetHelperDelegate(request: IRequestIntraModule, methodName: string) =
        let typeName = request.GetType().FullName
        this.GetHelperDelegateFromType(typeName, methodName, request.ToString())

    /// For the type name argument, any known type in the same assembly will do
    member this.GetHelperDelegateFromType(typeName: string, methodName: string, argumentType: string) =
        if typeKeyAssemblyLookup.ContainsKey (typeName) then
            let assemblyName = typeKeyAssemblyLookup.[typeName]
            this.GetHelperDelegateFromAssembly(assemblyName, methodName, argumentType)
        else
            invalidArg "request" (sprintf "Unknown type name (%s) in module implementing: %s" typeName argumentType)

    member this.GetHelperDelegateFromAssembly(assemblyName: string, methodName: string, argumentType: string) =
        let key = sprintf "%s.%s" assemblyName TypeResolverHelper
        if typeLookup.ContainsKey(key) then
            let helper = typeLookup.[key].CachedType
            if typeLookup.[key].KnownMethods.ContainsKey(methodName) then
                this.ResolveStaticCall(helper.FullName, methodName)
            else
                invalidArg "methodName" (sprintf "Unknown method name (%s) in module implementing: %s" methodName argumentType)
        else
            invalidOp (sprintf "TypeResolverHelper not found in module implementing: %s" argumentType)

    /// Get a delegate to the given static method within the specified type
    member this.ResolveStaticCall(typeName: string, methodName: string) =

        if typeLookup.ContainsKey typeName then
            let cachedItem = typeLookup.[typeName]

            // is the method known?
            if cachedItem.KnownMethods.ContainsKey(methodName) then
                // is the method cached?
                if cachedItem.CachedMethods.ContainsKey(methodName) then
                    cachedItem.CachedMethods.[methodName].CachedDelegate
                else
                    let methodInfo = cachedItem.CachedType.GetMethod(methodName, BindingFlags.Public ||| BindingFlags.Static)

                    let delegateType = cachedItem.KnownMethods.[methodName].DelegateType
                    let del = Delegate.CreateDelegate(delegateType, methodInfo, true)
                    
                    cachedItem.CachedMethods <- Map.add methodName (TypeResolverCachedMethod(methodInfo, del)) cachedItem.CachedMethods
                    del
            else
                invalidArg "methodName" "Unknown method name"
        else
            invalidArg "typeName" "Unknown type name"

    /// Get the string representation of the argument type that matches this request
    member this.GetMatchingArgument(request: IRequestIntraModule) =

        let del = this.GetHelperDelegate(request, MethodNameGetMatchingArgument) :?> Func<IRequestIntraModule, string>
        del.Invoke(request)

    /// Get the string representation of the specified request type
    member this.GetRequestAsString(request: IRequestIntraModule) =

        let del = this.GetHelperDelegate(request, MethodNameGetRequestAsString) :?> Func<IRequestIntraModule, string>
        del.Invoke(request)

    /// Construct a request of the given type from the specified request module
    // e.g. "CloudWeaver.Types.StandardRequestIntraModule" "RequestTaskIdentifier"
    member this.CreateRequest(requestModuleName: string, requestType: string) =

        let del = this.GetHelperDelegateFromType(requestModuleName, MethodNameCreateRequest, "; module not found") :?> Func<string, IRequestIntraModule>
        del.Invoke(requestType)

    /// Generate a serialized representation of the underlying type for a Request using the specified generator function
    member this.GenerateTypeRepresentation(request: IRequestIntraModule, generator: Func<string, string, string, Action<Utf8JsonWriter>, string, string>) =

        let del = this.GetHelperDelegate(request, MethodNameGenerateTypeRepresentation) :?> Func<IRequestIntraModule, Func<string, string, string, Action<Utf8JsonWriter>, string, string>, string>
        del.Invoke(request, generator)

    /// Return a serialized instance of the argument corresponding to the specified request module and type
    member this.CreateSerializedArgument(requestModuleName: string, requestType: string, arg: obj) =

        let del = this.GetHelperDelegateFromType(requestModuleName, MethodNameCreateSerializedArgument, "; module not found") :?> Func<string, obj, byte[]>
        del.Invoke(requestType, arg)

    /// Unwrap an argument type and return the underlying value
    member this.UnwrapArgument(argument: IShareIntraModule) =
        let typeName = argument.GetType().FullName

        let del = this.GetHelperDelegateFromType(typeName, MethodNameUnwrapInstance, argument.ToString()) :?> Func<IShareIntraModule, obj>
        del.Invoke(argument)

    /// Create a retaining stack that wraps iteration arguments
    member this.CreateRetainingStack(iterationArgumentTypeName: string, identifier: Guid, items: seq<IShareIterationArgument>) =
        let assemblyName =
            let matchingKeys =
                typeKeyAssemblyLookup.Keys
                |> Seq.filter (fun typeFullName -> typeFullName.Contains(iterationArgumentTypeName))
                |> Seq.toArray
            match matchingKeys.Length with
            | 0 -> invalidArg "iterationArgumentTypeName" (sprintf "Iteration argument type not found: %s" iterationArgumentTypeName)
            | 1 -> typeKeyAssemblyLookup.[matchingKeys.[0]]
            | _ -> invalidArg "iterationArgumentTypeName" (sprintf "Multiple matches found for iteration argument type: %s" iterationArgumentTypeName)

        let del = this.GetHelperDelegateFromAssembly(assemblyName, MethodNameCreateRetainingStack, iterationArgumentTypeName) :?> Func<Guid, seq<IShareIterationArgument>, RetainingStack>
        del.Invoke(identifier, items)

    /// Add a module-specific flag to the specified flag dictionary
    member this.AddFlag(flagTypeName: string, serializedFlagItem: string, source: Dictionary<string, ISaveFlagSet>) =
        let assemblyName =
            let matchingKeys =
                typeKeyAssemblyLookup.Keys
                |> Seq.filter (fun typeFullName -> typeFullName.Contains(flagTypeName))
                |> Seq.toArray
            match matchingKeys.Length with
            | 0 -> invalidArg "iterationArgumentTypeName" (sprintf "Flag type not found: %s" flagTypeName)
            | 1 -> typeKeyAssemblyLookup.[matchingKeys.[0]]
            | _ -> invalidArg "iterationArgumentTypeName" (sprintf "Multiple matches found for flag type: %s" flagTypeName)

        let del = this.GetHelperDelegateFromAssembly(assemblyName, MethodNameCreateRetainingStack, serializedFlagItem) :?> Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>
        del.Invoke(serializedFlagItem, source)
