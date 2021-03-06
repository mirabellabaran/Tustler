using CloudWeaver.Foundation.Types;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerAWSLib;
using TustlerModels;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for PollyFunctionLexicons.xaml
    /// </summary>
    public partial class PollyFunctionLexicons : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public PollyFunctionLexicons(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        //private void GetLexicon_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = !string.IsNullOrEmpty(tbLexiconName.Text);
        //}

        //private async void GetLexicon_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    var attributesInstance = this.FindResource("lexiconAttributesInstance") as LexiconAttributesViewModel;

        //    await attributesInstance.Refresh(notifications, tbLexiconName.Text)
        //        .ContinueWith(task => (
        //            dgLexiconAttributes.Items.Count > 0) ?
        //                dgLexiconAttributes.HeadersVisibility = DataGridHeadersVisibility.All :
        //                dgLexiconAttributes.HeadersVisibility = DataGridHeadersVisibility.None,
        //                TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        //}

        private void ListLexicons_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListLexicons_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var lexiconsInstance = this.FindResource("lexiconsInstance") as LexiconsViewModel;

                await lexiconsInstance.Refresh(awsInterface, notifications)
                    .ContinueWith(task => (dgLexicons.Items.Count > 0) ?
                            dgLexicons.HeadersVisibility = DataGridHeadersVisibility.All :
                            dgLexicons.HeadersVisibility = DataGridHeadersVisibility.None,
                            TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (dgLexicons.Items.Count == 0)
            {
                notifications.ShowMessage("No lexicons", "No lexicons have been defined. Use the Amazon Console to add new lexicons.");
            }
        }

    }
}
