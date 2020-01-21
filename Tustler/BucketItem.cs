using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Tustler
{
    public class BucketItemViewModel
    {
        public ObservableCollection<BucketItem> BucketItems
        {
            get;
            set;
        }

        public BucketItemViewModel()
        {
            this.BucketItems = new ObservableCollection<BucketItem>();
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
    }
}
