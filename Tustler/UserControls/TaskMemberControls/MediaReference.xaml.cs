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
using TustlerInterfaces;
using TustlerModels;
using TustlerServicesLib;
using static TustlerFSharpPlatform.TaskArguments;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for MediaReference.xaml
    /// </summary>
    public partial class MediaReference : UserControl
    {
        public static readonly DependencyProperty MediaTypeProperty =
            DependencyProperty.Register(
                "MediaType",
                typeof(BucketItemMediaType),
                typeof(MediaReference));

        private readonly IAmazonWebInterfaceS3 s3Interface;
        private readonly NotificationsList notifications;

        public MediaReference()
        {
            InitializeComponent();

            s3Interface = new S3();
            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

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

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            BucketViewModel bucketViewModel = this.FindResource("bucketsInstance") as BucketViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketViewModel.Refresh(s3Interface, false, notifications).ConfigureAwait(true);
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
                typeof(MediaReference),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MediaReference ctrl = (MediaReference)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get
            {
                if (!(lbBuckets.SelectedItem is Bucket bucket) || !(lbBucketItems.SelectedItem is BucketItem bucketItem))
                {
                    return null;
                }
                else
                {
                    var mediaReference = new TaskArguments.MediaReference(bucket.Name, bucketItem.Key, bucketItem.MimeType, bucketItem.Extension);
                    return TaskArgumentMember.NewMediaRef(mediaReference);
                }
            }
        }

        #endregion

        private async void BucketsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)e.Source;
            Bucket selectedBucket = (Bucket)listBox.SelectedItem;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            var audioBucketItemsInstance = this.FindResource("audioBucketItemsInstance") as MediaFilteredBucketItemViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketItemsInstance.Refresh(s3Interface, notifications, selectedBucket.Name).ConfigureAwait(true);
                audioBucketItemsInstance.Select(bucketItemsInstance, MediaType);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BucketItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
