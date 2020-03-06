using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Windows;

namespace Tustler.Models
{
    public static class ApplicationSettings
    {
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
            App current = Application.Current as App;
            return current.AppConfig;
        }
    }
}
