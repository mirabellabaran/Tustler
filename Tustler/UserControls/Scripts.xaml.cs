using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TustlerServicesLib;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Scripts.xaml
    /// </summary>
    public partial class Scripts : UserControl
    {
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(Scripts), new PropertyMetadata("", PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var ctrl = dependencyObject as Scripts;
            if (ctrl != null)
            {
                //if (dependencyPropertyChangedEventArgs.NewValue != null)
                //    ctrl.ReportViewerLoad(dependencyPropertyChangedEventArgs.NewValue.ToString());
            }
        }

        public string ScriptName
        {
            get { return (string) GetValue(ScriptNameProperty); }
            set { SetValue(ScriptNameProperty, value); }
        }

        public Scripts()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.ShowMessage("Parameter set", $"Script name set to {ScriptName}");
        }
    }
}
