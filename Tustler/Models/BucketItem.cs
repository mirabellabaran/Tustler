using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tustler.Models
{
    public class BucketItemViewModel
    {
        public ObservableCollection<BucketItem> BucketItems
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public BucketItemViewModel()
        {
            this.BucketItems = new ObservableCollection<BucketItem>();
            this.NeedsRefresh = true;
        }

        public async void Refresh(ApplicationErrorList errorList, string bucketName)
        {
            if (NeedsRefresh)
            {
                await FetchS3BucketItems(errorList, bucketName);
            }
        }

        private async Task FetchS3BucketItems(ApplicationErrorList errorList, string bucketName)
        {
            var bucketItemsResult = await TustlerAWSLib.S3.ListBucketItems(bucketName);
            if (bucketItemsResult.IsError)
            {
                errorList.HandleError(bucketItemsResult);
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

                        await FetchS3ItemMetadata(errorList, bucketName, item.Key);
                    }
                }

                NeedsRefresh = false;
            }
        }

        private async Task FetchS3ItemMetadata(ApplicationErrorList errorList, string bucketName, string key)
        {
            var metadataResult = await TustlerAWSLib.S3.GetItemMetadata(bucketName, key);
            if (metadataResult.IsError)
            {
                errorList.HandleError(metadataResult);
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
