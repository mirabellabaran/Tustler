using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TustlerWinPlatformLib
{
    /// <summary>
    /// Used throughout the application to retrieve application settings
    /// </summary>
    public static class JsonConfiguration
    {
        private static IConfigurationRoot appConfig;

        public static IConfigurationRoot AppConfig
        {
            get
            {
                return appConfig;
            }
        }

        public static string FilePathKey
        {
            get
            {
                return "AppSettingsFilePath";
            }
        }

        public static IEnumerable<KeyValuePair<string, string>> KeyValuePairs
        {
            get
            {
                var providers = JsonConfiguration.AppConfig.Providers as List<IConfigurationProvider>;
                var provider = providers[0] as MemoryConfigurationProvider;

                return provider.AsEnumerable();
            }
        }

        /// <summary>
        /// Parse a Json file and build an in-memory representation
        /// </summary>
        /// <param name="basePath">The folder containing the Json file</param>
        /// <param name="appSettingsFileName">The Json configuration filename</param>
        public static void ParseConfiguration(string basePath, string appSettingsFileName)
        {
            // read values from the Json configuration file
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(appSettingsFileName, optional: false, reloadOnChange: false);

            var jsonConfig = builder.Build();

            // fetch each value and build an IEnumerable<KeyValuePair>
            var providers = jsonConfig.Providers as List<IConfigurationProvider>;
            var provider = providers[0] as JsonConfigurationProvider;
            var keys = provider.GetChildKeys(Enumerable.Empty<string>(), null);
            var pairs = new Dictionary<string, string>(keys.Select(key => new KeyValuePair<string, string>(key, jsonConfig.GetValue<string>(key))));

            if (!pairs.ContainsKey(FilePathKey))
            {
                // add the file path value for later lookup
                var filePath = Path.Combine(basePath, appSettingsFileName);
                pairs.Add(FilePathKey, filePath);
            }

            // create an in-memory configuration that allows both read and write
            builder = new ConfigurationBuilder()
                .AddInMemoryCollection(pairs);

            appConfig = builder.Build();
        }

        public static void SaveConfiguration(Dictionary<string, string> keyValuePairs)
        {
            // TODO need to update the current configuration
            // TODO need to set datagrid editor background to a dark colour for readability
            // TODO ApplicationSettingsViewModel HasChanged is not working correctly

            var sb = new StringBuilder("{");
            sb.AppendLine();
            var lines = keyValuePairs.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value.Replace(@"\", @"\\")}\"");    // convert '\' to '\\'
            sb.AppendJoin(",\n", lines);
            sb.AppendLine();
            sb.Append('}');
            sb.AppendLine();

            var filePath = keyValuePairs[FilePathKey];
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
