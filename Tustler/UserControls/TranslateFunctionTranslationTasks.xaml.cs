using Amazon;
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerAWSLib;
using TustlerModels;
using AppSettings = TustlerServicesLib.ApplicationSettings;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranslateFunctionTranslationTasks.xaml
    /// </summary>
    public partial class TranslateFunctionTranslationTasks : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public TranslateFunctionTranslationTasks(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;

            tbInputFolder.Text = AppSettings.BatchTranslateInputFolder;
            tbOutputFolder.Text = AppSettings.BatchTranslateOutputFolder;
        }

        private void StartTranslationTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = lbTargetLanguages.SelectedItems.Count > 0;
                //&& !string.IsNullOrEmpty(tbInputFolder.Text)
                //&& !string.IsNullOrEmpty(tbOutputFolder.Text);
        }

        private async void StartTranslationTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var translationJobsInstance = this.FindResource("translationJobsInstance") as TranslationJobsViewModel;

            List<string> GetTargetLanguageCodes()
            {
                var selectedLanguageCodes = (lbTargetLanguages.SelectedItems as IEnumerable<object>).Cast<LanguageCode>();
                return selectedLanguageCodes.Select(lc => lc.Code).ToList();
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string jobName = string.IsNullOrEmpty(tbJobName.Text) ? $"TranslateJob-{DateTime.Now.Ticks}" : tbJobName.Text;
                string regionSystemName = AppSettings.BatchTranslateRegion;
                string dataAccessRoleArn = AppSettings.BatchTranslateServiceRole;
                string sourceLanguageCode = (cbSourceLanguage.SelectedItem as LanguageCode).Code;
                List<string> targetLanguageCodes = GetTargetLanguageCodes();
                string s3InputFolderName = AppSettings.BatchTranslateInputFolder;
                string s3OutputFolderName = AppSettings.BatchTranslateOutputFolder;
                List<string> terminologyNames = Helpers.UIServices.UIHelpers.GetFieldFromListBoxSelectedItems<Terminology>(chkIncludeTerminologyNames, lbTerminologyNames, term => term.Name);

                await translationJobsInstance.AddNewTask(awsInterface, notifications, jobName, RegionEndpoint.GetBySystemName(regionSystemName), dataAccessRoleArn, sourceLanguageCode, targetLanguageCodes, s3InputFolderName, s3OutputFolderName, terminologyNames).ConfigureAwait(true);
                if (dgTranslationTasks.Items.Count > 0)
                {
                    dgTranslationTasks.HeadersVisibility = DataGridHeadersVisibility.All;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

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
                string regionSystemName = AppSettings.BatchTranslateRegion;

                await translationJobsInstance.ListTasks(awsInterface, notifications, RegionEndpoint.GetBySystemName(regionSystemName))
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

        private void AddTerminologies_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void AddTerminologies_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (chkIncludeTerminologyNames.IsChecked.HasValue && chkIncludeTerminologyNames.IsChecked.Value)
                {
                    var terminologiesInstance = this.FindResource("terminologiesInstance") as TranslationTerminologiesViewModel;
                    await terminologiesInstance.Refresh(awsInterface, notifications).ConfigureAwait(true);

                    if (lbTerminologyNames.Items.Count == 0)
                    {
                        notifications.ShowMessage("No terminologies", "No terminologies have been defined. Use the Amazon Console to add new terminologies.");
                    }
                }
                else
                {
                    // clear selections
                    var selectedTerminologies = lbTerminologyNames.FindResource("selectedTerminologies") as SelectedItemsViewModel;
                    selectedTerminologies.Clear();

                    lbTerminologyNames.SelectedItem = null;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void lbTerminologyNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTerminologies = lbTerminologyNames.FindResource("selectedTerminologies") as SelectedItemsViewModel;
            selectedTerminologies.Update(lbTerminologyNames.SelectedItems as IEnumerable<object>);
        }

        private void lbTargetLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedLanguageCodes = lbTargetLanguages.FindResource("selectedLanguageCodes") as SelectedItemsViewModel;
            selectedLanguageCodes.Update(lbTargetLanguages.SelectedItems as IEnumerable<object>);

            if (e.AddedItems.Count > 0)
            {
                var firstItem = (e.AddedItems as IEnumerable<object>).First() as LanguageCode;
                lbTargetLanguages.ScrollIntoView(firstItem);
            }
        }
    }
}
