using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using TustlerServicesLib;

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
            if (e.OldItems != null)
                foreach (Setting oldItem in e.OldItems)
                    oldItem.PropertyChanged -= Setting_PropertyChanged;

            if (e.NewItems != null)
                foreach (Setting newItem in e.NewItems)
                    newItem.PropertyChanged += Setting_PropertyChanged;
        }

        private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        public void Save()
        {
            var keyValuePairs = new Dictionary<string, string>(Settings.Select(setting => new KeyValuePair<string, string>(setting.Key, setting.Value)));
            JsonConfiguration.SaveConfiguration(keyValuePairs);
        }
    }

    public class Setting : INotifyPropertyChanged   // : IEditableObject
    {
        private string _key;
        private string _value;

        public string Key
        {
            get
            {
                return _key;
            }
            internal set
            {
                if (_key == value) return;
                _key = value;
                OnPropertyChanged(nameof(Key));
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class SettingsValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value,
            System.Globalization.CultureInfo cultureInfo)
        {
            Setting setting = (value as BindingGroup).Items[0] as Setting;

            var result = setting.Key switch
            {
                // FileCache is allowed to be empty (as it has a default value); if it is set it must refer to an existing folder
                "FileCache" => (!string.IsNullOrEmpty(setting.Value) && !Directory.Exists(setting.Value))?
                    new ValidationResult(false, "The value for FileCache must be a valid folder path.") : ValidationResult.ValidResult,
                // FFmpegDirectory must refer to an existing folder
                "FFmpegDirectory" => !Directory.Exists(setting.Value)?
                    new ValidationResult(false, "The value for FFmpegDirectory must be a valid folder path.") : ValidationResult.ValidResult,
                _ => ValidationResult.ValidResult
            };

            return result;
        }
    }
}
