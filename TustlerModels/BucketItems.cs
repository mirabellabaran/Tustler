using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels
{
    public class BucketItemsCollection: ObservableCollection<BucketItem>
    {
        readonly Dictionary<string, BucketItem> keyLookup;

        public BucketItemsCollection() => keyLookup = new Dictionary<string, BucketItem>();

        public BucketItemsCollection(IEnumerable<BucketItem> items) : this()
        {
            foreach (var item in items)
            {
                Add(item.Key, item);
            }
        }

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
        public BucketItemViewModel()
        {
            this.BucketItems = new BucketItemsCollection();
            this.NeedsRefresh = true;
            this.CurrentBucketName = null;
        }

        public BucketItemsCollection BucketItems
        {
            get;
            private set;
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

        private void ProcessS3BucketItems(NotificationsList notifications, AWSResult<List<S3Object>> bucketItemsResult)
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

                NeedsRefresh = false;
            }
        }

        private void ProcessS3ItemMetadata(NotificationsList notifications, AWSResult<MetadataCollection> metadataResult, string key)
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
