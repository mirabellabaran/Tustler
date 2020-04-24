using Amazon.SimpleNotificationService.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib.Mocks
{
    public class MockSNS : IAmazonWebInterfaceSNS
    {
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly string notificationsARN;

        public MockSNS(AmazonWebServiceInterface awsInterface, string notificationsARN)
        {
            this.awsInterface = awsInterface;
            this.notificationsARN = notificationsARN;
        }

        public async Task<AWSResult<List<Topic>>> ListTopics()
        {
            await Task.Delay(1000);

            var topics = new List<Topic>() { new Topic() { TopicArn = notificationsARN } };
            return await Task.FromResult(new AWSResult<List<Topic>>(topics, null));
        }

        public async Task<AWSResult<string>> Publish(string topicARN, string message)
        {
            await Task.Delay(1000);

            var messageId = Guid.NewGuid().ToString();
            var msgInstance = new SNSNotificationMessage()
            {
                MessageId = messageId,
                Message = message,
                Timestamp = DateTime.Now.Ticks.ToString(),
                Type = "Notification"
            };

            var msgString = JsonSerializer.Serialize<SNSNotificationMessage>(msgInstance);
            var mockSQS = awsInterface.SQS as MockSQS;
            mockSQS.AddExpectedMessage(msgString);

            return await Task.FromResult(new AWSResult<string>(messageId, null));
        }
    }
}
