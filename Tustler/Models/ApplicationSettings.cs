using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.Configuration;

namespace Tustler.Models
{
    public static class ApplicationSettings
    {
        /// <summary>
        /// Dynamically creates a FormattableString using embedded expressions that are assumed to return a string type
        /// </summary>
        /// <param name="interpolatedStringFormat">A string in 'string interpolation'format (with embedded expressions)</param>
        /// <returns>A FormattableString with embedded expressions evaluated</returns>
        /// <example>The string "{System.AppContext.BaseDirectory}" returns the base directory path</example>
        private static async Task<FormattableString> Create(string interpolatedStringFormat)
        {
            var matches = Regex.Matches(interpolatedStringFormat, @"{(.+?)}");
            if (matches.Count > 0)
            {
                var expressions = (matches as IEnumerable<Match>).Select(match => CSharpScript.EvaluateAsync<string>(match.Groups[1].Value));
                var evaluated = await Task.WhenAll<string>(expressions).ConfigureAwait(false);

                // create a standard format string
                int count = 0;
                var format = Regex.Replace(interpolatedStringFormat, @"{(.+?)}", match =>
                {
                    return $"{{{count++}}}";
                });
                return FormattableStringFactory.Create(format, evaluated);
            }
            else
            {
                return FormattableStringFactory.Create(interpolatedStringFormat, null);
            }
        }

        public static string FileCachePath
        {
            get
            {
                var interpolatedStringFormat = GetAppConfig().GetValue<string>("FileCache");
                FormattableString unresolvedFileCachePath = Task.Run(() => Create(interpolatedStringFormat)).GetAwaiter().GetResult();
                var fileCachePath = FormattableString.Invariant(unresolvedFileCachePath);

                //// json format required forward slash; convert back to standard MS path format
                //return fileCachePath.Replace(@"/", @"\", StringComparison.InvariantCulture);
                return fileCachePath;
            }
        }

        public static string DefaultBucketName
        {
            get
            {
                return GetAppConfig().GetValue<string>("DefaultBucketName");
            }
        }

        public static string BatchTranslateServiceRole
        {
            get
            {
                return GetAppConfig().GetValue<string>("BatchTranslateServiceRole");
            }
        }

        public static string BatchTranslateRegion
        {
            get
            {
                return GetAppConfig().GetValue<string>("BatchTranslateRegion");
            }
        }

        public static string BatchTranslateInputFolder
        {
            get
            {
                return GetAppConfig().GetValue<string>("BatchTranslateInputFolder");
            }
        }

        public static string BatchTranslateOutputFolder
        {
            get
            {
                return GetAppConfig().GetValue<string>("BatchTranslateOutputFolder");
            }
        }

        public static string NotificationsARN
        {
            get
            {
                return GetAppConfig().GetValue<string>("NotificationsARN");
            }
        }

        public static string NotificationsQueue
        {
            get
            {
                return GetAppConfig().GetValue<string>("NotificationsQueue");
            }
        }

        private static IConfigurationRoot GetAppConfig()
        {
            App current = Application.Current as App;
            return current.AppConfig;
        }
    }
}
