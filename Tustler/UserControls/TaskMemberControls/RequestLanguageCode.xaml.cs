using CloudWeaver.AWS;
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
using TustlerUIShared;
using TustlerModels;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestLanguageCode.xaml
    /// Used for BOTH Transcription and Translation (use LanguageCodesViewModelType property)
    /// </summary>
    public partial class RequestLanguageCode : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestLanguageCode), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestLanguageCode ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var newState = (bool)dependencyPropertyChangedEventArgs.NewValue;
                    ctrl.cbLanguage.IsEnabled = newState;
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

        #region LanguageCodesViewModelType

        public static readonly DependencyProperty LanguageCodesViewModelTypeProperty =
            DependencyProperty.Register(
                "LanguageCodesViewModelType",
                typeof(LanguageCodesViewModelType),
                typeof(RequestLanguageCode));

        public LanguageCodesViewModelType LanguageCodesViewModelType
        {
            get
            {
                return (LanguageCodesViewModelType)GetValue(LanguageCodesViewModelTypeProperty);
            }
            set
            {
                SetValue(LanguageCodesViewModelTypeProperty, value);
            }
        }

        #endregion

        public RequestLanguageCode()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot control use this as the context
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // bind the view model
            LanguageCodesViewModel model = LanguageCodesViewModelType switch
            {
                LanguageCodesViewModelType.Transcription => new TranscriptionLanguageCodesViewModel(),
                LanguageCodesViewModelType.Translation => new TranslationLanguageCodesViewModel(),
                _ => throw new NotImplementedException(),
            };

            Binding myBinding = new Binding("LanguageCodes")
            {
                Source = model
            };

            cbLanguage.SetBinding(ComboBox.ItemsSourceProperty, myBinding);

            // set the default item
            cbLanguage.SelectedValue = LanguageCodesViewModelType switch
            {
                LanguageCodesViewModelType.Transcription => "en-US",
                LanguageCodesViewModelType.Translation => "en",
                _ => throw new ArgumentException("Language Code Task Member received an unknown language viewmodel type"),
            };
        }

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
                typeof(RequestLanguageCode),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestLanguageCode ctrl = (RequestLanguageCode) d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

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

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = cbLanguage.SelectedItem is TustlerModels.LanguageCode _;
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (cbLanguage.SelectedItem is TustlerModels.LanguageCode languageCode)
            {
                LanguageDomain domain = LanguageCodesViewModelType switch
                {
                    LanguageCodesViewModelType.Transcription => LanguageDomain.Transcription,
                    LanguageCodesViewModelType.Translation => LanguageDomain.Translation,
                    _ => null
                };

                var data = CloudWeaver.SerializableTypeGenerator.CreateLanguageCodeDomain(domain, languageCode.Name, languageCode.Code);


                CommandParameter = new UITaskArguments(UITaskMode.SetArgument, "AWSShareIntraModule", "SetLanguage", data);

                ExecuteCommand();
            }
        }
    }

    public static class LanguageCodeCommands
    {
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(LanguageCodeCommands),
                null
            );
    }
}
