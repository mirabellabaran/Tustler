using CloudWeaver.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Helpers;
using TustlerServicesLib;
using TustlerUIShared;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for SelectDefaultArguments.xaml
    /// </summary>
    public partial class SelectDefaultArguments : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(SelectDefaultArguments), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is SelectDefaultArguments ctrl)
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

        public SelectDefaultArguments()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            lbDescriptions.SelectAll();
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
                typeof(SelectDefaultArguments),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectDefaultArguments ctrl = (SelectDefaultArguments)d;
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
            // get an in-order list of the list box item containers
            // some of these may be null if the container has not yet been generated
            var items = Enumerable.Range(0, lbDescriptions.Items.Count)
                .Select(index => lbDescriptions.ItemContainerGenerator.ContainerFromIndex(index))
                .Cast<ListBoxItem>()
                .ToArray();

            // the bound value
            var wrapper = this.DataContext as DescriptionWrapper;

            // set a boolean flag for each item in the list of descriptions (selected or not selected)
            // note the need for a check that each ListBoxItem is generated (may be null)
            var flags = wrapper.Descriptions.Select((desc, index) => items[index] is object? items[index].IsSelected : false);
            var data = JsonSerializer.SerializeToUtf8Bytes(flags);

            CommandParameter = new UITaskArguments(UITaskMode.SelectDefaultArguments, "", "", data);

            ExecuteCommand();
        }
    }

    public static class SelectDefaultArgumentsCommands
    {
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(SelectDefaultArgumentsCommands),
                null
            );
    }
}
