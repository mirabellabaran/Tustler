using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;
using AppSettings = TustlerWinPlatformLib.ApplicationSettings;
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

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBuckets(false).ConfigureAwait(true);
        }

        private async void ListBuckets_Button_Click(object sender, RoutedEventArgs e)
        {
            await LoadBuckets(true).ConfigureAwait(true);
        }

        private async Task LoadBuckets(bool forceRefresh)
        {
            BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                //await Dispatcher.InvokeAsync<Task>(() => bucketViewModel.Refresh(notifications));
                await bucketViewModel.Refresh(forceRefresh, notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            if (!(selectedBucket is null))
            {
                var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    // refresh and then enable the headers
                    await bucketItemsInstance.Refresh(notifications, selectedBucket.Name)
                    .ContinueWith(task => dgBucketItems.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
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
                    bucketItemsInstance.FilteredMediaType = BucketItemMediaType.All,
                "rbFilterAudio" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemMediaType.Audio,
                "rbFilterVideo" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemMediaType.Video,
                "rbFilterText" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemMediaType.Text,
                "rbFilterDefined" =>
                    bucketItemsInstance.FilteredMediaType = BucketItemMediaType.Defined,
                _ =>
                    bucketItemsInstance.FilteredMediaType = BucketItemMediaType.All,
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

        private async void DeleteBucketItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var key = (e.OriginalSource as Button).Tag as string;
            MessageBoxResult result = MessageBox.Show($"Selecting OK will permanently delete the file named: {key}", "Confirm delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.OK:
                    var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                    var bucketName = bucketItemsInstance.CurrentBucketName;

                    var deleteResult = await Helpers.S3Services.DeleteItem(bucketName, key).ConfigureAwait(true);

                    var success = Helpers.S3Services.ProcessDeleteBucketItemResult(notifications, deleteResult, key);
                    if (success)
                    {
                        await bucketItemsInstance.RefreshAsync(notifications).ConfigureAwait(true);
                    }
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

        private async void UploadItem_Executed(object sender, ExecutedRoutedEventArgs e)
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
                var bucketName = bucketItemsInstance.CurrentBucketName;

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    var uploadResult = await Helpers.S3Services.UploadItem(bucketName, path, mimetype, extension).ConfigureAwait(true);
                    Helpers.S3Services.ProcessUploadItemResult(notifications, uploadResult);

                    await bucketItemsInstance.RefreshAsync(notifications).ConfigureAwait(true);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
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

        private async void DownloadItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (dgBucketItems.SelectedCells[0].Item is BucketItem selectedItem)
            {
                var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
                var bucketName = bucketItemsInstance.CurrentBucketName;
                var key = selectedItem.Key;
                var absolutePath = Path.GetFullPath(tbDownloadPath.Text);
                var filePath = string.IsNullOrEmpty(selectedItem.Extension) ?
                    absolutePath :
                    Path.ChangeExtension(absolutePath, selectedItem.Extension);

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    var downloadResult = await Helpers.S3Services.DownloadItem(bucketName, key, filePath).ConfigureAwait(true);
                    Helpers.S3Services.ProcessDownloadItemResult(notifications, downloadResult);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void RefreshItems_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            e.CanExecute = bucketItemsInstance.CurrentBucketName != null;
        }

        private async void RefreshItems_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketItemsInstance.RefreshAsync(notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void UploadFilePicker_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Choose a file to upload",
                Multiselect = false,
                InitialDirectory = AppSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbUploadPath.Text = dlg.FileName;
            }
        }

        private void DownloadFilePicker_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Title = "Choose a download destination",
                InitialDirectory = AppSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbDownloadPath.Text = dlg.FileName;
            }
        }
    }

    public static class S3Commands
    {
        public static readonly RoutedUICommand FilterBucketItems = new RoutedUICommand
            (
                "FilterBucketItems",
                "FilterBucketItems",
                typeof(S3Commands),
                null
            );

        public static readonly RoutedUICommand DeleteBucketItem = new RoutedUICommand
            (
                "DeleteBucketItem",
                "DeleteBucketItem",
                typeof(S3Commands),
                null
            );

        public static readonly RoutedUICommand UploadItem = new RoutedUICommand
            (
                "UploadItem",
                "UploadItem",
                typeof(S3Commands),
                null
            );

        public static readonly RoutedUICommand DownloadItem = new RoutedUICommand
            (
                "DownloadItem",
                "DownloadItem",
                typeof(S3Commands),
                null
            );

        public static readonly RoutedUICommand RefreshItems = new RoutedUICommand
            (
                "RefreshItems",
                "RefreshItems",
                typeof(S3Commands),
                null
            );
    }

}
