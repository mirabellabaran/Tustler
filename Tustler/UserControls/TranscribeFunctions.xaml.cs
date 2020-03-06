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
    /// Interaction logic for TranscribeFunctions.xaml
    /// </summary>
    public partial class TranscribeFunctions : UserControl
    {
        public TranscribeFunctions()
        {
            InitializeComponent();
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
    }
}
