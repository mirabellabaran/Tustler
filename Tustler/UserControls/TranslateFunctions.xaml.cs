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
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand SaveTranslation = new RoutedUICommand
            (
                "SaveTranslation",
                "SaveTranslation",
                typeof(PollyCommands),
                null
            );

    }
}
