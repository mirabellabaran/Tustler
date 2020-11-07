using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerInterfaces
{
    public class RuntimeOptions : IRuntimeOptions
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
