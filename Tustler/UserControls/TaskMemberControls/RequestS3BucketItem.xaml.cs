using CloudWeaver;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
using Tustler.Models;
using TustlerAWSLib;
using TustlerModels;
using TustlerServicesLib;
using TustlerUIShared;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for RequestS3BucketItem.xaml
    /// </summary>
    public partial class RequestS3BucketItem : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(RequestS3BucketItem), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is RequestS3BucketItem ctrl)
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

        #region BucketItemExtensionDescription DependencyProperty
        public static readonly DependencyProperty BucketItemExtensionDescriptionProperty =
            DependencyProperty.Register(
                "BucketItemExtensionDescription",
                typeof(string),
                typeof(RequestS3BucketItem));

        /// <summary>
        /// A description of the required extension filter e.g. 'image' or 'JSON'
        /// </summary>
        public string BucketItemExtensionDescription
        {
            get
            {
                return (string)GetValue(BucketItemExtensionDescriptionProperty);
            }
            set
            {
                SetValue(BucketItemExtensionDescriptionProperty, value);
            }
        }
        #endregion

        #region BucketItemExtension DependencyProperty
        public static readonly DependencyProperty BucketItemExtensionProperty =
            DependencyProperty.Register(
                "BucketItemExtension",
                typeof(string),
                typeof(RequestS3BucketItem));

        /// <summary>
        /// The file extension of the required filter e.g. 'json', 'jpg' or 'doc'
        /// </summary>
        public string BucketItemExtension
        {
            get
            {
                return (string)GetValue(BucketItemExtensionProperty);
            }
            set
            {
                SetValue(BucketItemExtensionProperty, value);
            }
        }
        #endregion

        private readonly AmazonWebServiceInterface awsInterface;
        private readonly NotificationsList notifications;

        public RequestS3BucketItem()
        {
            InitializeComponent();

            var serviceProvider = (Application.Current as App).ServiceProvider;

            this.awsInterface = serviceProvider.GetRequiredService<AmazonWebServiceInterface>();
            this.notifications = this.FindResource("applicationNotifications") as NotificationsList;

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot control use this as the context
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
                typeof(RequestS3BucketItem),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RequestS3BucketItem ctrl = (RequestS3BucketItem)d;
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
            e.CanExecute = (lbBucketItems.SelectedItem is object);
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var bucketItem = lbBucketItems.SelectedItem as BucketItem;

            var data = JsonSerializer.SerializeToUtf8Bytes(bucketItem);

            CommandParameter = new UITaskArguments(UITaskMode.TransformSetArgument, "AWSShareIntraModule", "BucketItem", data);

            ExecuteCommand();
        }

        private async void BucketsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = e.Source as ListBox;
            Bucket selectedBucket = listBox.SelectedItem as Bucket;

            var bucketItemsInstance = this.FindResource("bucketItemsInstance") as BucketItemViewModel;
            var filteredBucketItemsInstance = this.FindResource("filteredBucketItemsInstance") as FilteredBucketItemViewModel;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await bucketItemsInstance.ForceRefresh(awsInterface, notifications, selectedBucket.Name).ConfigureAwait(true);
                filteredBucketItemsInstance.Clear();
                if (this.BucketItemExtension is null)
                    filteredBucketItemsInstance.Select(bucketItemsInstance, BucketItemMediaType.All);
                else
                    filteredBucketItemsInstance.Select(bucketItemsInstance, this.BucketItemExtension);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }

    public static class RequestS3BucketItemCommands
    {
        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(RequestS3BucketItemCommands),
                null
            );
    }
}
