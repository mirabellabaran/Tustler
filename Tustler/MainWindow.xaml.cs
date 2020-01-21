using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
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

namespace Tustler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Credentials_Button_Click(object sender, RoutedEventArgs e)
        {
            var accessKey = TustlerAWSLib.Utilities.CheckCredentials();
            // TODO redirect to a form asking for accessKey and secretKey
            // and then store in shared credentials file

            var message = (accessKey != null) ? accessKey : "None";
            var region = TustlerAWSLib.Utilities.GetRegion();
            message = (region != null) ? string.Format("{0} ({1})", message, region) : message;
            MessageBox.Show(message, "Access Key");
        }

        private async void ListBuckets_Button_Click(object sender, RoutedEventArgs e)
        {
            var bucketsResult = await TustlerAWSLib.S3.ListBuckets();
            if (bucketsResult.IsError)
            {
                if (bucketsResult.Exception is HttpRequestException)
                {
                    MessageBox.Show(string.Format("Error: {0}", bucketsResult.Exception.Message), "Not connected");
                }
                else
                {
                    MessageBox.Show(string.Format("Error: {0}", bucketsResult.Exception));
                }
            }
            else
            {
                var buckets = bucketsResult.Result;
                if (buckets.Count > 0)
                {
                    MessageBox.Show(string.Format("Bucket: {0}", buckets[0].BucketName));
                }
            }
        }

        private async void ListBucketItems_Button_Click(object sender, RoutedEventArgs e)
        {
            var bucketItemsResult = await TustlerAWSLib.S3.ListBucketItems("tator");
            if (bucketItemsResult.IsError)
            {
                if (bucketItemsResult.Exception is HttpRequestException)
                {
                    MessageBox.Show(string.Format("Error: {0}", bucketItemsResult.Exception.Message), "Not connected");
                }
                else
                {
                    MessageBox.Show(string.Format("Error: {0}", bucketItemsResult.Exception));
                }
            }
            else
            {
                var bucketItems = bucketItemsResult.Result;
                if (bucketItems.Count > 0)
                {
                    var items = from item in bucketItems select new BucketItem { Key = item.Key, Size = item.Size, LastModified = item.LastModified, Owner = item.Owner?.DisplayName };
                    MessageBox.Show(string.Format("Items: {0}", string.Join("\n", items)));

                    ObservableCollection<BucketItem> data = new ObservableCollection<BucketItem>(items);
                    var bucketItemsInstance = (BucketItemViewModel) this.FindResource("bucketItemsInstance");
                    bucketItemsInstance.BucketItems.Clear();
                    foreach (var item in items)
                    {
                        bucketItemsInstance.BucketItems.Add(item);
                    }
                }
            }
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand
            (
                "Exit",
                "Exit",
                typeof(CustomCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.F4, ModifierKeys.Alt)
                }
            );

        //Define more commands here, just like the one above
    }
}
