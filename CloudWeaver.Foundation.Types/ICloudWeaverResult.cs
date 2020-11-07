using System;
using System.Collections.Generic;
using System.Text;

namespace CloudWeaver.Foundation.Types
{
    public interface ICloudWeaverResult<T, E>
    {
        public bool IsError { get; }
        public T Result { get; }
        public E Exception { get; }
    }
}
