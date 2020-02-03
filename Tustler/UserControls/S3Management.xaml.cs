using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;
using Path = System.IO.Path;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for S3Management.xaml
    /// </summary>
    public partial class S3Management : UserControl
    {
        private readonly NotificationsList notifications;

        public S3Management()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void ListBuckets_Button_Click(object sender, RoutedEventArgs e)
        {
            BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;

            Helpers.UIServices.SetBusyState();
            bucketViewModel.Refresh(notifications);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;

            Helpers.UIServices.SetBusyState();
            bucketItemsInstance.Refresh(notifications, selectedBucket.Name);

            // enable the headers
            dgBucketItems.HeadersVisibility = DataGridHeadersVisibility.All;
        }

        private void FilterBucketItems_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void FilterBucketItems_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var radioButton = e.OriginalSource as RadioButton;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            bucketItemsInstance.FilteredMediaType = (radioButton.Name) switch
            {
                "rbFilterAll" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemViewModel.MediaType.All,
                "rbFilterAudio" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemViewModel.MediaType.Audio,
                "rbFilterVideo" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemViewModel.MediaType.Video,
                "rbFilterText" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemViewModel.MediaType.Text,
                "rbFilterDefined" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemViewModel.MediaType.Defined,
                _ =>
                    bucketItemsInstance.FilteredMediaType = BucketItemViewModel.MediaType.All,
            };
        }

        private void DeleteBucketItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (dgBucketItems.SelectedCells.Count > 0)
            {
                var item = dgBucketItems.SelectedCells[0].Item as BucketItem;
                var buttonSourceTag = (e.OriginalSource as Button).Tag as string;

                e.CanExecute = item.Key == buttonSourceTag;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void DeleteBucketItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var buttonSourceTag = (e.OriginalSource as Button).Tag as string;
            MessageBoxResult result = MessageBox.Show($"Selecting OK will permanently delete the file named: {buttonSourceTag}", "Confirm delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.OK:
                    var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                    bucketItemsInstance.DeleteItem(notifications, buttonSourceTag);
                    break;
            }
        }

        private void UploadItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (tbUploadPath == null)
            {
                e.CanExecute = false;
            }
            else
            {
                var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                var bucketNameIsSet = bucketItemsInstance.CurrentBucketName != null;
                e.CanExecute = (tbUploadPath.Text.Length > 0) && File.Exists(tbUploadPath.Text) && bucketNameIsSet;
            }
        }

        private void UploadItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            static (bool proceed, string path, string mimetype, string extension) CheckAddExtension(string path)
            {
                var mimetype = Helpers.FileServices.GetMimeType(path);
                var extension = Path.GetExtension(path);

                if (string.IsNullOrEmpty(extension))
                {
                    extension = TustlerServicesLib.MimeTypeDictionary.GetExtensionFromMimeType(mimetype);

                    if (string.IsNullOrEmpty(extension))
                    {
                        // extension cannot be inferred
                        MessageBoxResult result = MessageBox.Show($"No file extension was supplied and the extension cannot be inferred. Select OK to upload the file without an extension, or Cancel to add your own extension.", "Proceed without an extension", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        var proceed = (result) switch
                        {
                            MessageBoxResult.OK => true,
                            MessageBoxResult.Cancel => false,
                            _ => false
                        };

                        return (proceed, path, mimetype, extension);
                    }
                    else
                    {
                        MessageBoxResult result = MessageBox.Show($"The inferred mimetype is {mimetype} with extension {extension}. Select Yes to upload the file with this extension, or No to add your own extension.", "Add a file extension", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                var newpath = Path.ChangeExtension(path, extension);
                                return (true, newpath, mimetype, extension);
                            case MessageBoxResult.No:
                                return (false, path, mimetype, extension);
                            default:
                                return (false, path, mimetype, extension);
                        }
                    }
                }
                else
                {
                    extension = extension.Substring(1).ToLowerInvariant();
                    return (true, path, mimetype, extension);
                }
            }

            (bool proceed, string path, string mimetype, string extension) = CheckAddExtension(tbUploadPath.Text);
            if (proceed)
            {
                var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                bucketItemsInstance.UploadItem(notifications, path, mimetype, extension);
            }
        }

        private void DownloadItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (tbDownloadPath == null || tbDownloadPath.Text.Length == 0 || Directory.Exists(tbDownloadPath.Text))
            {
                // need a full file path, not just a directory
                e.CanExecute = false;
            }
            else
            {
                // must have a selection (ie the item to download)
                var selectedItem = dgBucketItems.SelectedCells.Count > 0 ? dgBucketItems.SelectedCells[0].Item as BucketItem : null;
                if (selectedItem == null)
                {
                    e.CanExecute = false;
                }
                else {
                    // the folder must exist and a filename must be included
                    var downloadFolder = Path.GetDirectoryName(tbDownloadPath.Text);
                    var filename = Path.GetFileName(tbDownloadPath.Text);
                    e.CanExecute = !string.IsNullOrEmpty(filename) && Directory.Exists(downloadFolder);
                }
            }
        }

        private void DownloadItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItem = dgBucketItems.SelectedCells[0].Item as BucketItem;
            if (selectedItem != null)
            {
                var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                var key = selectedItem.Key;
                var absolutePath = Path.GetFullPath(tbDownloadPath.Text);
                var filePath = string.IsNullOrEmpty(selectedItem.Extension) ?
                    absolutePath :
                    Path.ChangeExtension(absolutePath, selectedItem.Extension);
                bucketItemsInstance.DownloadItem(notifications, key, filePath);
            }
        }

        private void RefreshItems_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            e.CanExecute = bucketItemsInstance.CurrentBucketName != null;
        }

        private void RefreshItems_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            bucketItemsInstance.Refresh(notifications);
        }

        private void UploadFilePicker_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.FileName = "Document"; // Default file name
            dlg.Title = "Choose a file to upload";
            dlg.Multiselect = false;
            dlg.InitialDirectory = ApplicationSettings.FileCachePath;
            //dlg.DefaultExt = ".txt"; // Default file extension
            //dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbUploadPath.Text = dlg.FileName;
            }
        }

        private void DownloadFilePicker_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Choose a download destination";
            dlg.InitialDirectory = ApplicationSettings.FileCachePath;
            //dlg.FileName = "Document"; // Default file name
            //dlg.DefaultExt = ".txt"; // Default file extension
            //dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbDownloadPath.Text = dlg.FileName;
            }
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand FilterBucketItems = new RoutedUICommand
            (
                "FilterBucketItems",
                "FilterBucketItems",
                typeof(CustomCommands),
                null
            );

        public static readonly RoutedUICommand DeleteBucketItem = new RoutedUICommand
            (
                "DeleteBucketItem",
                "DeleteBucketItem",
                typeof(CustomCommands),
                null
            );

        public static readonly RoutedUICommand UploadItem = new RoutedUICommand
            (
                "UploadItem",
                "UploadItem",
                typeof(CustomCommands),
                null
            );

        public static readonly RoutedUICommand DownloadItem = new RoutedUICommand
            (
                "DownloadItem",
                "DownloadItem",
                typeof(CustomCommands),
                null
            );

        public static readonly RoutedUICommand RefreshItems = new RoutedUICommand
            (
                "RefreshItems",
                "RefreshItems",
                typeof(CustomCommands),
                null
            );
    }

}
