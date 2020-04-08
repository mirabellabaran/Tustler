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
using TustlerModels;
using static TustlerFSharpPlatform.TaskArguments;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for TranscriptionLanguageCode.xaml
    /// </summary>
    public partial class LanguageCode : UserControl, ICommandSource
    {
        public LanguageCode()
        {
            InitializeComponent();
        }

        #region LanguageCodesViewModel

        public static readonly DependencyProperty LanguageCodesViewModelProperty =
            DependencyProperty.Register(
                "LanguageCodesViewModel",
                typeof(LanguageCodesViewModel),
                typeof(LanguageCode),
                new PropertyMetadata((LanguageCodesViewModel)null,
                new PropertyChangedCallback(LanguageCodesViewModelChanged)));

        public LanguageCodesViewModel LanguageCodesViewModel
        {
            get
            {
                return (LanguageCodesViewModel)GetValue(LanguageCodesViewModelProperty);
            }
            set
            {
                SetValue(LanguageCodesViewModelProperty, value);
            }
        }

        private static void LanguageCodesViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LanguageCode ctrl = (LanguageCode) d;
            var model = e.NewValue as LanguageCodesViewModel;

            Binding myBinding = new Binding("LanguageCodes")
            {
                Source = model
            };

            ctrl.cbLanguage.SetBinding(ComboBox.ItemsSourceProperty, myBinding);

            // set the default item
            ctrl.cbLanguage.SelectedValue = model switch
            {
                TranscriptionLanguageCodesViewModel _ => "en-US",
                TranslationLanguageCodesViewModel _ => "en",
                _ => throw new ArgumentException("Language Code Task Member received an unknown language viewmodel"),
            };
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
                typeof(LanguageCode),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LanguageCode ctrl = (LanguageCode) d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get
            {
                if (cbLanguage.SelectedItem is TustlerModels.LanguageCode languageCode) {
                    var member = LanguageCodesViewModel switch
                    {
                        TranscriptionLanguageCodesViewModel _ => TaskArgumentMember.NewTranscriptionLanguageCode(languageCode.Code),
                        TranslationLanguageCodesViewModel _ => TaskArgumentMember.NewTranslationLanguageCode(languageCode.Code),
                        _ => null
                    };

                    return member;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
    }
}
