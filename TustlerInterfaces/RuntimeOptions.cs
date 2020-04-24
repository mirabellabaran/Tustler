using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerInterfaces
{
    public class RuntimeOptions
    {
        public RuntimeOptions()
        {
            IsMocked = false;
        }

        public bool IsMocked
        {
            get;
            set;
        }

        public string NotificationsARN
        {
            get;
            set;
        }

        public string NotificationsQueueURL
        {
            get;
            set;
        }
    }
}
