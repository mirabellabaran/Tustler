using CloudWeaver.Types;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Tustler.Helpers;
using TustlerUIShared;
using AppSettings = TustlerServicesLib.ApplicationSettings;
using Path = System.IO.Path;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestFilePath.xaml
    /// </summary>
    public partial class RequestFilePath : UserControl
    {
        public enum FilePickerMode
        {
            Open,
            Save
        }

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

        #region FilePickerMode DependencyProperty
        public static readonly DependencyProperty FilePickerModeProperty =
            DependencyProperty.Register(
                "FilePickerMode",
                typeof(FilePickerMode),
                typeof(RequestFilePath));

        /// <summary>
        /// The mode of file picker: Open or Save
        /// </summary>
        public FilePickerMode PickerMode
        {
            get
            {
                return (FilePickerMode) GetValue(FilePickerModeProperty);
            }
            set
            {
                SetValue(FilePickerModeProperty, value);
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

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot control use this as the context
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
            static (Nullable<bool> completed, string fileName) GetOpenPickerResult(string? filter, string fileTypeDescription)
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = $"Select a source file {fileTypeDescription}",
                    Multiselect = false,
                    InitialDirectory = AppSettings.FileCachePath,
                    CheckPathExists = true,
                    Filter = filter
                };

                return (dlg.ShowDialog(), dlg.FileName);
            }

            static (Nullable<bool> completed, string fileName) GetSavePickerResult(string? filter, string fileTypeDescription)
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Title = $"Choose a destination {fileTypeDescription}",
                    InitialDirectory = AppSettings.FileCachePath,
                    AddExtension = true,
                    Filter = filter
                };

                return (dlg.ShowDialog(), dlg.FileName);
            }

            var filter = FileExtensionDescription is object && FileExtension is object ?
                $"{FileExtensionDescription} (*.{FileExtension}) | *.{FileExtension}" : null;   // e.g. All files(*.*) | *.*

            var fileTypeDescriptionArray = FileExtensionDescription is object? FileExtensionDescription.Split(" ").SkipLast(1).ToArray() : Array.Empty<string>();
            var fileTypeDescription = fileTypeDescriptionArray.Length > 0 ? $"({string.Join(" ", fileTypeDescriptionArray)})" : "";

            // PickerMode defaults to Open; ensure this is set in XAML
            var (completed, fileName) = PickerMode switch
            {
                FilePickerMode.Open => GetOpenPickerResult(filter, fileTypeDescription),
                FilePickerMode.Save => GetSavePickerResult(filter, fileTypeDescription),
                _ => throw new NotImplementedException(),
            };

            if (completed.HasValue && completed == true)
            {
                tbFilePath.Text = fileName;
            }
        }

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            static bool CheckOpenCriteria(string filePath, string fileExtension)
            {
                if (fileExtension == "*")
                    return File.Exists(filePath);
                else
                    return Path.GetExtension(filePath) == $".{fileExtension}" && File.Exists(filePath);
            }

            static bool CheckSaveCriteria(string filePath)
            {
                var directoryPath = System.IO.Path.GetDirectoryName(filePath);
                var fileName = System.IO.Path.GetFileName(filePath);
                var result = false;

                if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileName) && Directory.Exists(directoryPath))
                {
                    result = true;
                }

                return result;
            }

            e.CanExecute = PickerMode switch
            {
                FilePickerMode.Open => CheckOpenCriteria(tbFilePath.Text, FileExtension),
                FilePickerMode.Save => CheckSaveCriteria(tbFilePath.Text),
                _ => throw new NotImplementedException(),
            };
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.DataContext is ResponseWrapper wrapper)
            {
                var filePath = Path.HasExtension(tbFilePath.Text) ? tbFilePath.Text : Path.ChangeExtension(tbFilePath.Text, FileExtension);
                var fileInfo = new FileInfo(filePath);
                var pickerMode = PickerMode switch
                {
                    FilePickerMode.Open => CloudWeaver.Types.FilePickerMode.Open,
                    FilePickerMode.Save => CloudWeaver.Types.FilePickerMode.Save,
                    _ => throw new NotImplementedException(),
                };


                var extension = fileInfo.Extension.AsSpan()[1..].ToString();
                var data = CloudWeaver.SerializableTypeGenerator.CreateFilePath(fileInfo, extension, pickerMode);

                // this user control is responding to a RequestArgument
                if (wrapper.TaskResponse is TaskResponse.RequestArgument arg)
                {
                    var mode = UITaskMode.NewSetArgument(arg.Item);
                    CommandParameter = new UITaskArguments(mode, "StandardShareIntraModule", "SetFilePath", data);

                    ExecuteCommand();
                }
            }
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
