using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels
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

        public async Task Refresh(AmazonWebServiceInterface awsInterface, bool forceRefresh, NotificationsList notifications)
        {
            if (NeedsRefresh || forceRefresh)
            {
                // the underlying collection must be refreshed from the Dispatcher thread, not from within the awaited method
                var bucketsResult = await awsInterface.S3.ListBuckets().ConfigureAwait(true);
                ProcessS3Buckets(notifications, bucketsResult);
            }
        }

        private void ProcessS3Buckets(NotificationsList errorList, AWSResult<List<Amazon.S3.Model.S3Bucket>> bucketsResult)
        {
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
