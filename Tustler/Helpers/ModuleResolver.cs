using CloudWeaver.AWS;
using CloudWeaver.Types;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tustler.Helpers
{
    /// <summary>
    /// Determines which deserialization function to call on which module
    /// </summary>
    /// <remarks>
    /// The standard module (StandardShareIntraModule) includes a SaveFlags argument type which also requires resolving
    /// in that a flag item may be defined in any module e.g. standard vs AWS flag items
    /// </remarks>
    public class ModuleResolver
    {
        private readonly Dictionary<string, Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>> _flagSetLookup;

        /// <summary>
        /// Determine which module to call for seserialization
        /// </summary>
        /// <param name="moduleTag">The name of the module (the type as a string)</param>
        /// <returns></returns>
        public static Func<string, string, IShareIntraModule> ModuleLookup(string moduleTag)
        {
            static Func<string, string, IShareIntraModule> GetStandardResolver()
            {
                var flagSetLookup = new Dictionary<string, Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>>()
                    {
                        { "StandardFlagSet", FoldInStandardValue },
                        { "AWSFlagSet", FoldInAWSValue }
                    };
                return new ModuleResolver(flagSetLookup).Deserialize;
            }

            return moduleTag switch
            {
                "StandardShareIntraModule" => GetStandardResolver(),
                "AWSShareIntraModule" => AWSShareIntraModule.Deserialize,
                _ => throw new ArgumentException($"Unexpected module tag ({moduleTag}) in ModuleLookup")
            };
        }

        public ModuleResolver(Dictionary<string, Func<string, Dictionary<string, ISaveFlagSet>, Dictionary<string, ISaveFlagSet>>> flagSetLookup)
        {
            _flagSetLookup = flagSetLookup;
        }

        public IShareIntraModule Deserialize(string propertyName, string jsonString)
        {
            var resolver = _flagSetLookup[propertyName];

            return StandardShareIntraModule.Deserialize(propertyName, jsonString, resolver);
        }

        static Dictionary<string, ISaveFlagSet> FoldInStandardValue(string serializedFlagItem, Dictionary<string, ISaveFlagSet> source)
        {
            if (StandardFlagItem.GetNames().Contains(serializedFlagItem))
            {
                var flagItem = StandardFlagItem.Create(serializedFlagItem);
                var standardFlag = new StandardFlag(flagItem);

                var standardFlagSet = source["StandardFlagSet"] as StandardFlagSet;
                if (!(standardFlagSet!.IsSet(standardFlag)))
                {
                    standardFlagSet.SetFlag(standardFlag);
                }
            }

            return source;
        }

        static Dictionary<string, ISaveFlagSet> FoldInAWSValue(string serializedFlagItem, Dictionary<string, ISaveFlagSet> source)
        {
            if (AWSFlagItem.GetNames().Contains(serializedFlagItem))
            {
                var flagItem = AWSFlagItem.Create(serializedFlagItem);
                var awsFlag = new AWSFlag(flagItem);

                var awsFlagSet = source["AWSFlagSet"] as AWSFlagSet;
                if (!(awsFlagSet!.IsSet(awsFlag)))
                {
                    awsFlagSet.SetFlag(awsFlag);
                }
            }

            return source;
        }
    }
}
