using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for PollyFunctionTestSpeech.xaml
    /// </summary>
    public partial class PollyFunctionTestSpeech : UserControl
    {
        private readonly NotificationsList notifications;

        // fields related to audio streaming
        private MemoryStream audioStream = null;
        private string contentType;

        public PollyFunctionTestSpeech()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void ListVoices_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListVoices_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var voicesInstance = this.FindResource("voicesInstance") as VoicesViewModel;

            string languageCode = null;

            // refresh and then enable the headers
            await voicesInstance.Refresh(notifications, languageCode)
                .ContinueWith(task => dgVoices.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }

        private void GetLexicon_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbLexiconName.Text);
        }

        private async void GetLexicon_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var attributesInstance = this.FindResource("lexiconAttributesInstance") as LexiconAttributesViewModel;

            await attributesInstance.Refresh(notifications, tbLexiconName.Text)
                .ContinueWith(task => dgLexiconAttributes.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }

        private void ListLexicons_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListLexicons_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var lexiconsInstance = this.FindResource("lexiconsInstance") as LexiconsViewModel;

            await lexiconsInstance.Refresh(notifications)
                .ContinueWith(task => dgLexicons.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }

        private void SynthesizeSpeech_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbSpeechText.Text);
        }

        private async void SynthesizeSpeech_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var result = await Helpers.PollyServices.SynthesizeSpeech(tbSpeechText.Text, useNeural: true).ConfigureAwait(true);

            (audioStream, contentType) = Helpers.PollyServices.ProcessSynthesizeSpeechResult(notifications, result);

            if (audioStream != null)
            {
                Helpers.AudioStreamer.StreamAudio(audioStream);
                await Task.Delay(1000).ConfigureAwait(false);   // wait for socket to come alive

                var uri = new Uri("http://127.0.0.0:12");

                mePlayer.Source = uri;
                mePlayer.Play();
            }
        }

    }
}
