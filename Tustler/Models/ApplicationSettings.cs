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

        private static IConfigurationRoot GetAppConfig()
        {
            App current = Application.Current as App;
            return current.AppConfig;
        }
    }
}
