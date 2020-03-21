using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Helpers;
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
        private readonly NotificationsList notifications;

        public PollyFunctionSpeechTasks()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var voicesInstance = this.FindResource("voicesInstance") as VoicesViewModel;

            string languageCode = null;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // refresh and then enable the headers
                await voicesInstance.Refresh(notifications, languageCode).ConfigureAwait(true);
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
                string key = $"SpeechTaskOutput-{DateTime.Now.Ticks}";
                string arn = AppSettings.NotificationsARN;
                string filePath = tbTextFilePath.Text;
                bool useNeural = (string)(cbEngine.SelectedItem as ComboBoxItem).Tag == "neural";
                string voiceId = (cbVoice.SelectedItem as ComboBoxItem).Content as string;

                var taskId = await speechTasksInstance.AddNewTask(notifications, bucketName, key, arn, filePath, useNeural, voiceId).ConfigureAwait(true);

                // enable the headers
                if (dgSpeechSynthesisTasks.Items.Count > 0)
                    dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.All;
                else
                    dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.None;

                // wait on a notificaton and then continue with the following task
                void ContinuationTask(object? msg)
                {
                    var message = (NotificationMessage)msg;
                    var detail = $"Message content: {message.Message}";
                    notifications.ShowMessage("Task state changed", detail);
                    PollyCommands.RefreshTaskList.Execute(null, this);
                }
                var notificationsServicesInstance = this.FindResource("notificationsServicesInstance") as NotificationServices;
                await notificationsServicesInstance.WaitOnTaskStateChanged(notifications, taskId, ContinuationTask).ConfigureAwait(true);
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

                await speechTasksInstance.ListTasks(notifications)
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

                await notificationsServicesInstance.TestNotifications(notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
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
