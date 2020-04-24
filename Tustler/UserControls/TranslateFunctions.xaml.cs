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
    /// Interaction logic for TranslateFunctions.xaml
    /// </summary>
    public partial class TranslateFunctions : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;

        public TranslateFunctions(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //<TabItem Header="Realtime Translation" Width="150">
            //    <uc:TranslateFunctionRealtimeTranslation />
            //</TabItem>
            //<TabItem Header="Manage Batch Translation" Width="150">
            //    <uc:TranslateFunctionTranslationTasks />
            //</TabItem>
            //<TabItem Header="Test Translations" Width="150">
            //    <uc:TranslateFunctionTestTranslations />
            //</TabItem>
            //<TabItem Header="Manage Terminologies" Width="150">
            //    <uc:TranslateFunctionTerminologies/>
            //</TabItem>

            tabTranslateFunctions.Items.Add(new TabItem
            {
                Header = "Realtime Translation",
                Width = 150.0,
                Content = new TranslateFunctionRealtimeTranslation(awsInterface)
            });

            tabTranslateFunctions.Items.Add(new TabItem
            {
                Header = "Manage Batch Translation",
                Width = 150.0,
                Content = new TranslateFunctionTranslationTasks(awsInterface)
            });

            tabTranslateFunctions.Items.Add(new TabItem
            {
                Header = "Test Translations",
                Width = 150.0,
                Content = new TranslateFunctionTestTranslations(awsInterface)
            });

            tabTranslateFunctions.Items.Add(new TabItem
            {
                Header = "Manage Terminologies",
                Width = 150.0,
                Content = new TranslateFunctionTerminologies(awsInterface)
            });
        }
    }

    public static class TranslateCommands
    {
        public static readonly RoutedUICommand TranslateText = new RoutedUICommand
            (
                "TranslateText",
                "TranslateText",
                typeof(TranslateCommands),
                null
            );

        public static readonly RoutedUICommand RealtimeTranslate = new RoutedUICommand
            (
                "RealtimeTranslate",
                "RealtimeTranslate",
                typeof(TranslateCommands),
                null
            );

        public static readonly RoutedUICommand SaveTranslation = new RoutedUICommand
            (
                "SaveTranslation",
                "SaveTranslation",
                typeof(TranslateCommands),
                null
            );

        public static readonly RoutedUICommand ListTerminologies = new RoutedUICommand
            (
                "ListTerminologies",
                "ListTerminologies",
                typeof(TranslateCommands),
                null
            );

        public static readonly RoutedUICommand StartTranslationTask = new RoutedUICommand
            (
                "StartTranslationTask",
                "StartTranslationTask",
                typeof(TranslateCommands),
                null
            );

        public static readonly RoutedUICommand RefreshTaskList = new RoutedUICommand
            (
                "RefreshTaskList",
                "RefreshTaskList",
                typeof(TranslateCommands),
                null
            );

        public static readonly RoutedUICommand AddTerminologies = new RoutedUICommand
            (
                "AddTerminologies",
                "AddTerminologies",
                typeof(TranslateCommands),
                null
            );
    }
}
