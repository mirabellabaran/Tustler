using Amazon.S3.Model;
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels
{
    public enum BucketItemViewModelMode
    {
        Standard,
        ConfirmDelete,
        DownloadPrompt
    }

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
            if (keyLookup.TryGetValue(key, out BucketItem currentItem))
            {
                currentItem.MimeType = mimetype;
                currentItem.Extension = extension;

                using (this.BlockReentrancy())
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        internal void DeleteKey(string key)
        {
            if (keyLookup.TryGetValue(key, out BucketItem currentItem))
            {
                Remove(currentItem);
            }
        }
    }

    public class BucketItemViewModel : INotifyPropertyChanged, IDeletableViewModelItem, INotifiableViewModel<Notification>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BucketItemViewModel()
        {
            this.BucketItems = new BucketItemsCollection();
            this.NeedsRefresh = true;
            this.CurrentBucketName = null;

            this.NotificationsList = new ObservableCollection<Notification>();
        }

        public BucketItemsCollection BucketItems
        {
            get;
            private set;
        }

        public ObservableCollection<Notification> NotificationsList
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

        /// <summary>
        /// Delete the specified BucketItem from the view
        /// </summary>
        /// <remarks>An alternative is to call ForceRefresh after deleting an item</remarks>
        /// <param name="key"></param>
        public void DeleteItem(string key)
        {
            BucketItems.DeleteKey(key);
        }

        public async Task ForceRefresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string bucketName)
        {
            this.NeedsRefresh = true;
            await Refresh(awsInterface, notifications, bucketName).ConfigureAwait(true);
        }

        public async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string bucketName)
        {
            if (NeedsRefresh)
            {
                this.BucketItems.Clear();

                var bucketItemsResult = await awsInterface.S3.ListBucketItems(bucketName).ConfigureAwait(true);
                ProcessS3BucketItems(notifications, bucketItemsResult, bucketName);
                CurrentBucketName = bucketName;

                foreach (var key in this.BucketItems.Keys) {
                    var metadataResult = await awsInterface.S3.GetItemMetadata(bucketName, key).ConfigureAwait(true);
                    ProcessS3ItemMetadata(notifications, metadataResult, key);
                }
            }
        }

        private void ProcessS3BucketItems(NotificationsList notifications, AWSResult<List<S3Object>> bucketItemsResult, string bucketName)
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
                    var items = from item in bucketItems select new BucketItem { Key = item.Key, BucketName = bucketName, Size = item.Size, LastModified = item.LastModified, Owner = item.Owner?.DisplayName };

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

        public string BucketName
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
