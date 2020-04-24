using System;
using System.Collections.Generic;
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
using TustlerAWSLib;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranscribeFunctions.xaml
    /// </summary>
    public partial class TranscribeFunctions : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;

        public TranscribeFunctions(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //<TabItem Header="Manage Transcription Jobs" Width="180">
            //    <uc:TranscribeFunctionTranscriptionTasks />
            //</TabItem>
            //<TabItem Header="Manage Vocabularies" Width="180">
            //    <uc:TranscribeFunctionVocabularies />
            //</TabItem>

            tabTranscribeFunctions.Items.Add(new TabItem
            {
                Header = "Manage Transcription Jobs",
                Width = 180.0,
                Content = new TranscribeFunctionTranscriptionTasks(awsInterface)
            });

            tabTranscribeFunctions.Items.Add(new TabItem
            {
                Header = "Manage Vocabularies",
                Width = 180.0,
                Content = new TranscribeFunctionVocabularies(awsInterface)
            });
        }
    }

    public static class TranscribeCommands
    {
        public static readonly RoutedUICommand ListVocabularies = new RoutedUICommand
            (
                "ListVocabularies",
                "ListVocabularies",
                typeof(TranscribeCommands),
                null
            );

        /// <summary>
        /// Add a vocabulary to the transcription job
        /// </summary>
        public static readonly RoutedUICommand AddVocabulary = new RoutedUICommand
            (
                "AddVocabulary",
                "AddVocabulary",
                typeof(TranscribeCommands),
                null
            );

        public static readonly RoutedUICommand StartTranscriptionJob = new RoutedUICommand
            (
                "StartTranscriptionJob",
                "StartTranscriptionJob",
                typeof(TranscribeCommands),
                null
            );

        public static readonly RoutedUICommand RefreshTaskList = new RoutedUICommand
            (
                "RefreshTaskList",
                "RefreshTaskList",
                typeof(TranscribeCommands),
                null
            );
    }
}
