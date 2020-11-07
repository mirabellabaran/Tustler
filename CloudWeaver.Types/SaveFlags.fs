namespace CloudWeaver.Types

open System.Collections.Generic
open Microsoft.FSharp.Reflection
open System

type ISaveFlag =
    inherit System.IComparable
    abstract member Identifier: string with get

type ISaveFlagSet =
    inherit System.IComparable
    abstract member Identifier: string with get
    abstract member SetFlag: ISaveFlag -> unit
    abstract member IsSet: ISaveFlag -> bool
    abstract member ToString: unit -> string
    abstract member ToArray: unit -> string[]

type StandardFlagItem =
    | SaveTaskName
    with
    static member GetNames () =
        FSharpType.GetUnionCases typeof<StandardFlagItem>
        |> Seq.map (fun caseInfo ->
            caseInfo.Name
        )
        |> Seq.toArray
    static member Create name =
        let unionCase =
            FSharpType.GetUnionCases typeof<StandardFlagItem>
            |> Seq.find (fun unionCase -> unionCase.Name = name)
        FSharpValue.MakeUnion (unionCase, Array.empty) :?> StandardFlagItem

type StandardFlag(stdFlag: StandardFlagItem) =
    interface ISaveFlag with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> ISaveFlag).Identifier
            let str2 = (obj :?> ISaveFlag).Identifier
            System.String.Compare(str1, str2)
        member this.Identifier with get() = BaseUtilities.toString stdFlag

type StandardFlagSet(flags: StandardFlagItem[]) =
    let mutable _set =
        if isNull(flags) then
            Set.empty
        else
            flags |> Seq.map (fun flagItem -> (StandardFlag(flagItem) :> ISaveFlag)) |> Set.ofSeq

    new() = StandardFlagSet(null)

    member this.Identifier with get() = "StandardFlag"

    member this.SetFlag (flag: ISaveFlag) =
        match flag with
        | :? StandardFlag -> if not (_set.Contains flag) then _set <- _set.Add flag
        | _ -> invalidArg "flag" (sprintf "%s is not a StandardFlag item" flag.Identifier)

    member this.IsSet (flag: ISaveFlag) =
        match flag with
        | :? StandardFlag -> _set.Contains flag
        | _ -> false

    interface ISaveFlagSet with
        member this.CompareTo(obj: obj): int = 
            let str1 = (this :> ISaveFlagSet).Identifier
            let str2 = (obj :?> ISaveFlagSet).Identifier
            System.String.Compare(str1, str2)
        member this.Identifier with get() = this.Identifier
        member this.SetFlag flag = this.SetFlag flag
        member this.IsSet flag = this.IsSet flag
        override this.ToString(): string = System.String.Join(", ", (_set |> Seq.map(fun flag -> sprintf "%s.%s" this.Identifier flag.Identifier)))
        member this.ToArray(): string [] = (_set |> Seq.map(fun flag -> sprintf "%s.%s" this.Identifier flag.Identifier) |> Seq.toArray)

type SaveFlags(flagSets: ISaveFlagSet[]) =

    let mutable _flagSets =
        if isNull(flagSets) then
            Set.empty
        else
            flagSets |> Set.ofArray

    new() = SaveFlags(null)

    new(serializedData: string, flagResolverDictionary) =

        let getResolver flagSet (flagResolverDictionary: Dictionary<string, Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>>) =
            let resolver (wrappedResolver: Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>) (serializedFlagItem: string) (flagSets: Dictionary<string, ISaveFlagSet>) : Dictionary<string, ISaveFlagSet> =
                wrappedResolver.Invoke(serializedFlagItem, flagSets)
            let resolverFunc = flagResolverDictionary.[flagSet]
            resolver resolverFunc

        let flagsDictionary =
            serializedData.Split(",")
            |> Seq.map (fun flagItem -> flagItem.Trim())
            |> Seq.fold (fun acc item ->
                let flagSet, flagItem =
                    let parts = item.Split(".")
                    parts.[0], parts.[1]
                let flagResolver = getResolver flagSet flagResolverDictionary
                flagResolver flagItem acc
            ) (Dictionary(2))   // one key-value pair for each known module flagset type (e.g. StandardFlagSet, AWSlagSet, ...)

        if flagsDictionary.Count > 0 then
            let flagSets =
                flagsDictionary
                |> Seq.map (fun kvp -> kvp.Value)
                |> Seq.toArray
            SaveFlags(flagSets)
        else
            SaveFlags()

    override this.ToString(): string = System.String.Join(", ", (flagSets |> Seq.map(fun flagSet -> flagSet.ToString())))
    member this.ToArray(): string[] = (flagSets |> Seq.map(fun flagSet -> flagSet.ToArray()) |> Seq.concat |> Seq.toArray)
    member this.AddFlagSet (flagSet: ISaveFlagSet) = if not (_flagSets.Contains flagSet) then _flagSets <- _flagSets.Add flagSet
    member this.IsSet flag =
        _flagSets
        |> Seq.exists (fun flagSet -> flagSet.IsSet flag)
