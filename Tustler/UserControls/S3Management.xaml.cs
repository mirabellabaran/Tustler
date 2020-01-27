using Amazon.S3.Model;
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
using TustlerAWSLib;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for S3Management.xaml
    /// </summary>
    public partial class S3Management : UserControl
    {
        private readonly ApplicationErrorList errorList;

        public S3Management()
        {
            InitializeComponent();

            errorList = this.FindResource("applicationErrors") as ApplicationErrorList;
        }

        private void ListBuckets_Button_Click(object sender, RoutedEventArgs e)
        {
            BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;

            bucketViewModel.Refresh(errorList);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;

            bucketItemsInstance.Refresh(errorList, selectedBucket.Name);
        }

        //private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var grid = (DataGrid)e.Source;
        //    BucketItem selectedItem = (BucketItem)grid.SelectedItem;

        //    await FetchS3ItemMetadata("tator", selectedItem.Key);   // TODO should not be hardcoded
        //}

    }
}
