using System;
using System.Collections.ObjectModel;

namespace CloudWeaver.Foundation.Types
{
    public sealed class NotificationsList
    {
        public NotificationsList()
        {
            Notifications = new ObservableCollection<Notification>();
        }

        public ObservableCollection<Notification> Notifications { get; }

        public void Clear()
        {
            Notifications.Clear();
        }

        public void Add(Notification notification)
        {
            Notifications.Add(notification);
        }

        public void HandleError<T>(ICloudWeaverResult<T, ICloudWeaverException> result)
        {
            var ex = result.Exception;

            this.Add(new ApplicationErrorInfo { Context = ex.Context, Message = ex.Message, Exception = ex.InnerException});
        }

        public void HandleError<T>(string context, string message, T ex) where T: Exception
        {
            this.Add(new ApplicationErrorInfo { Context = context, Message = message, Exception = ex });
        }

        public void ShowMessage(string message, string detail)
        {
            this.Add(new ApplicationMessageInfo { Message = message, Detail = detail });
        }

        public static ApplicationErrorInfo CreateErrorNotification(string context, string message, Exception exception)
        {
            return new ApplicationErrorInfo() { Context = context, Message = message, Exception = exception };
        }
    }

    public abstract class Notification
    {
    }

    public sealed class ApplicationErrorInfo : Notification
    {
        public string Context
        {
            get;
            internal set;
        }

        public string Message
        {
            get;
            internal set;
        }

        public Exception Exception
        {
            get;
            internal set;
        }

        public override string ToString()
        {
            return $"Context={Context}; Message={Message}; Exception={Exception}";
        }
    }

    public sealed class ApplicationMessageInfo : Notification
    {
        public string Message
        {
            get;
            internal set;
        }

        public string Detail
        {
            get;
            internal set;
        }

        public override string ToString()
        {
            return $"Message={Message}; Detail={Detail}";
        }
    }
}
