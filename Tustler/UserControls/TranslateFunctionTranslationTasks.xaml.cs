using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tustler.Models;
using Microsoft.Win32;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranslateFunctionTranslationTasks.xaml
    /// </summary>
    public partial class TranslateFunctionTranslationTasks : UserControl
    {
        private readonly NotificationsList notifications;

        public TranslateFunctionTranslationTasks()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void StartTranslationTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbTextFilePath.Text)
                && File.Exists(tbTextFilePath.Text);
        }

        private async void StartTranslationTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var translationJobsInstance = this.FindResource("translationJobsInstance") as TranslationJobsViewModel;

            List<string> GetTargetLanguageCodes()
            {
                var selectedLanguageCodes = lbTargetLanguages.SelectedItems as IEnumerable<LanguageCode>;
                return selectedLanguageCodes.Select(lc => lc.Code).ToList();
            }

            string jobName = tbJobName.Text;
            string sourceLanguageCode = (cbSourceLanguage.SelectedItem as LanguageCode).Code;
            List<string> targetLanguageCodes = GetTargetLanguageCodes();
            string s3InputFolderName = tbInputFolder.Text;  // TODO lookup
            string s3OutputFolderName = tbOutputFolder.Text;    // TODO lookup
            List<string> terminologyNames = null;

            var taskId = await translationJobsInstance.AddNewTask(notifications, jobName, sourceLanguageCode, targetLanguageCodes, s3InputFolderName, s3OutputFolderName, terminologyNames).ConfigureAwait(true);

            // enable the headers
            if (dgTranslationTasks.Items.Count > 0)
                dgTranslationTasks.HeadersVisibility = DataGridHeadersVisibility.All;
            else
                dgTranslationTasks.HeadersVisibility = DataGridHeadersVisibility.None;
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

                var translationJobsInstance = this.FindResource("translationJobsInstance") as TranslationJobsViewModel;

                await translationJobsInstance.ListTasks(notifications)
                    .ContinueWith(task => (dgTranslationTasks.Items.Count > 0) ?
                            dgTranslationTasks.HeadersVisibility = DataGridHeadersVisibility.All :
                            dgTranslationTasks.HeadersVisibility = DataGridHeadersVisibility.None,
                            TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (dgTranslationTasks.Items.Count == 0)
            {
                notifications.ShowMessage("No translation tasks defined", "No translation tasks have been defined.");
            }
        }

        private void FilePicker_Click(object sender, RoutedEventArgs e)
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

        private void tbJobName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbJobName.Text)){
                tbJobName.Text = $"TranslationJob-{DateTime.Now.Ticks}";
            }
        }
    }
}
