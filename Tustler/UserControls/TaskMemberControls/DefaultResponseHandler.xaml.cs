using CloudWeaver;
using CloudWeaver.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using TustlerUIShared;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for DefaultResponseHandler.xaml
    /// </summary>
    public partial class DefaultResponseHandler : UserControl
    {
        #region Message DependencyProperty
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(DefaultResponseHandler));

        /// <summary>
        ///  A message describing the unhandled response
        /// </summary>
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        #endregion

        #region SuggestedTaskFunctions DependencyProperty
        public static readonly DependencyProperty SuggestedTaskFunctionsProperty =
            DependencyProperty.Register("SuggestedTaskFunctions", typeof(IEnumerable<string>), typeof(DefaultResponseHandler));

        /// <summary>
        ///  A list of TaskFunctions that are able to fulfill the argument request
        /// </summary>
        public IEnumerable<string> SuggestedTaskFunctions
        {
            get { return (IEnumerable<string>)GetValue(SuggestedTaskFunctionsProperty); }
            set { SetValue(SuggestedTaskFunctionsProperty, value); }
        }
        #endregion

        private readonly TaskFunctionResolver taskFunctionResolver;

        public DefaultResponseHandler()
        {
            InitializeComponent();

            var serviceProvider = (Application.Current as App).ServiceProvider;
            this.taskFunctionResolver = serviceProvider.GetRequiredService<TaskFunctionResolver>();

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot control use this as the context to access Available and Selected properties
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var wrapper = this.DataContext as ResponseWrapper;
            var response = wrapper.TaskResponse;
            var description = response.IsRequestArgument ? "request" : "response";

            tbInfo.Text = $"Unknown {description}: {response}";

            // suggest task functions that generate the requested output
            this.SuggestedTaskFunctions = response switch
            {
                TaskResponse.RequestArgument request => taskFunctionResolver.FindTaskFunctionsWithOutput(request.Item).ToArray(),
                _ => Array.Empty<string>()
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

        private void Select_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Select_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedTaskPath = (e.OriginalSource as Button).Content as string;

            var data = JsonSerializer.SerializeToUtf8Bytes(selectedTaskPath);

            CommandParameter = new UITaskArguments(UITaskMode.InsertTask, "", "", data);

            ExecuteCommand();
        }
    }

    public static class DefaultResponseHandlerCommands
    {
        public static readonly RoutedUICommand Select = new RoutedUICommand
            (
                "Select",
                "Select",
                typeof(DefaultResponseHandlerCommands),
                null
            );

    }
}