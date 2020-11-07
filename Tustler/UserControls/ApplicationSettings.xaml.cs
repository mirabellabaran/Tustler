using CloudWeaver.Foundation.Types;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tustler.Models;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for ApplicationSettings.xaml
    /// </summary>
    public partial class ApplicationSettings : UserControl
    {
        public ApplicationSettings()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var applicationSettingsInstance = this.FindResource("applicationSettingsInstance") as ApplicationSettingsViewModel;

            applicationSettingsInstance.Refresh();
        }

        private void SaveSettings_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var applicationSettingsInstance = this.FindResource("applicationSettingsInstance") as ApplicationSettingsViewModel;

            e.CanExecute = (applicationSettingsInstance.HasChanged && IsValid(dgApplicationSettings as DependencyObject));
        }

        private void SaveSettings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;

            var applicationSettingsInstance = this.FindResource("applicationSettingsInstance") as ApplicationSettingsViewModel;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                applicationSettingsInstance.Save();
            }
            catch (IOException ex)
            {
                notifications.HandleError("SaveSettings_Executed", "An error occurred during a save operation.", ex);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            applicationSettingsInstance.HasChanged = false;

            var filePath = TustlerServicesLib.ApplicationSettings.AppSettingsFilePath;
            notifications.ShowMessage("Configuration saved", $"Application settings were saved to {filePath}");
        }

        /// <summary>
        /// Check validation state for the datagrid and child elements
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        /// <remarks>see https://stackoverflow.com/questions/127477/detecting-wpf-validation-errors </remarks>
        private bool IsValid(DependencyObject parent)
        {
            if (Validation.GetHasError(parent))
                return false;

            // Validate all the bindings on the children
            for (int i = 0; i != VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (!IsValid(child)) { return false; }
            }

            return true;
        }
    }

    public static class ApplicationSettingsCommands
    {
        public static readonly RoutedUICommand SaveSettings = new RoutedUICommand
            (
                "SaveSettings",
                "SaveSettings",
                typeof(ApplicationSettingsCommands),
                null
            );
    }
}
