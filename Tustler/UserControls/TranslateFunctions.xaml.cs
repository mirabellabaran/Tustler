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

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranslateFunctions.xaml
    /// </summary>
    public partial class TranslateFunctions : UserControl
    {
        public TranslateFunctions()
        {
            InitializeComponent();
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
    }
}
