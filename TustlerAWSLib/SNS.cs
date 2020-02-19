using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TustlerAWSLib
{
    public class SNS
    {
        /// <summary>
        /// List available SNS topics
        /// </summary>
        /// <returns>A list of topics</returns>
        public async static Task<AWSResult<List<Topic>>> ListTopics()
        {
            try
            {
                using (var client = new AmazonSimpleNotificationServiceClient())
                {
                    var request = new ListTopicsRequest();
                    var result = new List<Topic>();
                    ListTopicsResponse response;
                    do
                    {
                        response = await client.ListTopicsAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.Topics);
                    } while (response.NextToken != null);

                    return new AWSResult<List<Topic>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<Topic>>(null, new AWSException(nameof(ListTopics), "Not connected.", ex));
            }
            catch (AuthorizationErrorException ex)
            {
                return new AWSResult<List<Topic>>(null, new AWSException(nameof(ListTopics), "No authorization to access this resource.", ex));
            }
            catch (InternalErrorException ex)
            {
                return new AWSResult<List<Topic>>(null, new AWSException(nameof(ListTopics), "An internal service error has occurred.", ex));
            }
        }

        /// <summary>
        /// Send a message to the specified topic
        /// </summary>
        /// <param name="topicARN">The address of the topic</param>
        /// <param name="message">The text message to send</param>
        /// <returns>The message ID</returns>
        public async static Task<AWSResult<string>> Publish(string topicARN, string message)
        {
            try
            {
                using (var client = new AmazonSimpleNotificationServiceClient())
                {
                    var request = new PublishRequest
                    {
                        Message = message,
                        TopicArn = topicARN
                    };
                    var response = await client.PublishAsync(request);

                    // return the MessageId
                    return new AWSResult<string>(response.MessageId, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(Publish), "Not connected.", ex));
            }
            catch (AuthorizationErrorException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(Publish), "No authorization to access this resource.", ex));
            }
            catch (InternalErrorException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(Publish), "An internal service error has occurred.", ex));
            }
        }

    }
}
