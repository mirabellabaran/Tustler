using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerInterfaces
{
    public class SNSNotificationMessage
    {
        public string Type { get; set; }
        public string MessageId { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
    }
}
