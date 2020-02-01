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
            if (result.Exception is HttpRequestException)
            {
                this.Add(new ApplicationErrorInfo { Message = "Not connected", Exception = result.Exception });
            }
            else
            {
                this.Add(new ApplicationErrorInfo { Message = result.Exception.Message, Exception = result.Exception });
            }
        }

        public void ShowMessage(string message, string detail)
        {
            this.Add(new ApplicationMessageInfo { Message = message });
        }
    }

    public abstract class Notification
    {
    }

    public sealed class ApplicationErrorInfo : Notification
    {
        public string Message
        {
            get;
            set;
        }

        public Exception Exception
        {
            get;
            set;
        }

    }

    public sealed class ApplicationMessageInfo : Notification
    {
        public string Message
        {
            get;
            set;
        }

    }

}
