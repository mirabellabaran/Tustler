using CloudWeaver.AWS;
using CloudWeaver.Types;
using Microsoft.Extensions.DependencyInjection;
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
using Tustler.Models;
using TustlerAWSLib;
using TustlerFSharpPlatform;
using TustlerModels;
using TustlerServicesLib;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestS3MediaReference.xaml
    /// </summary>
    public partial class RequestS3MediaReference : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestS3MediaReference), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestS3MediaReference ctrl)
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

        #region MediaType DependencyProperty
        public static readonly DependencyProperty MediaTypeProperty =
            DependencyProperty.Register(
                "MediaType",
                typeof(BucketItemMediaType),
                typeof(RequestS3MediaReference));

        public BucketItemMediaType MediaType
        {
            get
            {
                return (BucketItemMediaType)GetValue(MediaTypeProperty);
            }
            set
            {
                SetValue(MediaTypeProperty, value);
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
                typeof(RequestS3MediaReference),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestS3MediaReference ctrl = (RequestS3MediaReference)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get;
            internal set;
        }

        #endregion

        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public RequestS3MediaReference()
        {
            InitializeComponent();

            var serviceProvider = (Application.Current as App).ServiceProvider;

            this.awsInterface = serviceProvider.GetRequiredService<AmazonWebServiceInterface>();
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketViewModel.Refresh(awsInterface, false, notifications).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BucketsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            var audioBucketItemsInstance = this.FindResource("audioBucketItemsInstance") as MediaFilteredBucketItemViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketItemsInstance.Refresh(awsInterface, notifications, selectedBucket.Name).ConfigureAwait(true);
                audioBucketItemsInstance.Select(bucketItemsInstance, MediaType);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
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

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbBuckets.SelectedItem is Bucket _) && (lbBucketItems.SelectedItem is BucketItem _);
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if ((lbBuckets.SelectedItem is Bucket bucket) && (lbBucketItems.SelectedItem is BucketItem bucketItem))
            {
                var mediaReference = new S3MediaReference(bucket.Name, bucketItem.Key, bucketItem.MimeType, bucketItem.Extension);

                CommandParameter = new UITaskArguments()
                {
                    Mode = UITaskMode.SetArgument,
                    TaskArguments = new UITaskArgument[] { UITaskArgument.NewS3MediaReference(mediaReference) }
                };

                ExecuteCommand();
            }
        }
    }

    public static class S3MediaReferenceCommands
    {
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(S3MediaReferenceCommands),
                null
            );
    }
}
