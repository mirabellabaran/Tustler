using CloudWeaver.Foundation.Types;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerAWSLib;
using TustlerModels;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranscribeFunctionsVocabularies.xaml
    /// </summary>
    public partial class TranscribeFunctionVocabularies : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public TranscribeFunctionVocabularies(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;
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

                await vocabulariesInstance.Refresh(awsInterface, notifications)
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
