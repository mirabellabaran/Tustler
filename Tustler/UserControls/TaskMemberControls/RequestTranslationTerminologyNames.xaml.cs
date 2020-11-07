using CloudWeaver;
using CloudWeaver.Foundation.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TustlerAWSLib;
using TustlerModels;
using TustlerUIShared;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestTranslationTerminologyNames.xaml
    /// </summary>
    public partial class RequestTranslationTerminologyNames : UserControl
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestTranslationTerminologyNames), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestTranslationTerminologyNames ctrl)
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

        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public RequestTranslationTerminologyNames()
        {
            InitializeComponent();

            var serviceProvider = (Application.Current as App).ServiceProvider;

            this.awsInterface = serviceProvider.GetRequiredService<AmazonWebServiceInterface>();
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var terminologiesInstance = this.FindResource("terminologiesInstance") as TranslationTerminologiesViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await terminologiesInstance.Refresh(awsInterface, notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
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
                typeof(RequestTranslationTerminologyNames),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestTranslationTerminologyNames ctrl = (RequestTranslationTerminologyNames)d;
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
            e.CanExecute = true;
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var terminologyNames = (lbTerminologyNames.SelectedItems as IEnumerable<object>)
                .Cast<Terminology>()
                .Select(term => term.Name);
            var data = SerializableTypeGenerator.CreateTranslationTerminologyNames(terminologyNames);

            CommandParameter = new UITaskArguments(UITaskMode.SetArgument, "AWSShareIntraModule", "SetTranslationTerminologyNames", data);

            ExecuteCommand();
        }

        private void TerminologyNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTerminologies = lbTerminologyNames.FindResource("selectedTerminologies") as SelectedItemsViewModel;
            selectedTerminologies.Update(lbTerminologyNames.SelectedItems as IEnumerable<object>);

            if (e.AddedItems.Count > 0)
            {
                var firstItem = (e.AddedItems as IEnumerable<object>).First() as Terminology;
                lbTerminologyNames.ScrollIntoView(firstItem);
            }
        }
    }

    public static class TranslationTerminologyNamesCommands
    {
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(TranslationTerminologyNamesCommands),
                null
            );
    }
}
