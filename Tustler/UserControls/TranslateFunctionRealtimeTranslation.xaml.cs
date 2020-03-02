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
using System.Windows.Shapes;
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
            e.CanExecute = !string.IsNullOrEmpty(tbTranslationSourceDocument.Text) && File.Exists(tbTranslationSourceDocument.Text);
        }

        private async void RealtimeTranslate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string jobName = tbJobName.Text;
            string sourceLanguageCode = (cbSourceLanguage.SelectedItem as LanguageCode).Code;
            string targetLanguageCode = (cbTargetLanguage.SelectedItem as LanguageCode).Code;
            string textFilePath = tbTranslationSourceDocument.Text;
            Progress<int> progress = new Progress<int>(value =>
            {
                pbTranslationJob.Value = value;
            });

            try
            {

                List<string> terminologyNames = Helpers.UIServices.UIHelpers.GetTerminologyNames(chkIncludeTerminologyNames, lbTerminologyNames);
                await Helpers.TranslateServices.TranslateLargeText(notifications, progress, jobName, sourceLanguageCode, targetLanguageCode, textFilePath, terminologyNames).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
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
