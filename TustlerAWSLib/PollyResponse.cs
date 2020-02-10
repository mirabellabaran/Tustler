using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TustlerAWSLib
{
    public class PollyResponse
    {
        public PollyResponse(MemoryStream audioStream, string contentType, long contentLength)
        {
            this.AudioStream = audioStream;
            this.ContentType = contentType;
            this.ContentLength = contentLength;
        }

        public MemoryStream AudioStream
        {
            get;
            internal set;
        }

        public string ContentType
        {
            get;
            internal set;
        }

        public long ContentLength
        {
            get;
            internal set;
        }
    }
}
