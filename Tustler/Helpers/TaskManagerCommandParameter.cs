using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TustlerFSharpPlatform;

namespace Tustler.Helpers
{
    public class TaskManagerCommandParameterValue : ItemsControl
    {
        public static readonly DependencyProperty ParameterTypeProperty = DependencyProperty.Register("ParameterType", typeof(string), typeof(TaskManagerCommandParameterValue));

        public TaskManagerCommandParameterValue()
        {
        }

        public string ParameterType
        {
            get { return (string)GetValue(ParameterTypeProperty); }
            set { SetValue(ParameterTypeProperty, value); }
        }
    }

    public class TaskManagerCommandParameterArgument : DependencyObject
    {
        public static readonly DependencyProperty ItemTypeProperty = DependencyProperty.Register("ItemType", typeof(string), typeof(TaskManagerCommandParameterArgument));
        public static readonly DependencyProperty ItemKeyProperty = DependencyProperty.Register("ItemKey", typeof(string), typeof(TaskManagerCommandParameterArgument));

        public TaskManagerCommandParameterArgument()
        {
        }

        public string ItemType
        {
            get { return (string)GetValue(ItemTypeProperty); }
            set { SetValue(ItemTypeProperty, value); }
        }

        public string ItemKey
        {
            get { return (string)GetValue(ItemKeyProperty); }
            set { SetValue(ItemKeyProperty, value); }
        }
    }
}
