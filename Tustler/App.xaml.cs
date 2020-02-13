using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tustler.Models;

namespace Tustler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IConfigurationRoot appConfig;

        public IConfigurationRoot AppConfig
        {
            get
            {
                return appConfig;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var appSettingsFileName = "appsettings.json";
            var baseDirectory = System.AppContext.BaseDirectory;

            var appSettingsPath = Path.Combine(baseDirectory, appSettingsFileName);
            if (!File.Exists(appSettingsPath))
            {
                PrepareConfigurationFirstTimeExecution(baseDirectory, appSettingsPath);
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppContext.BaseDirectory)
                .AddJsonFile(appSettingsFileName, optional: true, reloadOnChange: true);

            appConfig = builder.Build();

            // set the path to the FFmpeg directory
            Unosquare.FFME.Library.FFmpegDirectory = @"C:\Users\Zev\Downloads\ffmpeg-20191122-27c6c92-win64-shared\bin";
        }

        private void PrepareConfigurationFirstTimeExecution(string baseDirectory, string appSettingsFileName)
        {
            var fileCacheFolderName = "FileCache";
            // default to placing the file cache in the application base directory
            var fileCachePath = Path.Combine(baseDirectory, fileCacheFolderName);
            if (!Directory.Exists(fileCachePath))
            {
                Directory.CreateDirectory(fileCachePath);
            }

            // prepare the configuration (file cache folder path, default S3 bucket name and the ARN for SNS notifications)
            var escapedPath = fileCachePath.Replace(@"\", @"/", StringComparison.InvariantCulture);
            var fileCacheFolderConfig = $"\t\"{fileCacheFolderName}\": \"{escapedPath}\",";
            var defaultBucketConfig = $"\t\"DefaultBucketName\": \"tator\",";
            var notificationsARNConfig = $"\t\"NotificationsARN\": \"poo\"";
            string[] lines = {
                "{",
                fileCacheFolderConfig,
                defaultBucketConfig,
                notificationsARNConfig,
                "}"
            };

            // write the configuration
            using (StreamWriter outputFile = new StreamWriter(appSettingsFileName))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }
        }

    }
}
