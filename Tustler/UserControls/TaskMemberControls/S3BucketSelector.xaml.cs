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
using TustlerFSharpPlatform;
using TustlerModels;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// S3BucketSelector expects to be bound to a TaskResponse.BucketsModel
    /// Interaction logic for S3BucketSelector.xaml
    /// </summary>
    public partial class S3BucketSelector : UserControl, ICommandSource
    {
        public S3BucketSelector()
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
                typeof(S3BucketSelector),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            S3BucketSelector ctrl = (S3BucketSelector)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        /// <summary>
        /// Set to the selected bucket
        /// </summary>
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
            var context = (e.OriginalSource as Button).DataContext as Bucket;

            var parameterData = new UITaskArgument?[] {
                UITaskArgument.NewBucket(context)
            };

            CommandParameter = new UITaskArguments()
            {
                Mode = UITaskMode.Select,
                TaskArguments = parameterData
            };

            ExecuteCommand();
        }
    }

    public static class S3BucketSelectorCommands
    {
        public static readonly RoutedUICommand Select = new RoutedUICommand
            (
                "Select",
                "Select",
                typeof(S3BucketSelectorCommands),
                null
            );
    }
}
