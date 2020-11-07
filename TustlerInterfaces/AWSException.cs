using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerInterfaces
{
    public class AWSException : ApplicationException, ICloudWeaverException
    {
        public AWSException()
        {

        }

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
