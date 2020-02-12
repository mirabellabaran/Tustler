using System;
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
            e.CanExecute = !string.IsNullOrEmpty(tbTextFilePath.Text);
        }

        private async void StartSpeechTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var speechTasksInstance = this.FindResource("speechTasksInstance") as SpeechSynthesisTasksViewModel;

            string bucketName = ApplicationSettings.DefaultBucketName;
            string key = $"SpeechTaskOutput-{DateTime.Now.Ticks}";
            string arn = ApplicationSettings.NotificationsARN;
            string filePath = tbTextFilePath.Text;
            bool useNeural = (string)(cbEngine.SelectedItem as ComboBoxItem).Tag == "neural";
            string voiceId = cbVoice.SelectedItem as string;

            // refresh and then enable the headers
            await speechTasksInstance.Refresh(notifications, bucketName, key, arn, filePath, useNeural, voiceId)
                .ContinueWith(task => dgSpeechSynthesisTasks.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }
    }
}
