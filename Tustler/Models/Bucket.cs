using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Tustler.Models
{
    public class BucketViewModel
    {
        public ObservableCollection<Bucket> Buckets
        {
            get;
            set;
        }

        public BucketViewModel()
        {
            //var buckets = new Bucket[]{
            //    new Bucket { Name = "Mira", CreationDate = DateTime.Now },
            //    new Bucket { Name = "Helen", CreationDate = DateTime.Now }
            //    };
            //this.Buckets = new ObservableCollection<Bucket>(buckets);

            this.Buckets = new ObservableCollection<Bucket>();
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
