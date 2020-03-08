using Microsoft.Win32;
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
using Tustler.Models;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranscribeFunctionTranscriptionTasks.xaml
    /// </summary>
    public partial class TranscribeFunctionTranscriptionTasks : UserControl
    {
        public TranscribeFunctionTranscriptionTasks()
        {
            InitializeComponent();
        }

        private void StartTranscriptionJob_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void StartTranscriptionJob_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void AddVocabulary_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }

        private void AddVocabulary_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void lbTerminologyNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTerminologies = lbTerminologyNames.FindResource("selectedTerminologies") as SelectedItemsViewModel;
            selectedTerminologies.Update(lbTerminologyNames.SelectedItems as IEnumerable<object>);
        }
    }
}
