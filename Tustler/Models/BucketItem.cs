using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Tustler.Models
{
    public class BucketItemsCollection: ObservableCollection<BucketItem>{
        Dictionary<string, BucketItem> keyLookup;

        public BucketItemsCollection() => keyLookup = new Dictionary<string, BucketItem>();

        public Dictionary<string, BucketItem>.KeyCollection Keys
        {
            get
            {
                return keyLookup.Keys;
            }
        }
        public void Add(string key, BucketItem item)
        {
            this.Add(item);
            keyLookup.Add(key, item);
        }

        public new void Clear()
        {
            base.Clear();
            keyLookup.Clear();
        }

        public void UpdateItem(string key, string mimetype, string extension)
        {
            BucketItem currentItem;
            keyLookup.TryGetValue(key, out currentItem);

            currentItem.MimeType = mimetype;
            currentItem.Extension = extension;

            using (this.BlockReentrancy())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }

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

        public BucketItemsCollection BucketItems
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
            this.BucketItems = new BucketItemsCollection();
            this.NeedsRefresh = true;
            this.CurrentBucketName = null;
        }

        public async Task RefreshAsync(NotificationsList notifications)
        {
            this.NeedsRefresh = true;
            await Refresh(notifications, CurrentBucketName).ConfigureAwait(true);
        }

        public async Task Refresh(NotificationsList notifications, string bucketName)
        {
            if (NeedsRefresh)
            {
                this.BucketItems.Clear();

                var bucketItemsResult = await TustlerAWSLib.S3.ListBucketItems(bucketName).ConfigureAwait(true);
                ProcessS3BucketItems(notifications, bucketItemsResult);
                CurrentBucketName = bucketName;

                foreach (var key in this.BucketItems.Keys) {
                    var metadataResult = await TustlerAWSLib.S3.GetItemMetadata(bucketName, key).ConfigureAwait(true);
                    ProcessS3ItemMetadata(notifications, metadataResult, key);
                }
            }
        }

        public async Task DeleteItem(NotificationsList notifications, string key)
        {
            await DeleteBucketItem(notifications, CurrentBucketName, key).ConfigureAwait(false);
        }

        public async Task UploadItem(NotificationsList notifications, string filePath, string mimetype, string extension)
        {
            await UploadS3Item(notifications, CurrentBucketName, filePath, mimetype, extension).ConfigureAwait(false);
        }

        public async Task DownloadItem(NotificationsList notifications, string key, string filePath)
        {
            await DownloadS3Item(notifications, CurrentBucketName, key, filePath).ConfigureAwait(false);
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
            var deleteResult = await TustlerAWSLib.S3.DeleteBucketItem(bucketName, key).ConfigureAwait(false);

            if (deleteResult.IsError)
            {
                notifications.HandleError(deleteResult);
            }
            else
            {
                var success = deleteResult.Result;
                if (success.HasValue && success.Value)
                {
                    await RefreshAsync(notifications).ConfigureAwait(false);
                }
            }
        }

        private void ProcessS3BucketItems(NotificationsList notifications, TustlerAWSLib.AWSResult<List<S3Object>> bucketItemsResult)
        {
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
                    for (var i = 0; i < data.Count; i++)
                    {
                        this.BucketItems.Add(data[i].Key, data[i]);
                    }
                }

                this.FilteredMediaType = MediaType.All;
                NeedsRefresh = false;
            }
        }

        private void ProcessS3ItemMetadata(NotificationsList notifications, TustlerAWSLib.AWSResult<MetadataCollection> metadataResult, string key)
        {
            if (metadataResult.IsError)
            {
                notifications.HandleError(metadataResult);
                NeedsRefresh = true;
            }
            else
            {
                var metadata = metadataResult.Result;

                // patch the current item with the returned metadata
                this.BucketItems.UpdateItem(key, metadata["mimetype"], metadata["extension"]);
            }

            NeedsRefresh = false;
        }

        private async Task UploadS3Item(NotificationsList notifications, string bucketName, string filePath, string mimetype, string extension)
        {
            var uploadResult = await TustlerAWSLib.S3.UploadItem(bucketName, filePath, mimetype, extension).ConfigureAwait(false);
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

                await RefreshAsync(notifications).ConfigureAwait(false);
            }
        }

        private async static Task DownloadS3Item(NotificationsList notifications, string bucketName, string key, string filePath)
        {
            var downloadResult = await TustlerAWSLib.S3.DownloadItem(bucketName, key, filePath).ConfigureAwait(false);
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
