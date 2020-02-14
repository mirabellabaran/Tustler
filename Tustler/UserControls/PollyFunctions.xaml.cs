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
        public PollyFunctions()
        {
            InitializeComponent();
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

        public static readonly RoutedUICommand ReplaySpeech = new RoutedUICommand
            (
                "ReplaySpeech",
                "ReplaySpeech",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand SaveSynthesizedSpeech = new RoutedUICommand
            (
                "SaveSynthesizedSpeech",
                "SaveSynthesizedSpeech",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand StartSpeechTask = new RoutedUICommand
            (
                "StartSpeechTask",
                "StartSpeechTask",
                typeof(PollyCommands),
                null
            );

    }
}
