using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerFFMPEG
{
    public class AVInteropResult<T>
    {
        public AVInteropResult(T result, AVInteropException ex)
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

        public AVInteropException Exception { get; }
    }
}
