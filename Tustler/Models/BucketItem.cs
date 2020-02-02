using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Tustler.Models
{
    public class BucketItemViewModel
    {
        public enum MediaType
        {
            All,
            Audio,
            Video,
            Text,
            Defined     // the mime type is non-null
        }

        private MediaType filteredMediaType;

        public ObservableCollection<BucketItem> BucketItems
        {
            get;
            private set;
        }

        public ICollectionView BucketItemsView
        {
            get { return CollectionViewSource.GetDefaultView(BucketItems); }
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public string CurrentBucketName
        {
            get;
            set;
        }

        public MediaType FilteredMediaType
        {
            get
            {
                return filteredMediaType;
            }
            set
            {
                filteredMediaType = value;
                SetFilter(filteredMediaType);
            }
        }

        public BucketItemViewModel()
        {
            this.BucketItems = new ObservableCollection<BucketItem>();
            this.NeedsRefresh = true;
            this.CurrentBucketName = null;
        }

        public void Refresh(NotificationsList notifications)
        {
            this.NeedsRefresh = true;
            Refresh(notifications, CurrentBucketName);
        }

        public async void Refresh(NotificationsList notifications, string bucketName)
        {
            if (NeedsRefresh)
            {
                await FetchS3BucketItems(notifications, bucketName);
                CurrentBucketName = bucketName;
            }
        }

        public async void DeleteItem(NotificationsList notifications, string key)
        {
            await DeleteBucketItem(notifications, CurrentBucketName, key);
        }

        public async void UploadItem(NotificationsList notifications, string filePath)
        {
            await UploadS3Item(notifications, CurrentBucketName, filePath);
        }

        public async void DownloadItem(NotificationsList notifications, string key, string filePath)
        {
            await DownloadS3Item(notifications, CurrentBucketName, key, filePath);
        }

        /// <summary>
        /// Set a filter on the view, showing only the specified mime types e.g. audio
        /// </summary>
        /// <param name="requiredMediaType"></param>
        private void SetFilter(MediaType selectedMediaType)
        {
            static bool CheckTypeIs(string mimeType, string type)
            {
                if (mimeType == null)
                    return false;
                else
                    return mimeType.Contains(type, StringComparison.InvariantCulture);
            }
            Func<string, bool> isRquiredMediaType = (selectedMediaType) switch
            {
                MediaType.All => (mimeType => true),
                MediaType.Audio => (mimeType => CheckTypeIs(mimeType, "audio")),
                MediaType.Video => (mimeType => CheckTypeIs(mimeType, "video")),
                MediaType.Text => (mimeType => CheckTypeIs(mimeType, "text")),
                MediaType.Defined => (mimeType => (mimeType != null)),
                _ => (mimeType => true),
            };

            BucketItemsView.Filter = new Predicate<object>(item => isRquiredMediaType((item as BucketItem).MimeType));
            BucketItemsView.Refresh();
        }

        private async Task DeleteBucketItem(NotificationsList notifications, string bucketName, string key)
        {
            var deleteResult = await TustlerAWSLib.S3.DeleteBucketItem(bucketName, key);

            if (deleteResult.IsError)
            {
                notifications.HandleError(deleteResult);
            }
            else
            {
                var success = deleteResult.Result;
                if (success.HasValue && success.Value)
                {
                    Refresh(notifications);
                }
            }
        }

        private async Task FetchS3BucketItems(NotificationsList notifications, string bucketName)
        {
            var bucketItemsResult = await TustlerAWSLib.S3.ListBucketItems(bucketName);
            if (bucketItemsResult.IsError)
            {
                notifications.HandleError(bucketItemsResult);
            }
            else
            {
                var bucketItems = bucketItemsResult.Result;
                if (bucketItems.Count > 0)
                {
                    var items = from item in bucketItems select new BucketItem { Key = item.Key, Size = item.Size, LastModified = item.LastModified, Owner = item.Owner?.DisplayName };

                    ObservableCollection<BucketItem> data = new ObservableCollection<BucketItem>(items);
                    this.BucketItems.Clear();
                    foreach (var item in items)
                    {
                        this.BucketItems.Add(item);

                        await FetchS3ItemMetadata(notifications, bucketName, item.Key);
                    }
                }

                this.FilteredMediaType = MediaType.All;
                NeedsRefresh = false;
            }
        }

        private async Task FetchS3ItemMetadata(NotificationsList notifications, string bucketName, string key)
        {
            var metadataResult = await TustlerAWSLib.S3.GetItemMetadata(bucketName, key);
            if (metadataResult.IsError)
            {
                notifications.HandleError(metadataResult);
                NeedsRefresh = true;
            }
            else
            {
                var metadata = metadataResult.Result;

                // patch the current item with the returned metadata
                var currentItem = this.BucketItems.First(item => item.Key == key);
                currentItem.MimeType = metadata["mimetype"];
                currentItem.Extension = metadata["extension"];
            }

            NeedsRefresh = false;
        }

        private async Task UploadS3Item(NotificationsList notifications, string bucketName, string filePath)
        {
            var uploadResult = await TustlerAWSLib.S3.UploadItem(bucketName, filePath);
            if (uploadResult.IsError)
            {
                notifications.HandleError(uploadResult);
            }
            else
            {
                var resultFlag = uploadResult.Result;
                var success = (resultFlag.HasValue && resultFlag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Upload {successStr}";
                notifications.ShowMessage(message, "Upload item to S3 task");

                Refresh(notifications);
            }
        }

        private async Task DownloadS3Item(NotificationsList notifications, string bucketName, string key, string filePath)
        {
            var downloadResult = await TustlerAWSLib.S3.DownloadItem(bucketName, key, filePath);
            if (downloadResult.IsError)
            {
                notifications.HandleError(downloadResult);
            }
            else
            {
                var resultFlag = downloadResult.Result;
                var success = (resultFlag.HasValue && resultFlag.Value);
                var successStr = success ? "succeeded" : "failed";
                var message = $"Download {successStr}";
                notifications.ShowMessage(message, "Download item to S3 task");
            }
        }

    }

    public class BucketItem
    {
        public string Key
        {
            get;
            set;
        }

        public long Size
        {
            get;
            set;
        }

        public DateTime LastModified
        {
            get;
            set;
        }

        public string Owner
        {
            get;
            set;
        }

        public string MimeType
        {
            get;
            set;
        }

        public string Extension
        {
            get;
            set;
        }
    }
}
