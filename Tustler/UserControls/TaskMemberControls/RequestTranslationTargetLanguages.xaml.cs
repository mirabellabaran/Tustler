using CloudWeaver;
using CloudWeaver.AWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerModels;
using TustlerUIShared;
using Converters = CloudWeaver.Converters;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestTranslationTargetLanguages.xaml
    /// </summary>
    public partial class RequestTranslationTargetLanguages : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestTranslationTargetLanguages), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestTranslationTargetLanguages ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var newState = (bool)dependencyPropertyChangedEventArgs.NewValue;
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

        public RequestTranslationTargetLanguages()
        {
            InitializeComponent();
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
            if (newCommand != null)
            {
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
                typeof(RequestTranslationTargetLanguages),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestTranslationTargetLanguages ctrl = (RequestTranslationTargetLanguages)d;
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
            e.CanExecute = lbTargetLanguages.SelectedItems.Count > 0;
        }

        private async void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var targetLanguageCodes = (lbTargetLanguages.SelectedItems as IEnumerable<object>)
                .Cast<LanguageCode>()
                .Select(lc => new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(lc)));

            var typeResolver = await TypeResolver.Create().ConfigureAwait(false);
            var jsonSerializerOptions = Converters.CreateSerializerOptions(typeResolver);
            var data = SerializableTypeGenerator.CreateTranslationTargetLanguageCodes("AWSShareIterationArgument", targetLanguageCodes, jsonSerializerOptions);

            var mode = UITaskMode.NewSetArgument(new AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages));
            CommandParameter = new UITaskArguments(mode, "AWSShareIntraModule", "SetTranslationTargetLanguages", data);

            ExecuteCommand();
        }

        private void TargetLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedLanguageCodes = lbTargetLanguages.FindResource("selectedLanguageCodes") as SelectedItemsViewModel;
            selectedLanguageCodes.Update(lbTargetLanguages.SelectedItems as IEnumerable<object>);

            if (e.AddedItems.Count > 0)
            {
                var firstItem = (e.AddedItems as IEnumerable<object>).First() as LanguageCode;
                lbTargetLanguages.ScrollIntoView(firstItem);
            }
        }
    }

    public static class TranslationTargetLanguagesCommands
    {
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(TranslationTargetLanguagesCommands),
                null
            );
    }
}
