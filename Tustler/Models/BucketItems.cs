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
using TustlerInterfaces;
using TustlerModels;
using TustlerServicesLib;

namespace Tustler.Models
{
    public enum BucketItemMediaType
    {
        All,
        Audio,
        Video,
        Text,
        Defined     // the mime type is non-null
    }

    /// <summary>
    /// Wraps TustlerModels.BucketItemViewModel adding an ICollectionView property for filtering 
    /// </summary>
    /// <remarks>The dependency on System.Windows.Data requires PresentationFramework.dll</remarks>
    public class BucketItemViewSourceModel : BucketItemViewModel
    {
        private BucketItemMediaType filteredMediaType;

        public BucketItemViewSourceModel() : base()
        {
        }

        public ICollectionView BucketItemsView
        {
            get { return CollectionViewSource.GetDefaultView(BucketItems); }
        }

        public BucketItemMediaType FilteredMediaType
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

        public async Task Refresh(NotificationsList notifications, string bucketName)
        {
            await base.Refresh(notifications, bucketName).ConfigureAwait(true);

            this.FilteredMediaType = BucketItemMediaType.All;
        }

        internal static bool IsRquiredMediaType(BucketItemMediaType selectedMediaType, BucketItem item)
        {
            static bool CheckTypeIs(string mimeType, string type)
            {
                if (mimeType == null)
                    return false;
                else
                    return mimeType.Contains(type, StringComparison.InvariantCulture);
            }

            return selectedMediaType switch
            {
                BucketItemMediaType.All => true,
                BucketItemMediaType.Audio => CheckTypeIs(item.MimeType, "audio"),
                BucketItemMediaType.Video => CheckTypeIs(item.MimeType, "video"),
                BucketItemMediaType.Text => CheckTypeIs(item.MimeType, "text"),
                BucketItemMediaType.Defined => (item.MimeType != null),
                _ => true
            };
        }

        /// <summary>
        /// Set a filter on the view, showing only the specified mime types e.g. audio
        /// </summary>
        /// <param name="requiredMediaType"></param>
        private void SetFilter(BucketItemMediaType selectedMediaType)
        {
            BucketItemsView.Filter = new Predicate<object>(item => IsRquiredMediaType(selectedMediaType, (item as BucketItem)));
            BucketItemsView.Refresh();
        }
    }

    /// <summary>
    /// A local filterable instance used by the Transcribe functions that consumes bucket items from the global instance
    /// </summary>
    /// <remarks>Used to select audio media for transcription</remarks>
    public class MediaFilteredBucketItemViewModel
    {
        public MediaFilteredBucketItemViewModel()
        {
            BucketItems = new ObservableCollection<BucketItem>();
        }

        public ObservableCollection<BucketItem> BucketItems
        {
            get;
            private set;
        }

        public void Select(BucketItemViewModel bucketItemViewModel, BucketItemMediaType selectedMediaType)
        {
            var filtered = bucketItemViewModel.BucketItems.Where(item => BucketItemViewSourceModel.IsRquiredMediaType(selectedMediaType, (item as BucketItem)));
            foreach (var item in filtered)
            {
                BucketItems.Add(item);
            }
        }
    }
}
