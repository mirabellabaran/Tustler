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
using TustlerAWSLib;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for PollyFunctions.xaml
    /// </summary>
    public partial class PollyFunctions : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;

        public PollyFunctions(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //<TabItem Header="Manage Speech Tasks" Width="150">
            //    <uc:PollyFunctionSpeechTasks />
            //</TabItem>
            //<TabItem Header="Test Voices" Width="150">
            //    <uc:PollyFunctionTestSpeech />
            //</TabItem>
            //<TabItem Header="Manage Lexicons" Width="150">
            //    <uc:PollyFunctionLexicons />
            //</TabItem>

            tabPollyFunctions.Items.Add(new TabItem
            {
                Header = "Manage Speech Tasks",
                Width = 150.0,
                Content = new PollyFunctionSpeechTasks(awsInterface)
            });

            tabPollyFunctions.Items.Add(new TabItem
            {
                Header = "Test Voices",
                Width = 150.0,
                Content = new PollyFunctionTestSpeech(awsInterface)
            });

            tabPollyFunctions.Items.Add(new TabItem
            {
                Header = "Manage Lexicons",
                Width = 150.0,
                Content = new PollyFunctionLexicons(awsInterface)
            });
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

        public static readonly RoutedUICommand RefreshTaskList = new RoutedUICommand
            (
                "RefreshTaskList",
                "RefreshTaskList",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand TestNotifications = new RoutedUICommand
            (
                "TestNotifications",
                "TestNotifications",
                typeof(PollyCommands),
                null
            );

    }
}
