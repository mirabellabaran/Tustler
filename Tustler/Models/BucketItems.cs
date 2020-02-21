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
        readonly Dictionary<string, BucketItem> keyLookup;

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
            keyLookup.TryGetValue(key, out BucketItem currentItem);

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
