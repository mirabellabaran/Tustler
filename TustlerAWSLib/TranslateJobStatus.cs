using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerAWSLib
{
    public class TranslateJobStatus
    {
        public TranslateJobStatus(string jobId, string jobStatus)
        {
            this.JobId = jobId;
            this.JobStatus = jobStatus;
        }

        public string JobId
        {
            get;
            internal set;
        }

        public string JobStatus
        {
            get;
            internal set;
        }
    }
}
