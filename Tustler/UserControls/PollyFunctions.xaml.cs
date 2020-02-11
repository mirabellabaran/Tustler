using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for PollyFunctions.xaml
    /// </summary>
    public partial class PollyFunctions : UserControl
    {
        private readonly NotificationsList notifications;

        // fields related to audio streaming
        private MemoryStream audioStream = null;
        private string contentType;

        public PollyFunctions()
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

    public static class PollyCommands
    {
        public static readonly RoutedUICommand ListVoices = new RoutedUICommand
            (
                "ListVoices",
                "ListVoices",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand GetLexicon = new RoutedUICommand
            (
                "GetLexicon",
                "GetLexicon",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand ListLexicons = new RoutedUICommand
            (
                "ListLexicons",
                "ListLexicons",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand SynthesizeSpeech = new RoutedUICommand
            (
                "SynthesizeSpeech",
                "SynthesizeSpeech",
                typeof(PollyCommands),
                null
            );

    }
}
