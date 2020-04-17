using Amazon.SimpleNotificationService.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TustlerInterfaces
{
    public interface IAmazonWebInterfaceSNS
    {
        public abstract Task<AWSResult<List<Topic>>> ListTopics();
        public abstract Task<AWSResult<string>> Publish(string topicARN, string message);
    }
}
