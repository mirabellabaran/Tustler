using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Models;
using TustlerWinPlatformLib;

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

            e.CanExecute = (applicationSettingsInstance.HasChanged);
        }

        private void SaveSettings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var applicationSettingsInstance = this.FindResource("applicationSettingsInstance") as ApplicationSettingsViewModel;

            var keyValuePairs = new Dictionary<string, string>(applicationSettingsInstance.Settings.Select(setting => new KeyValuePair<string, string>(setting.Key, setting.Value)));
            JsonConfiguration.SaveConfiguration(keyValuePairs);

            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            var filePath = keyValuePairs[JsonConfiguration.FilePathKey];
            notifications.ShowMessage("Configuration saved", $"Application settings were saved to {filePath}");
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
