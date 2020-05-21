using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Tustler.Models;
using TustlerAWSLib;
using TustlerFSharpPlatform;
using TustlerInterfaces;
using TustlerModels;
using TustlerServicesLib;
using static TustlerFSharpPlatform.TaskArguments;
using AWSTasks = TustlerFSharpPlatform.Tasks;
using AWSMiniTasks = TustlerFSharpPlatform.MiniTasks;
using Tustler.Helpers;
using System.IO;
using Tustler.UserControls.TaskMemberControls;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Tasks.xaml
    /// </summary>
    public partial class TasksManager : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;

        private readonly List<TaskEvent> events;    // ground truth for the events generated in a given session (start task to TaskResponse.TaskComplete)
        private readonly ObservableCollection<TaskResponse> taskResponses;      // bound to UI

        public static readonly DependencyProperty TaskNameProperty = DependencyProperty.Register("TaskName", typeof(string), typeof(TasksManager), new PropertyMetadata("", PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is TasksManager ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var taskName = dependencyPropertyChangedEventArgs.NewValue as string;
                    switch (taskName)
                    {
                        case "Cleanup":
                            //ctrl.TaskArguments = new TaskArguments.NotificationsOnlyArguments(ctrl.awsInterface, new NotificationsList());
                            ctrl.TaskFunction = AWSTasks.Cleanup;
                            break;
                        case "S3FetchItems":
                            //ctrl.TaskArguments = new TaskArguments.NotificationsOnlyArguments(ctrl.awsInterface, new NotificationsList());
                            ctrl.TaskFunction = AWSTasks.S3FetchItems;
                            break;
                        case "TranscribeAudio":
                            //ctrl.TaskArguments = new TaskArguments.TranscribeAudioArguments(ctrl.awsInterface, new NotificationsList());
                            ctrl.TaskFunction = AWSTasks.TranscribeAudio;
                            break;
                    }
                }
            }
        }

        public string TaskName
        {
            get { return (string) GetValue(TaskNameProperty); }
            set { SetValue(TaskNameProperty, value); }
        }

        //public ITaskArgumentCollection TaskArguments
        //{
        //    get;
        //    internal set;
        //}

        public Func<ITaskArgumentCollection, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> TaskFunction
        {
            get;
            internal set;
        }

        public TasksManager(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;

            this.events = new List<TaskEvent>();
            this.taskResponses = new ObservableCollection<TaskResponse>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.ShowMessage("Parameter set", $"Task name set to {TaskName}");

            //grdMembers.RowDefinitions.Clear();
            //grdMembers.ColumnDefinitions.Clear();

            //var requiredMembersOption = TaskArguments.GetRequiredMembers();
            //if (requiredMembersOption.IsRequired)
            //{
            //    // add the required number of rows and columns
            //    for (int i = 0; i < requiredMembersOption.Rows; i++)
            //    {
            //        grdMembers.RowDefinitions.Add(new RowDefinition());
            //    }
            //    for (int i = 0; i < requiredMembersOption.Columns; i++)
            //    {
            //        grdMembers.ColumnDefinitions.Add(new ColumnDefinition());
            //    }

            //    // instantiate the appropriate user control and set the position in the grid
            //    foreach (var requiredMemberGridReference in requiredMembersOption.Members)
            //    {
            //        // instantiate the user control and add to grid container in read-order
            //        UserControl uc;
            //        switch (requiredMemberGridReference.Tag)
            //        {
            //            case "taskName":
            //                var taskNameCtrl = new TaskMemberControls.TaskName
            //                {
            //                    Command = TaskCommands.UpdateTaskArguments,      // Command must come first
            //                    AttachedTask = TaskName
            //                };
            //                uc = taskNameCtrl;
            //                break;
            //            case "mediaRef":
            //                var mediaReferenceCtrl = new TaskMemberControls.MediaReference(awsInterface)
            //                {
            //                    Command = TaskCommands.UpdateTaskArguments,
            //                    MediaType = BucketItemMediaType.Audio
            //                };
            //                uc = mediaReferenceCtrl;
            //                break;
            //            case "filePath":
            //                var filePathCtrl = new TaskMemberControls.FilePath
            //                {
            //                    Command = TaskCommands.UpdateTaskArguments
            //                };
            //                uc = filePathCtrl;
            //                break;
            //            case "transcriptionLanguageCode":
            //                var transcriptionLanguageCodesInstance = this.FindResource("transcriptionLanguageCodesInstance") as TranscriptionLanguageCodesViewModel;
            //                var transcriptionLanguageCodeCtrl = new TaskMemberControls.LanguageCode
            //                {
            //                    Command = TaskCommands.UpdateTaskArguments,
            //                    LanguageCodesViewModel = transcriptionLanguageCodesInstance
            //                };
            //                uc = transcriptionLanguageCodeCtrl;
            //                break;
            //            case "translationLanguageCode":
            //                var translationLanguageCodesInstance = this.FindResource("translationLanguageCodesInstance") as TranslationLanguageCodesViewModel;
            //                var translationLanguageCodeCtrl = new TaskMemberControls.LanguageCode
            //                {
            //                    Command = TaskCommands.UpdateTaskArguments,
            //                    LanguageCodesViewModel = translationLanguageCodesInstance
            //                };
            //                uc = translationLanguageCodeCtrl;
            //                break;
            //            case "vocabularyName":
            //                var vocabularyNameCtrl = new TaskMemberControls.VocabularyName(awsInterface)
            //                {
            //                    Command = TaskCommands.UpdateTaskArguments
            //                };
            //                uc = vocabularyNameCtrl;
            //                break;
            //            default:
            //                throw new ArgumentException("Unknown Task Member Control tag.");
            //        }

            //        Grid.SetRow(uc, requiredMemberGridReference.RowIndex);
            //        Grid.SetColumn(uc, requiredMemberGridReference.ColumnIndex);
            //        Grid.SetRowSpan(uc, requiredMemberGridReference.RowSpan);
            //        Grid.SetColumnSpan(uc, requiredMemberGridReference.ColumnSpan);

            //        grdMembers.Children.Add(uc);
            //    }
            //}
        }

        private async void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems as System.Collections.IEnumerable;

            // add to the bound observable collection (can only add on the Dispatcher thread)
            foreach (var response in newItems.Cast<TaskResponse>())
            {
                switch (response)
                {
                    // argument responses are responses that return actual values
                    case TaskResponse.Bucket _:
                    case TaskResponse.BucketItem _:
                    case TaskResponse.BucketItemsModel _:
                    case TaskResponse.BucketsModel _:
                    case TaskResponse.TranscriptionJobsModel _:
                        events.Add(TaskEvent.NewSetArgument(response));
                        break;
                    case TaskResponse.TaskSelect _:
                        events.Add(TaskEvent.SelectArgument);
                        break;
                    case TaskResponse.TaskComplete _:
                        events.Add(TaskEvent.FunctionCompleted);
                        break;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    taskResponses.Add(response);
                });
            }

            //foreach (var response in newItems.Cast<TaskResponse>())
            //{
            //    switch (response)
            //    {
            //        case TaskResponse.Notification note:
            //            switch (note.Item)
            //            {
            //                case ApplicationErrorInfo error:
            //                    var errorMsg = $"{error.Context}: {error.Message}";
            //                    await AddResponseAsync(errorMsg, true).ConfigureAwait(false);
            //                    break;
            //                case ApplicationMessageInfo msg:
            //                    var infoMsg = $"{msg.Message}: {msg.Detail}";
            //                    await AddResponseAsync(infoMsg, true).ConfigureAwait(false);
            //                    break;
            //            }
            //            break;
            //        case TaskResponse.Bucket taskBucket:
            //            await AddResponseAsync(taskBucket.Item.Name, false).ConfigureAwait(false);
            //            break;
            //        case TaskResponse.BucketItem taskBucketItem:
            //            await AddResponseAsync(taskBucketItem.Item.Key, false).ConfigureAwait(false);
            //            break;
            //        case TaskResponse.TranscriptionJob taskTranscriptionJob:
            //            await AddResponseAsync(taskTranscriptionJob.Item.TranscriptionJobName, false).ConfigureAwait(false);
            //            break;
            //    }
            //}
        }

        private void StartTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;// !(TaskArguments is null) && TaskArguments.IsComplete();
        }

        private async void StartTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //if (TaskArguments.IsComplete())

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // generate an arguments stack (by default an infinite enumerable of Nothing arguments)
                var args = new InfiniteList<MaybeResponse>(MaybeResponse.Nothing);

                // replay the observed events, adding any defined arguments
                foreach (var evt in events)
                {
                    if (evt is TaskEvent.SetArgument arg)
                    {
                        args.Add(MaybeResponse.NewJust(arg.Item));
                    }
                    else if (evt.IsClearArguments)
                    {
                        args.Clear();
                    }
                }

                events.Add(TaskEvent.InvokingFunction);
                var responseStream = TaskFunction(new TaskArguments.NotificationsOnlyArguments(awsInterface, new NotificationsList()), args);
                lbTaskResponses.ItemsSource = taskResponses;

                var collection = new ObservableCollection<TaskResponse>();
                collection.CollectionChanged += Collection_CollectionChanged;

                await TaskQueue.Run(responseStream, collection).ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void StartMiniTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            //var btn = e.OriginalSource as Button;
            //var parameterInfo = btn.Tag as TaskManagerParameterInfoCollection;

            //switch (parameterInfo?.ParameterType)
            //{
            //    // enable if DataContext is set (DataGrid SelectedItem is not null)
            //    case "DownloadPrompt":
            //    case "ConfirmDelete":
            //        var context = (btn.DataContext as TaskResponse);
            //        if (context is null)
            //            e.CanExecute = false;
            //        else
            //            e.CanExecute = true;
            //        break;
            //    case "Download":
            //        var filePath = e.Parameter as string;
            //        e.CanExecute = !string.IsNullOrEmpty(filePath);
            //        break;
            //    default:
            //        e.CanExecute = true;
            //        break;
            //}
        }

        private void StartMiniTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var parameterInfo = e.Parameter as MiniTaskArguments;   // btn.Tag as TaskManagerParameterInfoCollection;

