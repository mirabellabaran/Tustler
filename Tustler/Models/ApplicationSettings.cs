using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using TustlerWinPlatformLib;

namespace Tustler.Models
{
    public class ApplicationSettingsViewModel
    {
        public ObservableCollection<Setting> Settings
        {
            get;
            private set;
        }

        public ApplicationSettingsViewModel()
        {
            Settings = new ObservableCollection<Setting>();
            Settings.CollectionChanged += Settings_CollectionChanged;
            HasChanged = false;
        }

        private void Settings_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasChanged = true;
        }

        public bool HasChanged
        {
            get;
            internal set;
        }

        public void Refresh()
        {
            Settings.Clear();
            foreach (var kvp in JsonConfiguration.KeyValuePairs)
            {
                Settings.Add(new Setting { Key = kvp.Key, Value = kvp.Value });
            }
        }
    }

    public class Setting// : IEditableObject, INotifyPropertyChanged
    {
        public string Key { get; internal set; }
        public string Value { get; set; }
    }

    public class SettingsValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value,
            System.Globalization.CultureInfo cultureInfo)
        {
            Setting setting = (value as BindingGroup).Items[0] as Setting;
            if (setting.Key == "FileCache" && !Directory.Exists(setting.Value))
            {
                return new ValidationResult(false, "The value for FileCache must be a valid folder path.");
            }
            else
            {
                return ValidationResult.ValidResult;
            }
        }
    }
}
