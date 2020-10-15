﻿using CloudWeaver;
using CloudWeaver.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
using TustlerServicesLib;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for DefaultResponseHandler.xaml
    /// </summary>
    public partial class DefaultResponseHandler : UserControl
    {
        public DefaultResponseHandler()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot control use this as the context to access Available and Selected properties
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var wrapper = this.DataContext as ResponseWrapper;
            var wrappedItem = wrapper.TaskResponse;
            var description = wrappedItem.IsRequestArgument ? "request" : "response";

            tbInfo.Text = $"Unknown {description}: {wrappedItem}";

            // create an agent and retrieve the outputs for all task functions
            var serviceProvider = (Application.Current as App).ServiceProvider;
            var taskLogger = serviceProvider.GetRequiredService<TaskLogger>();
            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();

            var agent = new Agent(knownArguments, null, taskLogger, false);

            var taskFunctions = wrapper.Owner.TaskFunctions;
            //var outputs = taskFunctions.Select(functionSpec =>
            //{
            //    var taskName = functionSpec.TaskName;

            //})
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
                typeof(DefaultResponseHandler),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefaultResponseHandler ctrl = (DefaultResponseHandler)d;
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
    }
}
