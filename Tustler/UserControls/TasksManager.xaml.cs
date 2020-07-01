#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Helpers.UIServices;
using Tustler.UserControls.TaskMemberControls;
using TustlerAWSLib;
using TustlerFSharpPlatform;
using TustlerInterfaces;
using TustlerServicesLib;
using static TustlerFSharpPlatform.TaskArguments;
using AWSMiniTasks = TustlerFSharpPlatform.MiniTasks;
using AWSTasks = TustlerFSharpPlatform.Tasks;

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
                    ctrl.TaskFunction = taskName switch
                    {
                        "S3FetchItems" => AWSTasks.S3FetchItems,
                        "Cleanup" => AWSTasks.Cleanup,
                        "CleanTranscriptionJobHistory" => AWSTasks.CleanTranscriptionJobHistory,
                        "SomeSubTask" => AWSTasks.SomeSubTask,
                        "TranscribeAudio" => AWSTasks.TranscribeAudio,//ctrl.TaskArguments = new TaskArguments.TranscribeAudioArguments(ctrl.awsInterface, new NotificationsList());
                        "UploadMediaFile" => AWSTasks.UploadMediaFile,
                        "StartTranscription" => AWSTasks.StartTranscription,
                        "MonitorTranscription" => AWSTasks.MonitorTranscription,
                        _ => throw new ArgumentException($"Unknown task name '{taskName}'"),
                    };
                }
            }
        }

        public string TaskName
        {
            get { return (string)GetValue(TaskNameProperty); }
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
            this.TaskFunction = AWSTasks.MinimalMethod;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications?.ShowMessage("Parameter set", $"Task name set to {TaskName}");

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

        private SubTaskItem? IsTaskAvailable()
        {
            // get the next item on the ForEach stack (if there is one)

            // ... first get the last ForEach event
            var lastForeach = events.FindLast(evt =>
                evt switch
                {
                    TaskEvent.ForEach _ => true,
                    _ => false
                }
            );

            // ... and retrieve the next item from its payload
            return lastForeach switch
            {
                TaskEvent.ForEach items => (items.Item.Count > 0) ? items.Item.Pop() : null,
                _ => null
            };
        }

        private void PrepareNextTask(SubTaskItem nextTask)
        {
            //events.Add(TaskEvent.ClearArguments);             // TODO MG might need this for Cleanup Task
            events.Add(TaskEvent.NewSubTask(nextTask.TaskName));
            //events.Add(TaskEvent.NewSetArgument(TaskResponse.NewTaskItem(nextTask)));     // TODO and this
        }

        private async void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems as System.Collections.IEnumerable;
            SubTaskItem? nextTask = null;

            // add to the bound observable collection (can only add on the Dispatcher thread)
            foreach (var response in newItems.Cast<TaskResponse>())
            {
                switch (response)
                {
                    // argument responses that return values to be passed back via SetArgument event
                    case TaskResponse.Bucket _:
                    //case TaskResponse.BucketItem _:
                    case TaskResponse.BucketItemsModel _:
                    case TaskResponse.BucketsModel _:
                    case TaskResponse.TranscriptionJobName _:       // note that TranscriptionJobsModel is for UI use only whereas TranscriptionJobName must be passed back
                    case TaskResponse.FileUpload _:
                        events.Add(TaskEvent.NewSetArgument(response));
                        break;

                    case TaskResponse.TaskSelect _:
                        events.Add(TaskEvent.SelectArgument);
                        break;
                    case TaskResponse.TaskSequence taskSequence:
                        var subTasks = new RetainingStack<SubTaskItem>(taskSequence.Item);
                        events.Add(TaskEvent.NewForEach(subTasks));
                        break;
                    case TaskResponse.TaskComplete _:
                        events.Add(TaskEvent.FunctionCompleted);

                        // check for a next sub-task (if there is one)
                        nextTask = IsTaskAvailable();
                        break;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    taskResponses.Add(response);
                    lbTaskResponses.ScrollIntoView(response);
                });
            }

            // if looping over sub-tasks then dispatch a ContinueWith Next response
            if (nextTask is object)
            {
                PrepareNextTask(nextTask);

                await Dispatcher.InvokeAsync(() =>
                {
                    taskResponses.Add(TaskResponse.NewTaskContinueWith(ContinueWithArgument.Next));
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

                await RunTask().ConfigureAwait(true);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task RunTask()
        {
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
            static T? GetNotifier<T>(TaskResponse? context) where T : class
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

            void RunDeleteMiniTask(TaskResponse? dataContext, MiniTaskArguments parameterInfo)
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

            void RunDownloadMiniTask(TaskResponse? dataContext, MiniTaskArguments parameterInfo)
            {
                var (notifications, success, _, _) = AWSMiniTasks.Download(awsInterface, new NotificationsList(), dataContext, parameterInfo.TaskArguments.ToArray());
                if (success)
                {
                    var notifiableInterface = GetNotifier<INotifiableViewModel<Notification>>(dataContext);
                    var applicationNotifications = this.FindResource("applicationNotifications") as NotificationsList;
                    RouteNotifications(notifications, applicationNotifications, notifiableInterface);
                }
            }

            var parameterInfo = e.Parameter as MiniTaskArguments;

            switch (parameterInfo?.Mode.Tag)
            {
                case MiniTaskMode.Tags.Delete:
                    RunDeleteMiniTask(GetContext(e.OriginalSource), parameterInfo);
                    break;
                case MiniTaskMode.Tags.Download:
                    RunDownloadMiniTask(GetContext(e.OriginalSource), parameterInfo);
                    break;
                default:
                    throw new ArgumentException($"StartMiniTask_Executed: unknown mode for parameterInfo");
            }
        }

        private void UIResponse_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void UIResponse_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            async Task RunUIResponseSelect(MiniTaskArguments parameterInfo)
            {
                // disable the Continue button and restart the task
                switch (e.OriginalSource)
                {
                    case RequestFileMediaReference ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestS3MediaReference ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestS3Bucket ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestLanguageCode ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestVocabularyName ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                }

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
                switch (parameterInfo.TaskArguments.First())
                {
                    case MiniTaskArgument.Bucket bucketArg:
                        var bucket = bucketArg.Item;
                        events.Add(TaskEvent.NewSetArgument(TaskResponse.NewBucket(bucket)));
                        break;
                    case MiniTaskArgument.FilePath filePathArg:
                        var filePath = filePathArg.Item;
                        events.Add(TaskEvent.NewSetArgument(TaskResponse.NewFilePath(filePath)));
                        break;
                    case MiniTaskArgument.FileMediaReference mediaReferenceArg:
                        var mediaReference = mediaReferenceArg.Item;
                        events.Add(TaskEvent.NewSetArgument(TaskResponse.NewFileMediaReference(mediaReference)));
                        break;
                    case MiniTaskArgument.TranscriptionLanguageCode transcriptionLanguageCodeArg:
                        var transcriptionLanguageCode = transcriptionLanguageCodeArg.Item;
                        events.Add(TaskEvent.NewSetArgument(TaskResponse.NewTranscriptionLanguageCode(transcriptionLanguageCode)));
                        break;
                    case MiniTaskArgument.TranslationLanguageCode translationLanguageCodeArg:
                        var translationLanguageCode = translationLanguageCodeArg.Item;
                        events.Add(TaskEvent.NewSetArgument(TaskResponse.NewTranscriptionLanguageCode(translationLanguageCode)));
                        break;
                    case MiniTaskArgument.VocabularyName vocabularyNameArg:
                        var vocabularyName = vocabularyNameArg.Item;
                        events.Add(TaskEvent.NewSetArgument(TaskResponse.NewVocabularyName(vocabularyName)));
                        break;
                    default:
                        throw new ArgumentException($"RunSelectBucketMiniTask: Unknown argument type");
                }

                await RunTask().ConfigureAwait(true);
            }

            async Task RunUIResponseAutoContinue(MiniTaskArguments parameterInfo)
            {
                static bool IsContinueWithNext(MiniTaskArgument arg)
                {
                    static bool CheckIfNext(ContinueWithArgument arg)
                    {
                        return arg.Tag switch
                        {
                            ContinueWithArgument.Tags.Next => true,
                            ContinueWithArgument.Tags.None => false,
                            _ => throw new NotImplementedException()
                        };
                    }

                    var continueWithArg = arg switch
                    {
                        MiniTaskArgument.ContinueWithArgument x => x.Item,
                        _ => throw new ArgumentException($"Unknown argument type")
                    };

                    return CheckIfNext(continueWithArg);
                }

                if (IsContinueWithNext(parameterInfo.TaskArguments.First()))
                {
                    // find the last SubTask event
                    var subTaskEvent = events.FindLast(evt =>
                        evt switch
                        {
                            TaskEvent.SubTask _ => true,
                            _ => false
                        }
                    );

                    // and set the task name
                    this.TaskName = subTaskEvent switch
                    {
                        TaskEvent.SubTask name => name.Item,
                        _ => throw new ArgumentException("Expecting a sub-task event in the events list"),
                    };
                }

                await RunTask().ConfigureAwait(true);
            }

            async Task RunUIResponseContinue()
            {
                // disable the Continue button and restart the task
                if (e.OriginalSource is TaskContinue ctrl) ctrl.IsButtonEnabled = false;
                await RunTask().ConfigureAwait(true);
            }

            void RunUIResponseForEach(MiniTaskArguments parameterInfo)
            {
                // expecting a single argument (an IEnumerable<MultiSelectArgument>)
                var subtasks = parameterInfo.TaskArguments.First() switch
                {
                    MiniTaskArgument.ForEach args => new RetainingStack<SubTaskItem>(args.Item),
                    _ => throw new ArgumentException($"Unknown argument type")
                };

                // now that the user has made their selections, add those selections to the event source for later reference
                events.Add(TaskEvent.NewForEach(subtasks));

                // pop the first task item and set an argument on the event source
                if (subtasks.Count > 0)
                {
                    // go to the first sub-task
                    var taskItem = subtasks.Pop();
                    PrepareNextTask(taskItem);
                    taskResponses.Add(TaskResponse.NewTaskContinueWith(ContinueWithArgument.Next));
                }
            }

            var parameterInfo = e.Parameter as MiniTaskArguments;

            switch (parameterInfo?.Mode.Tag)
            {
                case MiniTaskMode.Tags.Select:
                    await RunUIResponseSelect(parameterInfo).ConfigureAwait(true);
                    break;
                case MiniTaskMode.Tags.Continue:
                    await RunUIResponseContinue().ConfigureAwait(true);
                    break;
                case MiniTaskMode.Tags.AutoContinue:
                    await RunUIResponseAutoContinue(parameterInfo).ConfigureAwait(true);
                    break;
                case MiniTaskMode.Tags.ForEach:
                    RunUIResponseForEach(parameterInfo);
                    break;
                default:
                    throw new ArgumentException($"UIResponse_Executed: unknown mode for parameterInfo");
            }
        }

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

        public static readonly RoutedUICommand UIResponse = new RoutedUICommand
            (
                "UIResponse",
                "UIResponse",
                typeof(TaskCommands),
                null
            );
    }
}
