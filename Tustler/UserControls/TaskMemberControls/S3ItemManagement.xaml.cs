using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerFSharpPlatform;
using static TustlerFSharpPlatform.MiniTasks;
using AppSettings = TustlerServicesLib.ApplicationSettings;

namespace Tustler.UserControls.TaskMemberControls
{
    public enum S3ItemManagementMode
    {
        Default,
        Download,
        Delete
    }

    /// <summary>
    /// Interaction logic for S3ItemManagement.xaml
    /// </summary>
    public partial class S3ItemManagement : UserControl, ICommandSource, INotifyPropertyChanged
    {
        private S3ItemManagementMode mode;

        public S3ItemManagement()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot use this as the context
        }

        #region Mode property (binding source)
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public S3ItemManagementMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                mode = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Context DependencyProperty
        public static readonly DependencyProperty ContextProperty = DependencyProperty.Register("Context", typeof(object), typeof(S3ItemManagement));

        // bound to the DataContext of the parent
        public object Context
        {
            get { return (object)GetValue(ContextProperty); }
            set { SetValue(ContextProperty, value); }
        }
        #endregion

        #region SelectedItem DependencyProperty
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(object), typeof(S3ItemManagement));

        // bound to the SelectedItem of an items control
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        #endregion

        #region ICommandSource

        #region ICommandSource Common Elements

        public IInputElement CommandTarget => this;

        public ICommand Command
        {
            get
            {
                return (ICommand) GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
        {
            if (oldCommand != null)
            {
                RemoveCommand(oldCommand);
            }

            AddCommand(newCommand);
        }

        // Remove an old command from the Command Property
        private void RemoveCommand(ICommand oldCommand)
        {
            EventHandler handler = CanExecuteChanged;
            oldCommand.CanExecuteChanged -= handler;
        }

        // Add the command
        private void AddCommand(ICommand newCommand)
        {
            EventHandler handler = new EventHandler(CanExecuteChanged);
            //canExecuteChangedHandler = handler;
            if (newCommand != null)
            {
                //newCommand.CanExecuteChanged += canExecuteChangedHandler;
                newCommand.CanExecuteChanged += handler;
            }
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            if (this.Command != null)
            {
                if (this.Command is RoutedCommand command)
                {
                    if (command.CanExecute(CommandParameter, CommandTarget))
                    {
                        this.IsEnabled = true;
                    }
                    else
                    {
                        this.IsEnabled = false;
                    }
                }
                else
                {
                    if (Command.CanExecute(CommandParameter))
                    {
                        this.IsEnabled = true;
                    }
                    else
                    {
                        this.IsEnabled = false;
                    }
                }
            }
        }

        #endregion

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(S3ItemManagement),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            S3ItemManagement ctrl = (S3ItemManagement) d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        /// <summary>
        /// Set to the accumulated data for the control i.e. selected bucket name and item key plus the file path
        /// </summary>
        public object CommandParameter
        {
            get;
            internal set;
        }

        #endregion

        private void ExecuteCommand()
        {
            if (this.Command != null)
            {
                if (Command is RoutedCommand command)
                {
                    command.Execute(CommandParameter, CommandTarget);
                }
                else
                {
                    ((ICommand)Command).Execute(CommandParameter);
                }
            }
        }

#nullable enable
        private void PrepareData(S3ItemManagementMode mode, string? filePath)
        {
            static object? GetValueFromModel(object? model, Type? itemType, string itemName)
            {
                var modelType = model?.GetType();

                if (modelType is object && itemType is object)
                {
                    var propertyInfo = modelType.GetProperty(itemName, itemType);
                    return propertyInfo?.GetValue(model);
                }
                else
                {
                    return null;
                }
            }

            var parameterData = new MiniTaskArgument?[] {
                MiniTaskArgument.NewString((string?) GetValueFromModel(SelectedItem, typeof(string), "BucketName")),
                MiniTaskArgument.NewString((string?) GetValueFromModel(SelectedItem, typeof(string), "Key")),
                MiniTaskArgument.NewString((string?) filePath)
            };

            CommandParameter = new MiniTaskArguments()
            {
                Mode = mode switch
                {
                    S3ItemManagementMode.Delete => MiniTaskMode.Delete,
                    S3ItemManagementMode.Download => MiniTaskMode.Download,
                    _ => MiniTaskMode.Unknown
                },
                TaskArguments = parameterData
            };
        }
#nullable disable

        private void Download_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (SelectedItem is object);
        }

        private void Download_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PrepareData(S3ItemManagementMode.Download, e.Parameter as string);
            ExecuteCommand();

            Mode = S3ItemManagementMode.Default;
        }

        private void CancelDownload_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CancelDownload_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Mode = S3ItemManagementMode.Default;
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (SelectedItem is object);
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PrepareData(S3ItemManagementMode.Delete, null);
            ExecuteCommand();

            Mode = S3ItemManagementMode.Default;
        }

        private void CancelDelete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CancelDelete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Mode = S3ItemManagementMode.Default;
        }

        private void ChangeMode_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (SelectedItem is object);
        }

        private void ChangeMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (e.Parameter)
            {
                case "DownloadPrompt":
                    Mode = S3ItemManagementMode.Download;
                    break;
                case "ConfirmDelete":
                    Mode = S3ItemManagementMode.Delete;
                    break;
            }
        }

        private void OpenFilePicker_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenFilePicker_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Choose a file to open",
                Multiselect = false,
                InitialDirectory = AppSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var tbFilePath = (e.Parameter as TextBox);
                tbFilePath.Text = dlg.FileName;
            }
        }
    }

    public static class S3ItemManagementCommands
    {
        public static readonly RoutedUICommand Download = new RoutedUICommand
            (
                "Download",
                "Download",
                typeof(S3ItemManagementCommands),
                null
            );

        public static readonly RoutedUICommand CancelDownload = new RoutedUICommand
            (
                "CancelDownload",
                "CancelDownload",
                typeof(S3ItemManagementCommands),
                null
            );

        public static readonly RoutedUICommand Delete = new RoutedUICommand
            (
                "Delete",
                "Delete",
                typeof(S3ItemManagementCommands),
                null
            );

        public static readonly RoutedUICommand CancelDelete = new RoutedUICommand
            (
                "CancelDelete",
                "CancelDelete",
                typeof(S3ItemManagementCommands),
                null
            );

        public static readonly RoutedUICommand ChangeMode = new RoutedUICommand
            (
                "ChangeMode",
                "ChangeMode",
                typeof(S3ItemManagementCommands),
                null
            );

        public static readonly RoutedUICommand OpenFilePicker = new RoutedUICommand
            (
                "OpenFilePicker",
                "OpenFilePicker",
                typeof(S3ItemManagementCommands),
                null
            );
    }

}
