using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;
using TustlerServicesLib;
using AppSettings = TustlerWinPlatformLib.ApplicationSettings;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranslateFunctionTestTranslations.xaml
    /// </summary>
    public partial class TranslateFunctionTestTranslations : UserControl
    {
        private readonly NotificationsList notifications;

        public TranslateFunctionTestTranslations()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void TranslateText_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbSourceText.Text) && cbSourceLanguage.SelectedItem != null && cbTargetLanguage.SelectedItem != null;
        }

        private async void TranslateText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var sourceLanguageCode = (cbSourceLanguage.SelectedItem as LanguageCode).Code;
                var targetLanguageCode = (cbTargetLanguage.SelectedItem as LanguageCode).Code;
                var translatedResult = await Helpers.TranslateServices.TranslateText(sourceLanguageCode, targetLanguageCode, tbSourceText.Text).ConfigureAwait(true);
                tbTranslatedText.Text = Helpers.TranslateServices.ProcessTranslatedResult(notifications, translatedResult);

                CommandManager.InvalidateRequerySuggested();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveTranslation_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbTranslatedText.Text);
        }

        private void SaveTranslation_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Title = "Choose a destination",
                InitialDirectory = AppSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    var filePath = dlg.FileName;
                    if (!Path.HasExtension(filePath))
                    {
                        filePath = Path.ChangeExtension(filePath, "txt");
                    }

                    File.WriteAllText(filePath, tbTranslatedText.Text);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void tbSourceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbTranslatedText.Clear();
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
