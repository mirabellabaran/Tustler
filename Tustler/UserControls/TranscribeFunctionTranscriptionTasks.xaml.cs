using CloudWeaver.Foundation.Types;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;
using TustlerAWSLib;
using TustlerModels;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranscribeFunctionTranscriptionTasks.xaml
    /// </summary>
    public partial class TranscribeFunctionTranscriptionTasks : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public TranscribeFunctionTranscriptionTasks(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketViewModel.Refresh(awsInterface, false, notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void StartTranscriptionJob_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (!string.IsNullOrEmpty(tbJobName.Text) && !(lbBuckets.SelectedItem is null) && !(lbBucketItems.SelectedItem is null));
        }

        private async void StartTranscriptionJob_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var transcriptionJobsInstance = this.FindResource("transcriptionJobsInstance") as TranscriptionJobsViewModel;

            var jobName = tbJobName.Text;
            var bucketName = (lbBuckets.SelectedItem as Bucket).Name;
            var s3MediaKey = (lbBucketItems.SelectedItem as BucketItem).Key;
            var languageCode = (cbSourceLanguage.SelectedItem as LanguageCode).Code;
            List<string> vocabularyNames = Helpers.UIServices.UIHelpers.GetFieldFromListBoxSelectedItems<Vocabulary>(chkIncludeVocabulary, lbVocabularyNames, vocab => vocab.VocabularyName);
            var vocabularyName = vocabularyNames?[0];

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var success = await transcriptionJobsInstance.AddNewTask(awsInterface, notifications, jobName, bucketName, s3MediaKey, languageCode, vocabularyName).ConfigureAwait(true);
                if (success)
                {
                    notifications.ShowMessage($"Transcription job {jobName} started", "Manual polling is required to check for completion");
                }
                if (dgTranscriptionTasks.Items.Count > 0)
                {
                    dgTranscriptionTasks.HeadersVisibility = DataGridHeadersVisibility.All;
                }
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

                var transcriptionJobsInstance = this.FindResource("transcriptionJobsInstance") as TranscriptionJobsViewModel;

                await transcriptionJobsInstance.ListTasks(awsInterface, notifications)
                    .ContinueWith(task => (dgTranscriptionTasks.Items.Count > 0) ?
                            dgTranscriptionTasks.HeadersVisibility = DataGridHeadersVisibility.All :
                            dgTranscriptionTasks.HeadersVisibility = DataGridHeadersVisibility.None,
                            TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (dgTranscriptionTasks.Items.Count == 0)
            {
                notifications.ShowMessage("No transcription tasks defined", "No transcription tasks have been defined.");
            }
        }

        private void AddVocabulary_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void AddVocabulary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (chkIncludeVocabulary.IsChecked.Value)
                {
                    var vocabulariesInstance = this.FindResource("vocabulariesInstance") as TranscriptionVocabulariesViewModel;
                    await vocabulariesInstance.Refresh(awsInterface, notifications).ConfigureAwait(true);

                    if (lbVocabularyNames.Items.Count == 0)
                    {
                        notifications.ShowMessage("No vocabularies", "No vocabularies have been defined. Use the Amazon Console to add new vocabularies.");
                    }
                }
                else
                {
                    // clear selections
                    var selectedVocabularies = lbVocabularyNames.FindResource("selectedVocabularies") as SelectedItemsViewModel;
                    selectedVocabularies.Clear();

                    lbVocabularyNames.SelectedItem = null;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void VocabularyNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedVocabularies = lbVocabularyNames.FindResource("selectedVocabularies") as SelectedItemsViewModel;
            selectedVocabularies.Update(lbVocabularyNames.SelectedItems as IEnumerable<object>);
        }

        private async void BucketsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            var audioBucketItemsInstance = this.FindResource("audioBucketItemsInstance") as FilteredBucketItemViewModel;
            
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketItemsInstance.ForceRefresh(awsInterface, notifications, selectedBucket.Name).ConfigureAwait(true);
                audioBucketItemsInstance.Clear();
                audioBucketItemsInstance.Select(bucketItemsInstance, BucketItemMediaType.Audio);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}
