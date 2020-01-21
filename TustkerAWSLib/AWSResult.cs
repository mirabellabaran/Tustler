using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerAWSLib
{
    //public class AWSException<T> where T:Exception
    //{
    //    T ex;

    //    public AWSException(T ex)
    //    {
    //        this.ex = ex;
    //    }

    //    public Exception Exception
    //    {
    //        get
    //        {
    //            return ex;
    //        }
    //    }
    //}

    public class AWSResult<T>
    {
        T result;
        Exception ex;

        public AWSResult(T result, Exception ex)
        {
            this.result = result;
            this.ex = ex;
        }

        public bool IsError
        {
            get
            {
                return this.ex != null;
            }
        }

        public T Result
        {
            get
            {
                return this.result;
            }
        }

        public Exception Exception
        {
            get
            {
                return this.ex;
            }
        }
    }
}
