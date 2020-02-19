using Amazon.Polly;
using Amazon.Polly.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tustler.Models;
using TustlerAWSLib;

namespace Tustler.Helpers
{
    /// <summary>
    /// Manages Polly services
    /// </summary>
    public static class PollyServices
    {
        public static async Task<AWSResult<PollyAudioStream>> SynthesizeSpeech(string text, bool useNeural, string voiceId)
        {
            return await Polly.SynthesizeSpeech(text, useNeural ? Engine.Neural : Engine.Standard, voiceId is null ? "Joanna" : voiceId).ConfigureAwait(true);
        }

        public static (MemoryStream AudioStream, string ContentType) ProcessSynthesizeSpeechResult(NotificationsList notifications, AWSResult<PollyAudioStream> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return (null, null);
            }
            else
            {
                var response = result.Result;

                return (response.AudioStream, response.ContentType);
            }
        }

        private static async Task<AWSResult<NotificationMessage>> WaitOnMessage()
        {
            var queueUrl = ApplicationSettings.NotificationsQueue;

            // try receiving a message until a non-null result is returned
            NotificationMessage message = null;
            do
            {
                var result = await SQS.ReceiveMessage(queueUrl).ConfigureAwait(true);
                if (result.IsError)
                    return new AWSResult<NotificationMessage>(null, result.Exception);
                else
                    message = (result.Result is null) ? null : JsonSerializer.Deserialize<NotificationMessage>(result.Result);
            } while (message == null);

            return new AWSResult<NotificationMessage>(message, null);
        }

        public static async Task<bool> WaitOnNotification(NotificationsList notifications, string taskId)
        {
            var result = await WaitOnMessage().ConfigureAwait(true);
            if (result.IsError)
            {
                notifications.HandleError("WaitOnNotification", "An error occurred when waiting on a notification.", result.Exception);
                return false;
            }
            else
            {
                var message = result.Result;
                if (message.Type == "Notification" && message.Message.Contains(taskId, StringComparison.InvariantCulture))
                    return true;
                else
                    return false;
            }
        }

        public static async Task<AWSResult<bool>> TestNotifications()
        {
            var arn = ApplicationSettings.NotificationsARN;
            string messageId;

            AWSResult<string> publishResult = await SNS.Publish(arn, "Test message").ConfigureAwait(true);
            if (publishResult.IsError)
                return new AWSResult<bool>(false, publishResult.Exception);
            else
                messageId = publishResult.Result;

            AWSResult<NotificationMessage> result = await WaitOnMessage().ConfigureAwait(true);
            if (result.IsError)
            {
                return new AWSResult<bool>(false, result.Exception);
            }
            else
            {
                var message = result.Result;
                if (message.Type == "Notification" && message.MessageId == messageId)
                    return new AWSResult<bool>(true, null);
                else
                    return new AWSResult<bool>(false, null);
            }
        }

        public static void ProcessTestNotificationsResult(NotificationsList notifications, AWSResult<bool> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var status = result.Result ? "succeeded" : "failed";
                var message = $"Test {status}";

                notifications.ShowMessage(message, "A test message was published via SNS and received on the input queue.");
            }
        }

    }

    public class NotificationMessage
    {
        public string Type { get; set; }
        public string MessageId { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
    }
}
