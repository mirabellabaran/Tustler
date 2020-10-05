using CloudWeaver;
using CloudWeaver.AWS;
using CloudWeaver.Types;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerFSharpPlatform;
using AppSettings = TustlerServicesLib.ApplicationSettings;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestFileMediaReference.xaml
    /// </summary>
    public partial class RequestFileMediaReference : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestFileMediaReference), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestFileMediaReference ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var newState = (bool)dependencyPropertyChangedEventArgs.NewValue;
                    ctrl.tbFilePath.IsEnabled = newState;
                    ctrl.btnFilePicker.IsEnabled = newState;
                    ctrl.lblMimeTypeLabel.IsEnabled = newState;
                    ctrl.lblExtensionLabel.IsEnabled = newState;
                    ctrl.lblFileExistsLabel.IsEnabled = newState;
                    ctrl.lblMimeType.IsEnabled = newState;
                    ctrl.lblExtension.IsEnabled = newState;
                    ctrl.lblFileExists.IsEnabled = newState;
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
                typeof(RequestFileMediaReference),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestFileMediaReference ctrl = (RequestFileMediaReference) d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get;
            internal set;
        }

        #endregion

#nullable enable
        #region Locally Bound (MimeType and Extension)
        public string? Mimetype { get; internal set; }

        public string? Extension { get; internal set; }

        public bool IsFileTyped
        {
            get
            {
                return (!string.IsNullOrEmpty(Mimetype) && !string.IsNullOrEmpty(Extension));
            }
        }
        #endregion

        public RequestFileMediaReference()
        {
            InitializeComponent();
        }

        private void UpdateMimetype(string path)
        {
            if (string.IsNullOrEmpty(path) || (path.Length < 3))
            {
                lblFileExists.Content = false;

                this.Mimetype = null;
                lblMimeType.Content = null;
                this.Extension = null;
                lblExtension.Content = null;
            }
            else {
                var exists = File.Exists(path);
                lblFileExists.Content = exists;

                string? mimetype = exists ? Helpers.FileServices.GetMimeType(path) : null;
                string? extension = Path.GetExtension(path);

                if (string.IsNullOrEmpty(extension))
                {
                    extension = TustlerServicesLib.MimeTypeDictionary.GetExtensionFromMimeType(mimetype);
                }
                else
                {
                    extension = extension.Substring(1).ToLowerInvariant();
                }

                this.Mimetype = mimetype;
                lblMimeType.Content = mimetype;
                this.Extension = extension;
                lblExtension.Content = extension ?? "Needs an extension";
            }
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
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Choose a file to open",
                Multiselect = false,
                InitialDirectory = AppSettings.FileCachePath
            };

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                tbFilePath.Text = dlg.FileName;
            }
        }

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            UpdateMimetype(tbFilePath.Text);

            e.CanExecute = File.Exists(tbFilePath.Text) && IsFileTyped;
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var data = SerializableTypeGenerator.CreateFileMediaReference(tbFilePath.Text, this.Mimetype, this.Extension);

            CommandParameter = new UITaskArguments(UITaskMode.SetArgument, "StandardShareIntraModule", "SetTaskItem", data);

            ExecuteCommand();
        }
    }

    public static class FileMediaReferenceCommands
    {
        public static readonly RoutedUICommand OpenFilePicker = new RoutedUICommand
            (
                "OpenFilePicker",
                "OpenFilePicker",
                typeof(FileMediaReferenceCommands),
                null
            );
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(FileMediaReferenceCommands),
                null
            );
    }
}
