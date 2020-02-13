using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using TustlerAWSLib;

namespace Tustler.Models
{
    public sealed class NotificationsList
    {
        private ObservableCollection<Notification> notifications;

        public NotificationsList()
        {
            notifications = new ObservableCollection<Notification>();
        }

        public ObservableCollection<Notification> Notifications
        {
            get
            {
                return notifications;
            }
        }

        public void Clear()
        {
            notifications.Clear();
        }

        public void Add(Notification errorInfo)
        {
            notifications.Add(errorInfo);
        }

        public void HandleError<T>(AWSResult<T> result)
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

    }

}
