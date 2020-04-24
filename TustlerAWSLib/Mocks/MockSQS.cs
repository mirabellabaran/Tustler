using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib.Mocks
{
    public class MockSQS : IAmazonWebInterfaceSQS
    {
        private readonly string notificationsQueueURL;

        private readonly Queue<string> notificationsQueue;

        public MockSQS(string notificationsQueueURL)
        {
            this.notificationsQueueURL = notificationsQueueURL;

            this.notificationsQueue = new Queue<string>();
        }

        // Used by MockPolly and MockSNS to add an expected SQS message
        public void AddExpectedMessage(string serializedMessage)
        {
            notificationsQueue.Enqueue(serializedMessage);
        }

        public async Task<AWSResult<List<string>>> ListQueues()
        {
            await Task.Delay(1000);

            var queues = new List<string>() { notificationsQueueURL };
            return await Task.FromResult(new AWSResult<List<string>>(queues, null));
        }

        public async Task<AWSResult<string>> ReceiveMessage(string queueUrl)
        {
            await Task.Delay(1000);

            // return a message if available, otherwise null
            var message = (notificationsQueue.TryDequeue(out string msg)) switch
            {
                true => msg,
                false => null
            };

            return await Task.FromResult(new AWSResult<string>(message, null));
        }
    }
}
