using System;
using System.Collections.Generic;
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
    /// Interaction logic for TranscribeFunctionsVocabularies.xaml
    /// </summary>
    public partial class TranscribeFunctionsVocabularies : UserControl
    {
        private readonly NotificationsList notifications;

        public TranscribeFunctionsVocabularies()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void ListVocabularies_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListVocabularies_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var vocabulariesInstance = this.FindResource("vocabulariesInstance") as TranscriptionVocabulariesViewModel;

                await vocabulariesInstance.Refresh(notifications)
                    .ContinueWith(task => (dgVocabularies.Items.Count > 0) ?
                            dgVocabularies.HeadersVisibility = DataGridHeadersVisibility.All :
                            dgVocabularies.HeadersVisibility = DataGridHeadersVisibility.None,
                            TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (dgVocabularies.Items.Count == 0)
            {
                notifications.ShowMessage("No vocabularies", "No vocabularies have been defined. Use the Amazon Console to add new vocabularies.");
            }
        }
    }
}
