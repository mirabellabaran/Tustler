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

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Tasks.xaml
    /// </summary>
    public partial class TasksManager : UserControl
    {
        private readonly AmazonWebServiceInterface awsInterface;

        private readonly Stack<TaskEvent> events;    // ground truth for the events generated in a given session (start task to TaskResponse.TaskComplete)
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
                        case "TranscribeCleanup":
                            ctrl.TaskArguments = new TaskArguments.NotificationsOnlyArguments(ctrl.awsInterface, new NotificationsList());
                            ctrl.TaskFunction = AWSTasks.TranscribeCleanup;
                            break;
                        case "S3FetchItems":
                            ctrl.TaskArguments = new TaskArguments.NotificationsOnlyArguments(ctrl.awsInterface, new NotificationsList());
                            ctrl.TaskFunction = AWSTasks.S3FetchItems;
                            break;
                        case "TranscribeAudio":
                            ctrl.TaskArguments = new TaskArguments.TranscribeAudioArguments(ctrl.awsInterface, new NotificationsList());
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

        public ITaskArgumentCollection TaskArguments
        {
            get;
            internal set;
        }

        public Func<ITaskArgumentCollection, Stack<MaybeResponse>, IEnumerable<TaskResponse>> TaskFunction
        {
            get;
            internal set;
        }

        public TasksManager(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;

            this.events = new Stack<TaskEvent>();
            this.taskResponses = new ObservableCollection<TaskResponse>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.ShowMessage("Parameter set", $"Task name set to {TaskName}");

            grdMembers.RowDefinitions.Clear();
            grdMembers.ColumnDefinitions.Clear();

            var requiredMembersOption = TaskArguments.GetRequiredMembers();
            if (requiredMembersOption.IsRequired)
            {
                // add the required number of rows and columns
                for (int i = 0; i < requiredMembersOption.Rows; i++)
                {
                    grdMembers.RowDefinitions.Add(new RowDefinition());
                }
                for (int i = 0; i < requiredMembersOption.Columns; i++)
                {
                    grdMembers.ColumnDefinitions.Add(new ColumnDefinition());
                }

                // instantiate the appropriate user control and set the position in the grid
                foreach (var requiredMemberGridReference in requiredMembersOption.Members)
                {
                    // instantiate the user control and add to grid container in read-order
                    UserControl uc;
                    switch (requiredMemberGridReference.Tag)
                    {
                        case "taskName":
                            var taskNameCtrl = new TaskMemberControls.TaskName
                            {
                                Command = TaskCommands.UpdateTaskArguments,      // Command must come first
                                AttachedTask = TaskName
                            };
                            uc = taskNameCtrl;
                            break;
                        case "mediaRef":
                            var mediaReferenceCtrl = new TaskMemberControls.MediaReference(awsInterface)
                            {
                                Command = TaskCommands.UpdateTaskArguments,
                                MediaType = BucketItemMediaType.Audio
                            };
                            uc = mediaReferenceCtrl;
                            break;
                        case "filePath":
                            var filePathCtrl = new TaskMemberControls.FilePath
                            {
                                Command = TaskCommands.UpdateTaskArguments
                            };
                            uc = filePathCtrl;
                            break;
                        case "transcriptionLanguageCode":
                            var transcriptionLanguageCodesInstance = this.FindResource("transcriptionLanguageCodesInstance") as TranscriptionLanguageCodesViewModel;
                            var transcriptionLanguageCodeCtrl = new TaskMemberControls.LanguageCode
                            {
                                Command = TaskCommands.UpdateTaskArguments,
                                LanguageCodesViewModel = transcriptionLanguageCodesInstance
                            };
                            uc = transcriptionLanguageCodeCtrl;
                            break;
                        case "translationLanguageCode":
                            var translationLanguageCodesInstance = this.FindResource("translationLanguageCodesInstance") as TranslationLanguageCodesViewModel;
                            var translationLanguageCodeCtrl = new TaskMemberControls.LanguageCode
                            {
                                Command = TaskCommands.UpdateTaskArguments,
                                LanguageCodesViewModel = translationLanguageCodesInstance
                            };
                            uc = translationLanguageCodeCtrl;
                            break;
                        case "vocabularyName":
                            var vocabularyNameCtrl = new TaskMemberControls.VocabularyName(awsInterface)
                            {
                                Command = TaskCommands.UpdateTaskArguments
                            };
                            uc = vocabularyNameCtrl;
                            break;
                        default:
                            throw new ArgumentException("Unknown Task Member Control tag.");
                    }

                    Grid.SetRow(uc, requiredMemberGridReference.RowIndex);
                    Grid.SetColumn(uc, requiredMemberGridReference.ColumnIndex);
                    Grid.SetRowSpan(uc, requiredMemberGridReference.RowSpan);
                    Grid.SetColumnSpan(uc, requiredMemberGridReference.ColumnSpan);

                    grdMembers.Children.Add(uc);
                }
            }
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
                    case TaskResponse.TranscriptionJob _:
                        events.Push(TaskEvent.NewSetArgument(response));
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
            if (TaskArguments.IsComplete())
            {
                //var eventDict = new Dictionary<int, TaskEvent>(events.Where(evt => evt switch
                //{
                //    TaskEvent.SetArgument _ => true,
                //    _ => false
                //})
                //.Select((evt, i) => KeyValuePair.Create(i, evt)));

                // TODO generate correct number of arguments
                //var count = 3;
                //var argDict = new Dictionary<int, MaybeResponse>(Enumerable.Range(0, count).Select(i => KeyValuePair.Create(i, MaybeResponse.Nothing)));

                //foreach (var setArgumentEvent in eventDict)
                //{
                //    if (setArgumentEvent.Value is TaskEvent.SetArgument arg)
                //    {
                //        argDict[setArgumentEvent.Key] = MaybeResponse.NewJust(arg.Item);
                //    }
                //}

                // generate an arguments stack based on the observed events
                var argCount = 3;
                var setArgumentCount = events.Count(evt => evt switch
                {
                    TaskEvent.SetArgument _ => true,
                    _ => false
                });

                // create arguments stack and add unset arguments
                var args = new Stack<MaybeResponse>();
                for (int i = 0; i < argCount - setArgumentCount; i++)
                {
                    args.Push(MaybeResponse.Nothing);
                }

                foreach (var evt in events)
                {
                    if (evt is TaskEvent.SetArgument arg)
                    {
                        args.Push(MaybeResponse.NewJust(arg.Item));
                    }
                }

                var responseStream = TaskFunction(TaskArguments, args);
                lbTaskResponses.ItemsSource = taskResponses;

                var collection = new ObservableCollection<TaskResponse>();
                collection.CollectionChanged += Collection_CollectionChanged;

                await TaskQueue.Run(responseStream, collection).ConfigureAwait(true);
            }
        }

        private void StartMiniTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void StartMiniTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
#nullable enable
            static object? GetValueFromModel(object? model, Type? itemType, string itemName)
            {
                var modelType = model?.GetType();

                if (modelType is object && itemType is object)
                {
                    var propertyInfo = modelType.GetProperty(itemName, itemType);
                    return propertyInfo?.GetValue(model);
                }
                else
                {
                    return null;
                }
            }

            static MiniTaskArgument? GenerateArgument(object? model, string typeName, string itemName)
            {
                MiniTaskArgument? result = null;
                var itemType = Type.GetType(typeName);
                var value = GetValueFromModel(model, itemType, itemName);

                if (value is object)
                {
                    result = itemType?.FullName switch
                    {
                        "System.Boolean" => MiniTaskArgument.NewBool((bool)value),
                        "System.Int" => MiniTaskArgument.NewInt((int)value),
                        "System.String" => MiniTaskArgument.NewString((string)value),
                        _ => null
                    };
                }

                return result;
            }
#nullable disable

            // transform each TaskManagerCommandParameterArgument into a MiniTaskArgument
            MiniTaskArgument[] GenerateArgumentList(TaskManagerCommandParameterValue parameterValue)
            {
                var boundResponse = (e.OriginalSource as Button).DataContext as TaskResponse;
                var model = boundResponse switch
                {
                    TaskResponse.BucketItem taskBucketItem => taskBucketItem.Item as object,
                    _ => null
                };

                return parameterValue.Items.Cast<TaskManagerCommandParameterArgument>()
                    .Select(arg => GenerateArgument(model, arg.ItemType, arg.ItemKey))
                    .ToArray();
            }

            switch (e.Parameter as TaskManagerCommandParameterValue)
            {
                case TaskManagerCommandParameterValue parameterValue when parameterValue.ParameterType == "Delete":
                    var taskUpdate = AWSMiniTasks.Delete(awsInterface, new NotificationsList(), GenerateArgumentList(parameterValue));
                    var success = taskUpdate?.Item1;
                    var notificationData = taskUpdate.Item2;
                    if (notificationData is object && notificationData.Notifications.Count > 0)
                    {
                        var notifications = this.FindResource("applicationNotifications") as NotificationsList;
                        foreach (var notification in notificationData.Notifications)
                        {
                            notifications.Add(notification);
                        }
                    }
                    break;
                case TaskManagerCommandParameterValue parameterValue when parameterValue.ParameterType == "Select":

                    // TODO check if task is complete; if so then truncate BOTH the ObservableCollection and the events stack before adding

                    // Add a SetArgument event to the events list and reinvoke the function
                    var model = ((e.OriginalSource as Button).DataContext);
                    switch (model)
                    {
                        case Bucket bucket:
                            events.Push(TaskEvent.NewSetArgument(TaskResponse.NewBucket(bucket)));
                            break;
                    }

                    btnStartTask.Command.Execute(null);
                    break;
                default:
                    throw new ArgumentException($"StartMiniTask_Executed: unknown parameter value for TaskManagerCommandParameterValue");
            }
        }

        private void UpdateTaskArguments_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void UpdateTaskArguments_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is TaskArgumentMember data)
                this.TaskArguments.SetValue(data);
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

        public static readonly RoutedUICommand UpdateTaskArguments = new RoutedUICommand
            (
                "UpdateTaskArguments",
                "UpdateTaskArguments",
                typeof(TaskCommands),
                null
            );
    }

}
