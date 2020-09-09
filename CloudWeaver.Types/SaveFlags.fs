namespace CloudWeaver.Types

open System.Collections.Generic
open Microsoft.FSharp.Reflection

type ISaveFlag =
    inherit System.IComparable
    abstract member Identifier: string with get

type ISaveFlagSet =
    inherit System.IComparable
    abstract member Identifier: string with get
    abstract member SetFlag: ISaveFlag -> unit
    abstract member IsSet: ISaveFlag -> bool

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
        member this.Identifier with get() = CommonUtilities.toString stdFlag

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

type SaveFlags(flagSets: ISaveFlagSet[]) =

    let mutable _flagSets =
        if isNull(flagSets) then
            Set.empty
        else
            flagSets |> Set.ofArray

    new() = SaveFlags(null)

    new(serializedData: string, flagResolver: string -> Dictionary<string, ISaveFlagSet> -> Dictionary<string, ISaveFlagSet>) =
        //let flagResolver = serviceProvider.GetService(typeof<AmazonWebServiceInterface>())
        // MG remove reference to DependencyInjection package
        let flagsDictionary =
            let flagItems = serializedData.Split(",")
            flagItems
            |> Seq.fold (fun acc item ->
                flagResolver item acc
            ) (Dictionary(2))   // one key-value pair for each known module flagset type (e.g. StandardFlagSet, AWSlagSet, ...)

        if flagsDictionary.Count > 0 then
            let flagSets =
                flagsDictionary
                |> Seq.map (fun kvp -> kvp.Value)
                |> Seq.toArray
            SaveFlags(flagSets)
        else
            SaveFlags()

    override this.ToString(): string = System.String.Join(", ", (flagSets |> Seq.map(fun flagSet -> flagSet.Identifier)))
    member this.AddFlagSet (flagSet: ISaveFlagSet) = if not (_flagSets.Contains flagSet) then _flagSets <- _flagSets.Add flagSet
    member this.IsSet flag =
        _flagSets
        |> Seq.exists (fun flagSet -> flagSet.IsSet flag)
