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

        public void Refresh(ApplicationErrorList errorList)
        {
            this.NeedsRefresh = true;
            Refresh(errorList, CurrentBucketName);
        }

        public async void Refresh(ApplicationErrorList errorList, string bucketName)
        {
            if (NeedsRefresh)
            {
                await FetchS3BucketItems(errorList, bucketName);
                CurrentBucketName = bucketName;
            }
        }

        public async void DeleteItem(ApplicationErrorList errorList, string key)
        {
            await DeleteBucketItem(errorList, CurrentBucketName, key);
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

        private async Task DeleteBucketItem(ApplicationErrorList errorList, string bucketName, string key)
        {
            var deleteResult = await TustlerAWSLib.S3.DeleteBucketItem(bucketName, key);

            if (deleteResult.IsError)
            {
                errorList.HandleError(deleteResult);
            }
            else
            {
                var success = deleteResult.Result;
                if (success.HasValue && success.Value)
                {
                    NeedsRefresh = true;
                    Refresh(errorList, bucketName);
                }
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

                this.FilteredMediaType = MediaType.All;
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
