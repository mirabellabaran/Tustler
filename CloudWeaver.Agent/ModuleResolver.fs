namespace CloudWeaver

open System.Collections.Generic
open System
open CloudWeaver.Types
open Converters
open TustlerServicesLib
open System.Text.Json


/// <summary>
/// Determines which deserialization function to call on which module
/// </summary>
/// <remarks>
/// The standard module (StandardShareIntraModule) includes a SaveFlags argument type which also requires resolving
/// in that a flag item may be defined in any module e.g. standard vs AWS flag items
/// </remarks>
type ModuleResolver (serializerOptions, flagSetLookup: Dictionary<string, Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>>) =

    /// Get the deserializer for Standard modules (wraps the flag module resolver)
    member this.Deserialize with get () =
        Func<_, _, _>(fun propertyName jsonString -> StandardShareIntraModule.Deserialize propertyName jsonString serializerOptions flagSetLookup :> IShareIntraModule)

    ///// Deserialize and add a new Standard flag itme
    //static member FoldInStandardValue (serializedFlagItem: string) (source: Dictionary<string, ISaveFlagSet>) =

    //    if StandardFlagItem.GetNames() |> Seq.contains(serializedFlagItem) then
    //        let flagItem = StandardFlagItem.Create(serializedFlagItem)
    //        let standardFlag = StandardFlag(flagItem)

    //        let standardFlagSet =
    //            if source.ContainsKey("StandardFlagSet") then
    //                (source.["StandardFlagSet"]) :?> StandardFlagSet
    //            else
    //                let flagSet = new StandardFlagSet()
    //                source.Add("StandardFlagSet", flagSet)
    //                flagSet
            
    //        standardFlagSet.SetFlag(standardFlag)

    //    source

    ///// Deserialize and add a new AWS flag itme
    //static member FoldInAWSValue (serializedFlagItem: string) (source: Dictionary<string, ISaveFlagSet>): Dictionary<string, ISaveFlagSet> =

    //    if AWSFlagItem.GetNames() |> Seq.contains(serializedFlagItem) then
    //        let flagItem = AWSFlagItem.Create(serializedFlagItem)
    //        let awsFlag = AWSFlag(flagItem)
            
    //        let awsFlagSet =
    //            if source.ContainsKey("AWSFlagSet") then
    //                (source.["AWSFlagSet"]) :?> AWSFlagSet
    //            else
    //                let flagSet = new AWSFlagSet()
    //                source.Add("AWSFlagSet", flagSet)
    //                flagSet

    //        awsFlagSet.SetFlag(awsFlag)

    //    source

    /// <summary>
    /// Determine which module to call for deserialization
    /// </summary>
    /// <param name="moduleTag">The name of the module (here using the module type as a string)</param>
    /// <returns>A deserialization function appropriate for the module</returns>
    static member ModuleLookup (moduleTag: string) =

        //let ToFunc flagModuleTag (typeResolver: TypeResolver) = 
        //    let flagResolver =
        //        match flagModuleTag with
        //        | "StandardFlag" -> ModuleResolver.FoldInStandardValue
        //        | "AWSFlag" -> ModuleResolver.FoldInAWSValue
        //        | _ -> invalidArg "flagModuleTag" (sprintf "Unexpected flag module tag (%s) in ModuleLookup" flagModuleTag)

        //    Func<_, _, _>(fun (serializedFlagItem: string) (source: Dictionary<string, ISaveFlagSet>) -> flagResolver serializedFlagItem source)

        let toFunc flagModuleTag (typeResolver: TypeResolver) = 
            Func<_, _, _>(fun (serializedFlagItem: string) (source: Dictionary<string, ISaveFlagSet>) -> typeResolver.AddFlag(flagModuleTag, serializedFlagItem, source))

        let getStandardResolver typeResolver serializerOptions =
            let flagSetLookup =
                let pairs = seq {
                    KeyValuePair( "StandardFlag", toFunc "StandardFlag" typeResolver );
                    KeyValuePair( "AWSFlag", toFunc "AWSFlag" typeResolver );
                }
                Dictionary<string, (Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>)>(pairs)
            ModuleResolver(serializerOptions, flagSetLookup).Deserialize

        let getModuleResolver (typeResolver: TypeResolver) typeName serializerOptions =
            let deserialize = typeResolver.ResolveStaticCall(typeName, "Deserialize") :?> Func<string, string, JsonSerializerOptions, IShareIntraModule>
            Func<_, _, _>(fun propertyName jsonString -> deserialize.Invoke(propertyName, jsonString, serializerOptions))

        let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously
        
        let serializerOptions = CreateSerializerOptions(typeResolver)
        serializerOptions.Converters.Add(SentenceChunkerConverter())

        /// Get the deserializer for each module (wraps the flag module resolver for the Standard module)
        match moduleTag with
        | "StandardShareIntraModule" -> getStandardResolver typeResolver serializerOptions
        | "AWSShareIntraModule" -> getModuleResolver typeResolver "CloudWeaver.AWS.AWSShareIntraModule" serializerOptions
        | "AVShareIntraModule" -> getModuleResolver typeResolver "CloudWeaver.MediaServices.AVShareIntraModule" serializerOptions
        | _ -> invalidArg "moduleTag" (sprintf "Unexpected module tag (%s) in ModuleLookup" moduleTag)

