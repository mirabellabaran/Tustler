using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace TustlerWinPlatformLib
{
    [GuidAttribute("E4BC2ECF-8852-4F86-9569-7E60A43F9FB5")]
    public static class ApplicationSettings
    {
        public static string AppSettingsFileName = "appsettings.json";

        /// <summary>
        /// Get the path to the application settings file (appsettings.json)
        /// </summary>
        public static string AppSettingsFilePath
        {
            get
            {
                return Path.Combine(UserDataFolder, AppSettingsFileName);
            }
        }

        /// <summary>
        /// Get the application data folder (that MAY hold application settings (appsettings.json))
        /// </summary>
        /// <remarks>Note that this folder is used ONLY if appsettings.json is modified, otherwise appsettings.json in the application folder is used</remarks>
        /// <see cref="https://www.codeproject.com/Tips/370232/Where-should-I-store-my-data"/>
        public static string UserDataFolder
        {
            get
            {
                MemberInfo classInfo = typeof(ApplicationSettings);
                object[] attr = (classInfo.GetCustomAttributes(typeof(GuidAttribute), true));
                Guid appGuid = new Guid((attr[0] as GuidAttribute).Value);

                string folderBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string subFolder = appGuid.ToString("B").ToUpper();
                string appFolder = Path.Combine(folderBase, subFolder);

                return appFolder;
            }
        }

        public static void CreateUserDataFolder()
        {
            var appFolder = UserDataFolder;
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
        }

        public static string FileCachePath
        {
            get
            {
                var fileCachePath = GetAppConfig().GetValue<string>("FileCache");

                if (string.IsNullOrEmpty(fileCachePath))
                    return Path.Combine(System.AppContext.BaseDirectory, "FileCache");
                else
                    //// json format required forward slash; convert back to standard MS path format
                    return fileCachePath.Replace(@"/", @"\", StringComparison.InvariantCulture);
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
            return JsonConfiguration.AppConfig;
        }
    }
}
