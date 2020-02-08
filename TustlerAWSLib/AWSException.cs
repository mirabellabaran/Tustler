using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerAWSLib
{
    public class AWSException : ApplicationException
    {
        public AWSException()
        {

        }

        //public AWSException(string message) : base(message)
        //{

        //}

        //public AWSException(string message, Exception innerException) : base(message, innerException)
        //{

        //}

        public AWSException(string context, string message, Exception innerException) : base(message, innerException)
        {
            this.Context = context;
        }

        /// <summary>
        /// The called function context (site of the exception)
        /// </summary>
        public string Context
        {
            get;
            internal set;
        }
    }
}
