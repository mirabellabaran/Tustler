using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace TustlerInterfaces
{
    public interface INotifiableViewModel<T>
    {
        public ObservableCollection<T> NotificationsList
        {
            get;
        }
    }
}
