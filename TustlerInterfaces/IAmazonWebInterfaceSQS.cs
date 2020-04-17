using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TustlerInterfaces
{
    public interface IAmazonWebInterfaceSQS
    {
        public abstract Task<AWSResult<List<string>>> ListQueues();
        public abstract Task<AWSResult<string>> ReceiveMessage(string queueUrl);
    }
}
