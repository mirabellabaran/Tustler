using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TustlerFSharpPlatform;
using TustlerModels;
using TustlerServicesLib;
using AWSTasks = TustlerFSharpPlatform.Tasks;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Tasks.xaml
    /// </summary>
    public partial class Tasks : UserControl
    {
        public static readonly DependencyProperty TaskNameProperty = DependencyProperty.Register("TaskName", typeof(string), typeof(Tasks), new PropertyMetadata("", PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var ctrl = dependencyObject as Tasks;
            if (ctrl != null)
            {
                //if (dependencyPropertyChangedEventArgs.NewValue != null)
                //    ctrl.ReportViewerLoad(dependencyPropertyChangedEventArgs.NewValue.ToString());
            }
        }

        public string TaskName
        {
            get { return (string) GetValue(TaskNameProperty); }
            set { SetValue(TaskNameProperty, value); }
        }

        public Tasks()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.ShowMessage("Parameter set", $"Task name set to {TaskName}");
        }

        private async void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems as System.Collections.IEnumerable;
            foreach (var response in newItems.Cast<TaskResponse>())
            {
                switch (response)
                {
                    case TaskResponse.Notification note:
                        switch (note.Item)
                        {
                            case ApplicationErrorInfo error:
                                var errorMsg = $"{error.Context}: {error.Message}";
                                await AddControlAsync(errorMsg, true).ConfigureAwait(false);
                                break;
                            case ApplicationMessageInfo msg:
                                var infoMsg = $"{msg.Message}: {msg.Detail}";
                                await AddControlAsync(infoMsg, true).ConfigureAwait(false);
                                break;
                        }
                        break;
                    case TaskResponse.Bucket taskBucket:
                        await AddControlAsync(taskBucket.Item.Name, false).ConfigureAwait(false);
                        break;
                    case TaskResponse.BucketItem taskBucketItem:
                        await AddControlAsync(taskBucketItem.Item.Key, false).ConfigureAwait(false);
                        break;
                }
            }
        }

        private void StartTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void StartTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await RunTaskAsync().ConfigureAwait(true);
        }

        private async Task RunTaskAsync()
        {
            var task = TaskName switch
            {
                "S3FetchItems" => AWSTasks.S3FetchItems(TaskArgument.NoArguments),
                "TranscribeAudio" => AWSTasks.TranscribeAudio(TaskArgument.NewS3MediaReference(
                        ("TranscribeAudio1", "tator", @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\SallyRide2.wav", "audio/wav", "wav")
                    )),
                _ => throw new ArgumentException($"RunTaskAsync: received an unknown tag")
            };

            var collection = new ObservableCollection<TaskResponse>();
            collection.CollectionChanged += Collection_CollectionChanged;

            await TaskQueue.Run(task, collection).ConfigureAwait(true);
        }

        private async Task AddControlAsync(string content, bool showBold)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var tb = new TextBlock(new Run(content) { FontWeight = showBold ? FontWeights.Bold : FontWeights.Normal });
                lbTaskResponses.Items.Add(tb);
            });
        }
    }

    public static class TaskCommands
    {
        public static readonly RoutedUICommand StartTask = new RoutedUICommand
            (
                "StartTask",
                "StartTask",
                typeof(TaskCommands),
                null
            );
    }

}
