using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerFFMPEG
{
    public class AVInteropException : ApplicationException
    {
        public AVInteropException()
        {

        }

        public AVInteropException(string context, int errorCode, string message) : base(message)
        {
            this.Context = context;
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// The called function context (site of the exception)
        /// </summary>
        public string Context
        {
            get;
            internal set;
        }

        /// <summary>
        /// The error code
        /// </summary>
        public int ErrorCode
        {
            get;
            internal set;
        }
    }
}
