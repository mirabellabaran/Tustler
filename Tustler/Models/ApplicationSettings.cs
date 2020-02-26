using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Configuration;

namespace Tustler.Models
{
    public static class ApplicationSettings
    {
        public static string FileCachePath
        {
            get
            {
                var fileCachePath = GetAppConfig().GetValue<string>("FileCache");

                // json format required forward slash; convert back to standard MS path format
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

        public static string DefaultUserARN
        {
            get
            {
                return GetAppConfig().GetValue<string>("DefaultUserARN");
            }
        }

        public static string TranslateInputFolder
        {
            get
            {
                return GetAppConfig().GetValue<string>("TranslateInputFolder");
            }
        }

        public static string TranslateOutputFolder
        {
            get
            {
                return GetAppConfig().GetValue<string>("TranslateOutputFolder");
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
