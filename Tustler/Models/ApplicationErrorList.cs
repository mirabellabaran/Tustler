using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using TustlerAWSLib;

namespace Tustler.Models
{
    public sealed class ApplicationErrorList
    {
        private ObservableCollection<ApplicationErrorInfo> errors;

        public ApplicationErrorList()
        {
            errors = new ObservableCollection<ApplicationErrorInfo>();
        }

        public ObservableCollection<ApplicationErrorInfo> Errors
        {
            get
            {
                return errors;
            }
        }

        public void Clear()
        {
            errors.Clear();
        }

        public void Add(ApplicationErrorInfo errorInfo)
        {
            errors.Add(errorInfo);
        }

        public void HandleError<T>(AWSResult<T> result)
        {
            if (result.Exception is HttpRequestException)
            {
                this.Add(new ApplicationErrorInfo { Message = "Not connected", DetailVisible = false, Exception = result.Exception });
            }
            else
            {
                this.Add(new ApplicationErrorInfo { Message = result.Exception.Message, DetailVisible = false, Exception = result.Exception });
            }
        }

    }

    public sealed class ApplicationErrorInfo
    {
        public string Message
        {
            get;
            set;
        }

        public bool DetailVisible
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
}
