﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using TustlerFSharpPlatform;
using AppSettings = TustlerServicesLib.ApplicationSettings;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestFilePath.xaml
    /// </summary>
    public partial class RequestFilePath : UserControl
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestFilePath), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestFilePath ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var newState = (bool)dependencyPropertyChangedEventArgs.NewValue;
                    ctrl.tbFilePath.IsEnabled = newState;
                    ctrl.btnFilePicker.IsEnabled = newState;
                    ctrl.btnContinue.IsEnabled = newState;
                }
            }
        }

        /// <summary>
        ///  Enables or disables the Continue button
        /// </summary>
        public bool IsButtonEnabled
        {
            get { return (bool)GetValue(IsButtonEnabledProperty); }
            set { SetValue(IsButtonEnabledProperty, value); }
        }
        #endregion

        #region FileExtensionDescription DependencyProperty
        public static readonly DependencyProperty FileExtensionDescriptionProperty =
            DependencyProperty.Register(
                "FileExtensionDescription",
                typeof(string),
                typeof(RequestFilePath));

        /// <summary>
        /// A description of the required filter e.g. 'Image files' or 'All files'
        /// </summary>
        public string FileExtensionDescription
        {
            get
            {
                return (string)GetValue(FileExtensionDescriptionProperty);
            }
            set
            {
                SetValue(FileExtensionDescriptionProperty, value);
            }
        }
        #endregion

        #region FileExtension DependencyProperty
        public static readonly DependencyProperty FileExtensionProperty =
            DependencyProperty.Register(
                "FileExtension",
                typeof(string),
                typeof(RequestFilePath));

        /// <summary>
        /// The file extension of the required filter e.g. 'json', 'jpg' or 'doc'
        /// </summary>
        public string FileExtension
        {
            get
            {
                return (string)GetValue(FileExtensionProperty);
            }
            set
            {
                SetValue(FileExtensionProperty, value);
            }
        }
        #endregion

        #region ICommandSource

        #region ICommandSource Common Elements

        public IInputElement CommandTarget => this;

        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
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
                typeof(RequestFilePath),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestFilePath ctrl = (RequestFilePath)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get;
            internal set;
        }

        #endregion

#nullable enable

        public RequestFilePath()
        {
            InitializeComponent();
        }

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

        private void OpenFilePicker_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenFilePicker_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var filter = FileExtensionDescription is object && FileExtension is object?
                $"{FileExtensionDescription} (*.{FileExtension}) | *.{FileExtension}" : null;   // e.g. All files(*.*) | *.*

            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Choose a file",
                Multiselect = false,
                InitialDirectory = AppSettings.FileCachePath,
                CheckPathExists = true,
                Filter = filter
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbFilePath.Text = dlg.FileName;
            }
        }

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = File.Exists(tbFilePath.Text);
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var fileInfo = new FileInfo(tbFilePath.Text);

            CommandParameter = new UITaskArguments()
            {
                Mode = UITaskMode.Select,
                TaskArguments = new UITaskArgument[] { UITaskArgument.NewFilePath(fileInfo, FileExtension) }
            };

            ExecuteCommand();
        }
    }

    public static class FilePathCommands
    {
        public static readonly RoutedUICommand OpenFilePicker = new RoutedUICommand
            (
                "OpenFilePicker",
                "OpenFilePicker",
                typeof(FilePathCommands),
                null
            );
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(FilePathCommands),
                null
            );
    }
}
