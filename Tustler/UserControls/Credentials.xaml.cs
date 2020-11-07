using Amazon;
using CloudWeaver.Foundation.Types;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerModels;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Credentials.xaml
    /// </summary>
    public partial class Credentials : UserControl
    {
        public Credentials()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var credentials = TustlerAWSLib.Credentials.GetCredentials();
            tbAccessKey.Text = credentials?.AccessKey;
            tbSecretKey.Password = credentials?.SecretKey;

            var configuredRegion = TustlerAWSLib.Region.GetRegion();
            if (!(configuredRegion is null))
            {
                var regions = cbRegion.ItemsSource as IEnumerable<Endpoint>;
                var hits = regions.Where(region => region.Code == configuredRegion.SystemName).ToArray();
                if (hits.Length > 0)
                {
                    var configuredRegionItem = hits.First();
                    cbRegion.SelectedItem = configuredRegionItem;
                }
            }
        }

        private void SaveCredentials_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (!string.IsNullOrEmpty(tbAccessKey.Text) && !string.IsNullOrEmpty(tbSecretKey.Password));
        }

        private void SaveCredentials_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var regionModel = cbRegion.SelectedItem as Endpoint;
                var region = RegionEndpoint.GetBySystemName(regionModel.Code);

                // save the credentials and region
                TustlerAWSLib.Credentials.StoreCredentials(tbAccessKey.Text, tbSecretKey.Password, region);

                var notifications = this.FindResource("applicationNotifications") as NotificationsList;
                notifications.ShowMessage("Credentials saved", $"Credentials were saved to a folder named .aws in your home directory");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }

    public static class CredentialsCommands
    {
        public static readonly RoutedUICommand SaveCredentials = new RoutedUICommand
            (
                "SaveCredentials",
                "SaveCredentials",
                typeof(CredentialsCommands),
                null
            );
    }
}
