using CloudWeaver;
using CloudWeaver.Foundation.Types;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TustlerAWSLib;
using TustlerFFMPEG;
using TustlerInterfaces;
using TustlerServicesLib;

namespace Tustler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        private static readonly ILog log = LogManager.GetLogger(typeof(App));

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            log.Info("        =============  Started Logging  =============        ");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            var appSettingsPath = ApplicationSettings.AppSettingsFilePath;
            if (!File.Exists(appSettingsPath))
            {
                appSettingsPath = Path.Combine(System.AppContext.BaseDirectory, ApplicationSettings.AppSettingsFileName);
                if (!File.Exists(appSettingsPath))
                {
                    MessageBox.Show($"Missing configuration file {appSettingsPath}", "Configuration file missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }

            try
            {
                var baseDirectory = Path.GetDirectoryName(appSettingsPath);
                JsonConfiguration.ParseConfiguration(baseDirectory, ApplicationSettings.AppSettingsFileName);
            }
            catch (FormatException)
            {
                MessageBox.Show($"The configuration file is incorrectly formatted: {appSettingsPath}", "Configuration file error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            // set the path to the FFmpeg directory
            Unosquare.FFME.Library.FFmpegDirectory = ApplicationSettings.FFmpegDirectory;

            // create the task function resolver
            var taskFunctionResolver = await TaskFunctionResolver.Create().ConfigureAwait(false);

            // set runtime options
            var runtimeOptions = new RuntimeOptions
            {
                NotificationsARN = ApplicationSettings.NotificationsARN,
                NotificationsQueueURL = ApplicationSettings.NotificationsQueue
            };

            // prepare for dependency injection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, runtimeOptions, taskFunctionResolver);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services, RuntimeOptions runtimeOptionsInstance, TaskFunctionResolver taskFunctionResolverInstance)
        {
            // dependent services
            services.AddSingleton(taskFunctionResolverInstance);
            services.AddSingleton<TaskLogger>();
            services.AddSingleton(runtimeOptionsInstance);
            services.AddSingleton(new FFMPEGServiceInterface(runtimeOptionsInstance));
            services.AddSingleton<AmazonWebServiceInterface>();
            services.AddSingleton<MainWindow>();
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

            var taskLogger = ServiceProvider.GetService<TaskLogger>();
            taskLogger.Dispose();

            Application.Current.Shutdown();
        }
    }
}
