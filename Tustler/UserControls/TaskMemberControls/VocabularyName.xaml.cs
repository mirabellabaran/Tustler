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
using TustlerAWSLib;
using TustlerModels;
using TustlerServicesLib;
using static TustlerFSharpPlatform.TaskArguments;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for VocabularyName.xaml
    /// </summary>
    public partial class VocabularyName : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;

        public VocabularyName(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;

            var vocabulariesInstance = this.FindResource("vocabulariesInstance") as TranscriptionVocabulariesViewModel;
            await vocabulariesInstance.Refresh(awsInterface, notifications).ConfigureAwait(true);
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
                typeof(VocabularyName),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VocabularyName ctrl = (VocabularyName)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get
            {
                return (cbVocabularyName.SelectedItem is Vocabulary vocabulary) ? TaskArgumentMember.NewVocabularyName(vocabulary.VocabularyName) : null;
            }
        }

        #endregion

        private void cbVocabularyName_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
