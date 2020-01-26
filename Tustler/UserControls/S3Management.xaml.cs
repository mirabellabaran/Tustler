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
using Tustler.Models;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for S3Management.xaml
    /// </summary>
    public partial class S3Management : UserControl
    {
        public S3Management()
        {
            InitializeComponent();
        }

        private async void ListBuckets_Button_Click(object sender, RoutedEventArgs e)
        {
            await FetchS3Buckets();
        }

        private async void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            await FetchS3BucketItems(selectedBucket.Name);
        }

        //private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var grid = (DataGrid)e.Source;
        //    BucketItem selectedItem = (BucketItem)grid.SelectedItem;

        //    await FetchS3ItemMetadata("tator", selectedItem.Key);   // TODO should not be hardcoded
        //}

        private async Task FetchS3Buckets()
        {
            var bucketsResult = await TustlerAWSLib.S3.ListBuckets();
            if (bucketsResult.IsError)
            {
                if (bucketsResult.Exception is HttpRequestException)
                {
                    tbErrors.Text = "Not connected";
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
                    static void AppendBucketCollection(ObservableCollection<Bucket> collection, List<Amazon.S3.Model.S3Bucket> buckets)
                    {
                        var bucketModelItems = from bucket in buckets select new Bucket { Name = bucket.BucketName, CreationDate = bucket.CreationDate };

                        collection.Clear();
                        foreach (var bucket in bucketModelItems)
                        {
                            collection.Add(bucket);
                        }
                    };
                    BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;
                    AppendBucketCollection(bucketViewModel.Buckets, buckets);
                }
            }
        }

        private async Task FetchS3BucketItems(string bucketName)
        {
            var bucketItemsResult = await TustlerAWSLib.S3.ListBucketItems(bucketName);
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

                    ObservableCollection<BucketItem> data = new ObservableCollection<BucketItem>(items);
                    var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                    bucketItemsInstance.BucketItems.Clear();
                    foreach (var item in items)
                    {
                        bucketItemsInstance.BucketItems.Add(item);

                        await FetchS3ItemMetadata("tator", item.Key);   // TODO should not be hardcoded
                    }
                }
            }
        }

        private async Task FetchS3ItemMetadata(string bucketName, string key)
        {
            var metadataResult = await TustlerAWSLib.S3.GetItemMetadata(bucketName, key);
            if (metadataResult.IsError)
            {
                if (metadataResult.Exception is HttpRequestException)
                {
                    MessageBox.Show(string.Format("Error: {0}", metadataResult.Exception.Message), "Not connected");
                }
                else
                {
                    MessageBox.Show(string.Format("Error: {0}", metadataResult.Exception));
                }
            }
            else
            {
                var metadata = metadataResult.Result;

                var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;

                // patch the current item with the returned metadata
                var currentItem = bucketItemsInstance.BucketItems.First(item => item.Key == key);
                currentItem.MimeType = metadata["MimeType"];
                currentItem.Extension = metadata["Extension"];
            }
        }
    }
}