#nullable enable
            static T? GetNotifier<T>(TaskResponse context)
                where T : class
            {
                return context switch
                {
                    TaskResponse.BucketItemsModel bucketItemsModel => bucketItemsModel.Item as T,
                    _ => null
                };
            }

            static void RouteNotifications(NotificationsList sourceNotifications, NotificationsList? sinkNotifications, INotifiableViewModel<Notification>? notifiableInterface)
            {
                if (sourceNotifications.Notifications.Count > 0)
                {
                    var collection = sourceNotifications.Notifications;
                    notifiableInterface?.NotificationsList.Add(collection.First());

                    if (collection.Count > 1)
                    {
                        // show any additional notifications in the main window notifications area
                        foreach (var notification in collection)
                        {
                            sinkNotifications?.Add(notification);
                        }
                    }
                }
            }

            static TaskResponse? GetContext(object eventSource)
            {
                return eventSource switch
                {
                    S3ItemManagement itemManagement => itemManagement.DataContext as TaskResponse,
                    S3BucketSelector bucketSelector => bucketSelector.DataContext as TaskResponse,
                    _ => throw new ArgumentException($"StartMiniTask: Unknown data context")
                };
            }

            void RunDeleteMiniTask(TaskResponse dataContext, MiniTaskArguments parameterInfo)
            {
                // create a new notifications list for each operation
                var (notifications, success, key) = AWSMiniTasks.Delete(awsInterface, new NotificationsList(), dataContext, parameterInfo.TaskArguments.ToArray());
                if (success)
                {
                    // deletion from the remote source was successful; now delete from the local view model
                    var deleteItemInterface = dataContext switch
                    {
                        TaskResponse.BucketItemsModel bucketItemsModel => bucketItemsModel.Item as IDeletableViewModelItem,
                        _ => null
                    };

                    deleteItemInterface?.DeleteItem(key);
                }
                var notifiableInterface = GetNotifier<INotifiableViewModel<Notification>>(dataContext);
                var applicationNotifications = this.FindResource("applicationNotifications") as NotificationsList;
                RouteNotifications(notifications, applicationNotifications, notifiableInterface);
            }

            void RunDownloadMiniTask(TaskResponse dataContext, MiniTaskArguments parameterInfo)
            {
                var (notifications, success, _, _) = AWSMiniTasks.Download(awsInterface, new NotificationsList(), dataContext, parameterInfo.TaskArguments.ToArray());
                if (success)
                {
                    var notifiableInterface = GetNotifier<INotifiableViewModel<Notification>>(dataContext);
                    var applicationNotifications = this.FindResource("applicationNotifications") as NotificationsList;
                    RouteNotifications(notifications, applicationNotifications, notifiableInterface);
                }
            }

            void RunSelectMiniTask(MiniTaskArguments parameterInfo)
            {
                // the user has selected an item that sets an argument
                // first check if the task is complete
                if (events.Last().IsFunctionCompleted)
                {
                    // if so then add a ClearArguments to the events stack before adding SetArgument below
                    events.Add(TaskEvent.ClearArguments);

                    // and prune the ObservableCollection back to the last TaskSelect
                    var tempStack = new Stack<TaskResponse>(taskResponses);
                    TaskResponse? lastResponse = null;
                    while (!tempStack.Peek().IsTaskSelect)
                    {
                        lastResponse = tempStack.Pop();
                    }

                    if (lastResponse is object)
                    {
                        // re-add the last response (immediately after the TaskSelect)
                        tempStack.Push(lastResponse);

                        // add the last response as a SetArgument to the event source (after a SelectArgument)
                        events.Add(TaskEvent.SelectArgument);
                        events.Add(TaskEvent.NewSetArgument(lastResponse));
                    }

                    // clearing the ObservableCollection will disconnect the data bindings (the DataContext on the ItemsControl item containers)
                    // instead, just remove the last numItems items
                    var numItems = taskResponses.Count - tempStack.Count;
                    for (int i = 0; i < numItems; i++)
                    {
                        taskResponses.RemoveAt(taskResponses.Count - 1);
                    }
                }

                // Add a SetArgument event to the events list and reinvoke the function
                var bucket = parameterInfo.TaskArguments.First() switch
                {
                    MiniTaskArgument.Bucket bucketArg => bucketArg.Item,
                    _ => throw new ArgumentException($"RunSelectBucketMiniTask: Unknown argument type")
                };
                events.Add(TaskEvent.NewSetArgument(TaskResponse.NewBucket(bucket)));

                btnStartTask.Command.Execute(null);
            }

