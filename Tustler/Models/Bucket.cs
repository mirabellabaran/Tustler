using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tustler.Models
{
    public class BucketViewModel
    {
        public ObservableCollection<Bucket> Buckets
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public BucketViewModel()
        {
            this.Buckets = new ObservableCollection<Bucket>();
            this.NeedsRefresh = true;
        }

        public async void Refresh(NotificationsList errorList)
        {
            if (NeedsRefresh)
            {
                await FetchS3Buckets(errorList);
            }
        }

        private async Task FetchS3Buckets(NotificationsList errorList)
        {
            var bucketsResult = await TustlerAWSLib.S3.ListBuckets();
            if (bucketsResult.IsError)
            {
                errorList.HandleError(bucketsResult);
            }
            else
            {
                var buckets = bucketsResult.Result;
                if (buckets.Count > 0)
                {
                    static void AppendBucketCollection(ObservableCollection<Bucket> collection, List<Amazon.S3.Model.S3Bucket> buckets)
                    {
                        var bucketModelItems = from bucket in buckets select new Bucket { Name = bucket.BucketName, CreationDate = bucket.CreationDate };

                        collection.Clear();
                        foreach (var bucket in bucketModelItems)
                        {
                            collection.Add(bucket);
                        }
                    };
                    AppendBucketCollection(this.Buckets, buckets);
                }

                NeedsRefresh = false;
            }
        }

    }

    public class Bucket
    {
        public string Name
        {
            get;
            set;
        }

        public DateTime CreationDate
        {
            get;
            set;
        }
    }
}
