using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerInterfaces
{
    public class AWSResult<T> : ICloudWeaverResult<T, ICloudWeaverException>
    {
        public AWSResult(T result, AWSException ex)
        {
            this.Result = result;
            this.Exception = ex;
        }

        public bool IsError
        {
            get
            {
                return this.Exception is object;
            }
        }

        public T Result { get; }

        public ICloudWeaverException Exception { get; }
    }
}
