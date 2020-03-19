using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib
{
    public class SQS
    {
        /// <summary>
        /// Returns a list of all queues
        /// </summary>
        /// <returns>A list of queue URLs</returns>
        public async static Task<AWSResult<List<string>>> ListQueues()
        {
            try
            {
                using (var client = new AmazonSQSClient())
                {
                    var response = await client.ListQueuesAsync(new ListQueuesRequest());

                    return new AWSResult<List<string>>(response.QueueUrls, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<string>>(null, new AWSException(nameof(ListQueues), "Not connected.", ex));
            }
        }

        /// <summary>
        /// Wait on a single message from the specified queue in long poll mode
        /// </summary>
        /// <param name="queueUrl">The queue to wait on</param>
        /// <returns>The message body or null</returns>
        public async static Task<AWSResult<string>> ReceiveMessage(string queueUrl)
        {
            try
            {
                using (var client = new AmazonSQSClient())
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = 1,
                        VisibilityTimeout = 20, // seconds
                        WaitTimeSeconds = 20    // seconds
                    };
                    var response = await client.ReceiveMessageAsync(request);
                    var messages = response.Messages;

                    // requested maximum of one message in the request
                    if (messages.Count > 0)
                    {
                        var message = messages[0];
                        var body = message.Body;

                        // delete the message
                        _ = await client.DeleteMessageAsync(queueUrl, message.ReceiptHandle);

                        return new AWSResult<string>(body, null);
                    }
                    else
                    {
                        return new AWSResult<string>(null, null);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(ReceiveMessage), "Not connected.", ex));
            }
            catch (OverLimitException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(ReceiveMessage), "The maximum number of inflight messages has been reached.", ex));
            }
            catch (ReceiptHandleIsInvalidException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(ReceiveMessage), "The specified receipt handle isn't valid.", ex));
            }
        }
    }
}
