#nullable enable
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public class NotificationServices
    {
        // key is a messageId or taskId
        private readonly Dictionary<string, MatchedAction> notificationTasks;

        /// <summary>
        /// Wraps a matching function and a related action
        /// </summary>
        private class MatchedAction
        {
            public Func<string, SNSNotificationMessage, bool>? Match { get; internal set; }
            public Action<object?>? Action { get; internal set; }
        }

        public NotificationServices()
        {
            notificationTasks = new Dictionary<string, MatchedAction>();
        }

        /// <summary>
        /// Wait on a notification message on the input queue that contains a message body that references the supplied task Id
        /// The expected message is added to an internal dictionary
        /// </summary>
        /// <param name="notifications">A reference to the global notifications list</param>
        /// <param name="taskId">The task Id to match</param>
        /// <param name="continuation">An action to perform if a matching message is found</param>
        /// <returns></returns>
        public async Task<bool> WaitOnTaskStateChanged(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string taskId, Action<object?> continuation)
        {
            notificationTasks.Add(taskId, new MatchedAction { Match = MatchTaskChangedState, Action = continuation });
            return await WaitOnMessage(awsInterface, notifications).ConfigureAwait(true);
        }

        /// <summary>
        /// Wait on a notification message on the input queue that matches the supplied message Id
        /// The expected message is added to an internal dictionary
        /// </summary>
        /// <param name="notifications">A reference to the global notifications list</param>
        /// <param name="messageId">The message Id to match</param>
        /// <param name="continuation">An action to perform if a matching message is found</param>
        /// <returns></returns>
        public async Task<bool> WaitOnReceivedMessage(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string messageId, Action<object?> continuation)
        {
            notificationTasks.Add(messageId, new MatchedAction { Match = MatchReceivedMessage, Action = continuation });
            return await WaitOnMessage(awsInterface, notifications).ConfigureAwait(true);
        }

        /// <summary>
        /// Test the notifications service by publishing a message via SNS and receiving it on the input queue
        /// </summary>
        /// <param name="notifications">A reference to the global notifications list</param>
        /// <returns></returns>
        public async Task<bool> TestNotifications(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            static async Task<AWSResult<string>> PublishMessage(AmazonWebServiceInterface awsInterface)
            {
                var arn = ApplicationSettings.NotificationsARN;
                return await awsInterface.SNS.Publish(arn, "Test message").ConfigureAwait(true);
            }

            var result = await PublishMessage(awsInterface).ConfigureAwait(true);
            if (result.IsError)
            {
                notifications.HandleError("TestNotifications", "An error occurred when publishing a message.", result.Exception.InnerException);
                return false;
            }
            else
            {
                var messageId = result.Result;

                void ContinuationTask(object? msg)
                {
                    var message = msg as SNSNotificationMessage;
                    notifications.ShowMessage("Test succeeded", $"Received a message with content \"{message!.Message}\" on the input queue.");
                }
                return await WaitOnReceivedMessage(awsInterface, notifications, messageId, ContinuationTask).ConfigureAwait(true);
            }
        }

        public async Task<bool> WaitOnMessage(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            var queueUrl = ApplicationSettings.NotificationsQueue;
            SNSNotificationMessage? message;

            var result = await awsInterface.SQS.ReceiveMessage(queueUrl).ConfigureAwait(true);
            if (result.IsError)
            {
                notifications.HandleError("WaitOnNotification", "An error occurred when waiting on a notification.", result.Exception.InnerException);
                message = null;
            }
            else
            {
                // result is null if the operation timed out
                message = (result.Result is null) ? null : JsonSerializer.Deserialize<SNSNotificationMessage>(result.Result);
            }

            if (message != null)
            {
                // look for a match and return the related action
                var matched = notificationTasks.Where(kvp => kvp.Value.Match(kvp.Key, message)).ToArray();
                if (matched.Length > 0)
                {
                    string key = matched[0].Key;
                    var action = matched[0].Value.Action;

                    // invoke the action and remove the kvp from the notification tasks list
                    using var cancellationTokenSource = new CancellationTokenSource();
                    await Task.Factory.StartNew(action, message, cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);

                    notificationTasks.Remove(key);
                }
                else
                {
                    // unmatched message
                    var detail = $"The following message was unmatched. Type = {message.Type}; MessageId = {message.MessageId}; Content = {message.Message}";
                    notifications.ShowMessage("Unmatched message.", detail);
                }
            }

            return (notificationTasks.Count > 0);
        }

        private bool MatchTaskChangedState(string taskId, SNSNotificationMessage message)
        {
            // return true if a notification message is received that contains a reference to the given task Id in the message body
            return (message.Type == "Notification" && message.Message.Contains(taskId, StringComparison.InvariantCulture));
        }

        private bool MatchReceivedMessage(string messageId, SNSNotificationMessage message)
        {
            // return true if a notification message is received with the given message Id
            return (message.Type == "Notification" && message.MessageId == messageId);
        }
    }
}
