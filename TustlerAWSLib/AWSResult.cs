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

    //    public T Exception
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
        AWSException ex;

        public AWSResult(T result, AWSException ex)
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

        public AWSException Exception
        {
            get
            {
                return this.ex;
            }
        }
    }
}
