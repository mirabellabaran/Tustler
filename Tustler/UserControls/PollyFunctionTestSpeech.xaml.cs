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

        private void SynthesizeSpeech_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbSpeechText.Text);
        }

        private async void SynthesizeSpeech_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedVoice = (dgVoices.SelectedCells.Count > 0) ? (dgVoices.SelectedCells[0].Item as Voice) : null;
            var voiceId = (selectedVoice is null) ? null : selectedVoice.Id;
            var useNeural = (selectedVoice is null) ? true : selectedVoice.SupportedEngines.Contains("neural", StringComparison.InvariantCulture);

            var result = await Helpers.PollyServices.SynthesizeSpeech(tbSpeechText.Text, useNeural, voiceId).ConfigureAwait(true);

            (audioStream, contentType) = Helpers.PollyServices.ProcessSynthesizeSpeechResult(notifications, result);

            if (audioStream != null)
            {
                var prefix = "http://localhost:8000/audiostreamer/";    // must end in '/'
                var task = Task.Run(() =>
                {
                    return Helpers.AudioStreamer.StreamAudioAsync(audioStream, contentType, prefix, notifications);
                });

                await Task.Delay(1000).ConfigureAwait(false);   // wait for socket to come alive

                await mePlayer.Open(new Uri(prefix));
            }
        }

        private void mePlayer_MediaFailed(object sender, Unosquare.FFME.Common.MediaFailedEventArgs e)
        {
            notifications.HandleError("SynthesizeSpeech_Executed", "Audio streamer failed", e.ErrorException);
        }
    }
}
