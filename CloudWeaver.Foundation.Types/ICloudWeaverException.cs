using System;
using System.Collections.Generic;
using System.Text;

namespace CloudWeaver.Foundation.Types
{
    public interface ICloudWeaverException
    {
        public string Context { get; }
        public string Message { get; }
        public Exception InnerException { get; }
    }
}
