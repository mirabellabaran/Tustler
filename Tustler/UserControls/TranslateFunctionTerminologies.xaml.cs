﻿using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerModels;
using TustlerServicesLib;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for TranslateFunctionTerminologies.xaml
    /// </summary>
    public partial class TranslateFunctionTerminologies : UserControl
    {
        private readonly NotificationsList notifications;

        public TranslateFunctionTerminologies()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void ListTerminologies_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListTerminologies_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var terminologiesInstance = this.FindResource("terminologiesInstance") as TranslationTerminologiesViewModel;

                await terminologiesInstance.Refresh(notifications)
                    .ContinueWith(task => (dgTerminologies.Items.Count > 0) ?
                            dgTerminologies.HeadersVisibility = DataGridHeadersVisibility.All :
                            dgTerminologies.HeadersVisibility = DataGridHeadersVisibility.None,
                            TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (dgTerminologies.Items.Count == 0)
            {
                notifications.ShowMessage("No terminologies", "No terminologies have been defined. Use the Amazon Console to add new terminologies.");
            }
        }
    }
}
