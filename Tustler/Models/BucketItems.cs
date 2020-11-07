using CloudWeaver.Foundation.Types;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using TustlerAWSLib;
using TustlerModels;

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

        public new async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string bucketName)
        {
            await base.Refresh(awsInterface, notifications, bucketName).ConfigureAwait(true);

            this.FilteredMediaType = BucketItemMediaType.All;
        }

        internal static bool IsRequiredMediaType(BucketItemMediaType selectedMediaType, BucketItem item)
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

        internal static bool IsRequiredExtension(string extension, BucketItem item)
        {
            if (item.Extension is null)
            {
                return false;
            }
            else
            {
                return item.Extension == extension;
            }
        }

        /// <summary>
        /// Set a filter on the view, showing only the specified mime types e.g. audio
        /// </summary>
        /// <param name="requiredMediaType"></param>
        private void SetFilter(BucketItemMediaType selectedMediaType)
        {
            BucketItemsView.Filter = new Predicate<object>(item => IsRequiredMediaType(selectedMediaType, (item as BucketItem)));
            BucketItemsView.Refresh();
        }
    }

    /// <summary>
    /// A local filterable instance that consumes bucket items from the global instance
    /// </summary>
    /// <remarks>Used to select items by mimetype (e.g. audio media for transcription) or to select items by their extension</remarks>
    public class FilteredBucketItemViewModel
    {
        public FilteredBucketItemViewModel()
        {
            BucketItems = new ObservableCollection<BucketItem>();
        }

        public ObservableCollection<BucketItem> BucketItems
        {
            get;
            private set;
        }

        public void Clear()
        {
            BucketItems.Clear();
        }

        public void Select(BucketItemViewModel bucketItemViewModel, BucketItemMediaType selectedMediaType)
        {
            if (bucketItemViewModel is null) throw new ArgumentNullException(nameof(bucketItemViewModel));

            var filtered = bucketItemViewModel.BucketItems.Where(item => BucketItemViewSourceModel.IsRequiredMediaType(selectedMediaType, item));
            foreach (var item in filtered)
            {
                BucketItems.Add(item);
            }
        }

        public void Select(BucketItemViewModel bucketItemViewModel, string extension)
        {
            if (bucketItemViewModel is null) throw new ArgumentNullException(nameof(bucketItemViewModel));

            var filtered = bucketItemViewModel.BucketItems.Where(item => BucketItemViewSourceModel.IsRequiredExtension(extension, item));
            foreach (var item in filtered)
            {
                BucketItems.Add(item);
            }
        }
    }
}
