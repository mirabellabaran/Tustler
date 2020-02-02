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

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Credentials.xaml
    /// </summary>
    public partial class Credentials : UserControl
    {
        public Credentials()
        {
            InitializeComponent();

            string path = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\poo.poo";
            Helpers.FileServices.GetMimeType(path);
        }

        private void Credentials_Button_Click(object sender, RoutedEventArgs e)
        {
            var accessKey = TustlerAWSLib.Utilities.CheckCredentials();
            // TODO redirect to a form asking for accessKey and secretKey
            // and then store in shared credentials file

            var message = (accessKey != null) ? accessKey : "None";
            var region = TustlerAWSLib.Utilities.GetRegion();
            message = (region != null) ? string.Format("{0} ({1})", message, region) : message;
            MessageBox.Show(message, "Access Key");
        }
    }
}
