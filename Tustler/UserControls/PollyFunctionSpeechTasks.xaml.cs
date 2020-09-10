using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Tustler.Helpers;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerModels;
using TustlerServicesLib;
using AppSettings = TustlerServicesLib.ApplicationSettings;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for PollyFunctionSpeechTasks.xaml
    /// </summary>
    public partial class PollyFunctionSpeechTasks : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;
        private DispatcherTimer timer = null;

        public PollyFunctionSpeechTasks(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var voicesInstance = this.FindResource("voicesInstance") as VoicesViewModel;

            string languageCode = null;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // refresh and then enable the headers
                await voicesInstance.Refresh(awsInterface, notifications, languageCode).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void StartSpeechTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbTextFilePath.Text)
                && File.Exists(tbTextFilePath.Text);
        }

        private async void StartSpeechTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var speechTasksInstance = this.FindResource("speechTasksInstance") as SpeechSynthesisTasksViewModel;

                string bucketName = AppSettings.DefaultBucketName;
                string baseKey = "PollyTaskOutput";         // Polly appends the task Id (a GUID) and the file extension
                string arn = AppSettings.NotificationsARN;
                string filePath = tbTextFilePath.Text;
                bool useNeural = (string)(cbEngine.SelectedItem as ComboBoxItem).Tag == "neural";
                string voiceId = (cbVoice.SelectedItem as Voice).Id;

                var taskId = await speechTasksInstance.AddNewTask(awsInterface, notifications, bucketName, baseKey, arn, filePath, useNeural, voiceId).ConfigureAwait(true);

                // enable the headers
                if (dgSpeechSynthesisTasks.Items.Count > 0)
                    dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.All;
                else
                    dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.None;

                // wait on a notificaton and then continue with the following task
                void ContinuationTask(object? msg)
                {
                    var message = (SNSNotificationMessage)msg;
                    var detail = $"Message content: {message.Message}";
                    notifications.ShowMessage("Task state changed", detail);
                    PollyCommands.RefreshTaskList.Execute(null, this);
                }
                var notificationsServicesInstance = this.FindResource("notificationsServicesInstance") as NotificationServices;
                var waitingOnNotifications = await notificationsServicesInstance.WaitOnTaskStateChanged(awsInterface, notifications, taskId, ContinuationTask).ConfigureAwait(true);
                if (waitingOnNotifications)
                    BeginRegularNotificationRequery();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void RefreshTaskList_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void RefreshTaskList_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var speechTasksInstance = this.FindResource("speechTasksInstance") as SpeechSynthesisTasksViewModel;

                await speechTasksInstance.ListTasks(awsInterface, notifications)
                    .ContinueWith(task => (dgSpeechSynthesisTasks.Items.Count > 0) ?
                            dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.All :
                            dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.None,
                            TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void TestNotifications_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void TestNotifications_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var notificationsServicesInstance = this.FindResource("notificationsServicesInstance") as NotificationServices;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var waitingOnNotifications = await notificationsServicesInstance.TestNotifications(awsInterface, notifications).ConfigureAwait(true);
                if (waitingOnNotifications)
                    BeginRegularNotificationRequery();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BeginRegularNotificationRequery()
        {
            // wait one minute then requery the input queue
            if (timer == null)
            {
                var seconds = awsInterface.RuntimeOptions.IsMocked ? 10 : 60;
                timer = new DispatcherTimer(TimeSpan.FromSeconds(seconds), DispatcherPriority.ApplicationIdle, DispatcherTimer_Tick, Dispatcher.CurrentDispatcher);
            }

            timer.Start();
        }

        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (sender is DispatcherTimer dispatcherTimer)
            {
                dispatcherTimer.Stop();

                var notificationsServicesInstance = this.FindResource("notificationsServicesInstance") as NotificationServices;
                var notifications = this.FindResource("applicationNotifications") as NotificationsList;

                var waitingOnNotifications = await notificationsServicesInstance.WaitOnMessage(awsInterface, notifications).ConfigureAwait(true);
                if (waitingOnNotifications)
                    dispatcherTimer.Start();
            }
        }

        private void FilePicker_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Choose a file to upload",
                Multiselect = false,
                InitialDirectory = AppSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbTextFilePath.Text = dlg.FileName;
            }
        }
    }
}
