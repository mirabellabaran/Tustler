#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Tustler.Models;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranslateFunctionRealtimeTranslation.xaml
    /// </summary>
    public partial class TranslateFunctionRealtimeTranslation : UserControl
    {
        private readonly NotificationsList notifications;

        public TranslateFunctionRealtimeTranslation()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void RealtimeTranslate_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = 
                (!string.IsNullOrEmpty(tbTranslationSourceDocument.Text) && File.Exists(tbTranslationSourceDocument.Text))
                || !(Helpers.TranslateServices.GetArchivedJob(tbJobName.Text) is null);
        }

        private async void RealtimeTranslate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // returns true if an archived job should be used
            bool CheckUseArchivedJob(string jobName)
            {
                string? filePath = Helpers.TranslateServices.GetArchivedJob(jobName);
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }
                else
                {
                    // an archive exists with this name
                    if (!string.IsNullOrEmpty(tbTranslationSourceDocument.Text) && File.Exists(tbTranslationSourceDocument.Text))
                    {
                        // possible to start a new job; ask the user
                        MessageBoxResult result = MessageBox.Show($"An incomplete job with the name {jobName} can be found at {filePath}. Select Yes to continue with the unfinished job, or No to start a new job.", "Continue unfinished job?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        return (result) switch
                        {
                            MessageBoxResult.Yes => true,
                            MessageBoxResult.No => false,
                            _ => false
                        };
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            bool useArchivedJob = CheckUseArchivedJob(tbJobName.Text);
            string jobName = tbJobName.Text;
            string sourceLanguageCode = (cbSourceLanguage.SelectedItem as LanguageCode)!.Code;  // combobox must have a selection
            string targetLanguageCode = (cbTargetLanguage.SelectedItem as LanguageCode)!.Code;  // combobox must have a selection
            string textFilePath = tbTranslationSourceDocument.Text;
            Progress<int> progress = new Progress<int>(value =>
            {
                pbTranslationJob.Value = value;
            });

            try
            {
                pbTranslationJob.Value = 0.0;
                pbTranslationJob.Visibility = Visibility.Visible;
                List<string> terminologyNames = Helpers.UIServices.UIHelpers.GetTerminologyNames(chkIncludeTerminologyNames, lbTerminologyNames);
                await Helpers.TranslateServices.TranslateLargeText(notifications, progress, useArchivedJob, jobName, sourceLanguageCode, targetLanguageCode, textFilePath, terminologyNames).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                pbTranslationJob.Visibility = Visibility.Collapsed;
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

                var terminologiesInstance = this.FindResource("terminologiesInstance") as TranslationTerminologiesViewModel;

                await terminologiesInstance.Refresh(notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (lbTerminologyNames.Items.Count == 0)
            {
                notifications.ShowMessage("No terminologies", "No terminologies have been defined. Use the Amazon Console to add new terminologies.");
            }
        }

        private void lbTerminologyNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTerminologies = lbTerminologyNames.FindResource("selectedTerminologies") as SelectedItemsViewModel;
            selectedTerminologies.Update(lbTerminologyNames.SelectedItems as IEnumerable<object>);
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
                tbTranslationSourceDocument.Text = dlg.FileName;
            }
        }
    }
}