#nullable disable

            switch (parameterInfo.Mode.Tag)
            {
                case MiniTaskMode.Tags.Delete:
                    RunDeleteMiniTask(GetContext(e.OriginalSource), parameterInfo);
                    break;
                case MiniTaskMode.Tags.Download:
                    RunDownloadMiniTask(GetContext(e.OriginalSource), parameterInfo);
                    break;
                case MiniTaskMode.Tags.Select:
                    RunSelectMiniTask(parameterInfo);
                    break;
                case MiniTaskMode.Tags.Continue:
                    // disable the Continue button
                    (e.OriginalSource as TaskContinue).IsButtonEnabled = false;
                    btnStartTask.Command.Execute(null);
                    break;
                default:
                    throw new ArgumentException($"StartMiniTask_Executed: unknown parameter mode for S3ItemManagementParameter");
            }
        }

        //private void UpdateTaskArguments_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = true;
        //}

        //private void UpdateTaskArguments_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    if (e.Parameter is TaskArgumentMember data)
        //        this.TaskArguments.SetValue(data);
        //}

        //private async Task RunTaskAsync()
        //{
        //    //"TranscribeAudio1",
        //    //new MediaReference("tator", "TranscribeAudio1", "audio/wav", "wav"),
        //    //@"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\SallyRide2.wav",
        //    //"en-US", "Test")
        //}

        //private async Task AddResponseAsync(string content, bool showBold)
        //{
        //    await Dispatcher.InvokeAsync(() =>
        //    {
        //        var tb = new TextBlock(new Run(content) { FontWeight = showBold ? FontWeights.Bold : FontWeights.Normal });
        //        lbTaskResponses.Items.Add(tb);
        //    });
        //}
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

        public static readonly RoutedUICommand StartMiniTask = new RoutedUICommand
            (
                "StartMiniTask",
                "StartMiniTask",
                typeof(TaskCommands),
                null
            );

        //public static readonly RoutedUICommand UpdateTaskArguments = new RoutedUICommand
        //    (
        //        "UpdateTaskArguments",
        //        "UpdateTaskArguments",
        //        typeof(TaskCommands),
        //        null
        //    );
    }

}
