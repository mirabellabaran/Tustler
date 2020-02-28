using log4net;
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
        private static readonly ILog log = LogManager.GetLogger(typeof(App));
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
            log.Info("        =============  Started Logging  =============        ");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

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

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // an awaited task has generated an unobserved exception; record it and set it as observed
            Exception ex = e.Exception as AggregateException;

            log.Error("TaskScheduler_UnobservedTaskException", ex);

            var notifications = this.FindResource("applicationNotifications") as NotificationsList;

            // note that there may be more than one wrapped exception; here just show the first one
            Dispatcher.Invoke(() => notifications.HandleError("TaskScheduler_UnobservedTaskException", ex.InnerException.Message, ex.InnerException));

            e.SetObserved();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            log.Error($"CurrentDomain_UnhandledException: IsTerminating = {e.IsTerminating}", ex);
            Application.Current.Shutdown();
        }

        private void PrepareConfigurationFirstTimeExecution(string baseDirectory, string appSettingsFileName)
        {
            // TODO remove this function because some of the information is private and should be in a config file only

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
            var batchTranslateServiceRole = $"\t\"BatchTranslateServiceRole\": \"arn:aws:iam::261914005867:role/TODO-create-this-role\",";
            var batchTranslateRegion = $"\t\"BatchTranslateRegion\": \"ap-northeast-2\",";
            var batchTranslateInputFolder = $"\t\"BatchTranslateInputFolder\": \"TranslationInput/\",";
            var batchTranslateOutputFolder = $"\t\"BatchTranslateOutputFolder\": \"TranslationOutput/\",";
            var notificationsARNConfig = $"\t\"NotificationsARN\": \"arn:aws:sns:ap-southeast-2:261914005867:TatorNotifications\",";
            var notificationsQueueConfig = $"\t\"NotificationsQueue\": \"https://sqs.ap-southeast-2.amazonaws.com/261914005867/TatorQueue\"";
            string[] lines = {
                "{",
                fileCacheFolderConfig,
                defaultBucketConfig,
                batchTranslateServiceRole,
                batchTranslateRegion,
                batchTranslateInputFolder,
                batchTranslateOutputFolder,
                notificationsARNConfig,
                notificationsQueueConfig,
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
