using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;

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

        private void StartSpeechTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbTextFilePath.Text)
                && File.Exists(tbTextFilePath.Text);
        }

        private async void StartSpeechTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var speechTasksInstance = this.FindResource("speechTasksInstance") as SpeechSynthesisTasksViewModel;

            string bucketName = ApplicationSettings.DefaultBucketName;
            string key = $"SpeechTaskOutput-{DateTime.Now.Ticks}";
            string arn = ApplicationSettings.NotificationsARN;
            string filePath = tbTextFilePath.Text;
            bool useNeural = (string)(cbEngine.SelectedItem as ComboBoxItem).Tag == "neural";
            string voiceId = (cbVoice.SelectedItem as ComboBoxItem).Content as string;

            var taskId = await speechTasksInstance.AddNewTask(notifications, bucketName, key, arn, filePath, useNeural, voiceId).ConfigureAwait(true);

            // enable the headers
            if (dgSpeechSynthesisTasks.Items.Count > 0)
                dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.All;
            else
                dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.None;

            var notificationReceived = await Helpers.PollyServices.WaitOnNotification(notifications, taskId).ConfigureAwait(true);
            if (notificationReceived)
            {
                PollyCommands.RefreshTaskList.Execute(null, this);
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
            var result = await Helpers.PollyServices.TestNotifications().ConfigureAwait(true);
            Helpers.PollyServices.ProcessTestNotificationsResult(notifications, result);
        }

        private void FilePicker_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Choose a file to upload",
                Multiselect = false,
                InitialDirectory = ApplicationSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbTextFilePath.Text = dlg.FileName;
            }
        }
    }
}
