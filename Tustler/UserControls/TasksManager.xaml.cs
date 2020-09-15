﻿#nullable enable

using CloudWeaver;
using CloudWeaver.AWS;
using CloudWeaver.Types;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Helpers;
using Tustler.Helpers.UIServices;
using Tustler.Models;
using Tustler.UserControls.TaskMemberControls;
using TustlerAWSLib;
using TustlerFSharpPlatform;
using TustlerInterfaces;
using TustlerModels;
using TustlerServicesLib;
using AWSMiniTasks = CloudWeaver.AWS.MiniTasks;
using AWSTasks = CloudWeaver.AWS.Tasks;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Tasks.xaml
    /// </summary>
    public partial class TasksManager : UserControl
    {
        private const string EventStackArgumentRestoreName = "defaultargs.json";

        private readonly NotificationsList notificationsList;
        private readonly AmazonWebServiceInterface awsInterface;
        private readonly TaskLogger taskLogger;

        private readonly Dictionary<string, TaskFunctionSpecifier> taskFunctionLookup;
        private readonly Queue<TaskFunctionSpecifier> taskQueue;

        private readonly Agent agent;                                           // the executor that manages Task Function execution
        private readonly ObservableCollection<TaskResponse> taskResponses;      // the sequence of UI responses generated by a call to a Task Function; bound to the UI

        public static readonly DependencyProperty TaskSpecifierProperty = DependencyProperty.Register("TaskSpecifier", typeof(TaskFunctionSpecifier), typeof(TasksManager), new PropertyMetadata(null, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is TasksManager ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var taskSpecifier = dependencyPropertyChangedEventArgs.NewValue as TaskFunctionSpecifier;
                    ctrl.TaskFunction = taskSpecifier?.TaskName switch
                    {
                        "S3FetchItems" => AWSTasks.S3FetchItems,

                        "Cleanup" => AWSTasks.Cleanup,
                        "CleanTranscriptionJobHistory" => AWSTasks.CleanTranscriptionJobHistory,
                        "SomeSubTask" => AWSTasks.SomeSubTask,

                        "TranscribeAudio" => AWSTasks.TranscribeAudio,
                        "UploadMediaFile" => AWSTasks.UploadMediaFile,
                        "StartTranscription" => AWSTasks.StartTranscription,
                        "MonitorTranscription" => AWSTasks.MonitorTranscription,
                        "DownloadTranscriptFile" => AWSTasks.DownloadTranscriptFile,
                        "ExtractTranscript" => AWSTasks.ExtractTranscript,
                        "SaveTranscript" => AWSTasks.SaveTranscript,

                        "CreateSubTitles" => AWSTasks.CreateSubTitles,

                        "MultiLanguageTranslateText" => AWSTasks.MultiLanguageTranslateText,
                        "TranslateText" => AWSTasks.TranslateText,
                        "SaveTranslation" => AWSTasks.SaveTranslation,

                        "ConvertJsonLogToLogFormat" => AWSTasks.ConvertJsonLogToLogFormat,
                        "ConvertLogFormatToJsonLog" => AWSTasks.ConvertLogFormatToJsonLog,

                        _ => throw new ArgumentException($"Unknown task name '{taskSpecifier?.TaskName}'"),
                    };
                }
            }
        }

        public TaskFunctionSpecifier TaskSpecifier
        {
            get { return (TaskFunctionSpecifier)GetValue(TaskSpecifierProperty); }
            set { SetValue(TaskSpecifierProperty, value); }
        }

        ///// <summary>
        ///// The name of the root task called when this user control is constructed
        ///// </summary>
        ///// <remarks>This remains constant throughout the task run (unlike the TaskSpecifier) and can be used to generate the working directory path</remarks>
        //public string RootTaskName
        //{
        //    get
        //    {
        //        return this.RootTaskName;
        //    }
        //    set
        //    {
        //        RootTaskName = value;
        //    }
        //}

        public Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> TaskFunction
        {
            get;
            internal set;
        }

        public TasksManager(TaskFunctionSpecifier[] taskFunctions, AmazonWebServiceInterface awsInterface, TaskLogger logger, TaskFunctionSpecifier rootSpecifier)
        {
            InitializeComponent();


            this.taskFunctionLookup = new Dictionary<string, TaskFunctionSpecifier>(taskFunctions.Select(tfs => new KeyValuePair<string, TaskFunctionSpecifier>(tfs.TaskFullPath, tfs)));
            this.taskQueue = new Queue<TaskFunctionSpecifier>();

            if (awsInterface is null) throw new ArgumentNullException(nameof(awsInterface));
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            this.awsInterface = awsInterface;
            this.taskLogger = logger;
            this.notificationsList = new NotificationsList();

            this.TaskFunction = AWSTasks.MinimalMethod;
            this.TaskSpecifier = rootSpecifier;
            this.taskLogger.StartLogging(this.TaskSpecifier);

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new StandardKnownArguments(notificationsList));
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            this.taskResponses = new ObservableCollection<TaskResponse>();

            //this.RootTaskName = "MinimalMethod";

            agent = new Agent(knownArguments, false);

            agent.NewUIResponse += Agent_NewUIResponse;
            agent.SaveEvents += Agent_SaveEvents;
            agent.CallTask += Agent_CallTask;
            agent.ConvertToJson += Agent_ConvertToJson;
            agent.ConvertToBinary += Agent_ConvertToBinary;
            agent.Error += Agent_Error;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ShowGlobalMessage("Parameter set", $"Task name set to {TaskSpecifier.TaskName}");
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            taskLogger.Dispose();
        }

        private void ShowGlobalMessage(string message, string detail)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications?.ShowMessage(message, detail);
        }

        private void ShowGlobalError(Notification errorInfo)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications?.Add(errorInfo);
        }

        /// <summary>
        /// Serialize all events (or just all SetArgument events) on the event stack for later restore
        /// </summary>
        private void SaveArgumentsAsJSON(TaskEvent[] events)
        {
            var taskFolderPath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, this.TaskSpecifier.TaskName);
            if (!Directory.Exists(taskFolderPath))
            {
                Directory.CreateDirectory(taskFolderPath);
            }

            var newData = Serialization.SerializeEventsAsJSON(events);

            // compare current version (if any)
            var serializedDataPath = Path.Combine(taskFolderPath, EventStackArgumentRestoreName);
            if (File.Exists(serializedDataPath))
            {
                var oldData = File.ReadAllBytes(serializedDataPath);

                bool unchanged = (oldData.Length == newData.Length) && (oldData.Zip(newData).All(item => item.First == item.Second));

                if (!unchanged)
                    File.WriteAllBytes(serializedDataPath, newData);
            }
            else
            {
                File.WriteAllBytes(serializedDataPath, newData);
            }
        }

        private async Task LogEventsAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var unloggedEvents = agent.SerializeUnloggedEventsAsBytes();

                var data = EventLoggingUtilities.BlockArrayToByteArray(unloggedEvents);
                this.taskLogger.AddToLog(data);
            });
        }

        private void UnLogEvents(string logFilePath)
        {
            var data = File.ReadAllBytes(logFilePath);
            var blocks = EventLoggingUtilities.ByteArrayToBlockArray(data);
            var loggedEvents = Serialization.DeserializeEventsFromBytes(blocks, ModuleResolver.ModuleLookup);
            agent.ContinueWith(loggedEvents);
        }

        private void StartTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;// !(TaskArguments is null) && TaskArguments.IsComplete();
        }

        private async void StartTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // attempt to restore event stack arguments from a previous session
                var taskFolderPath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, this.TaskSpecifier.TaskName);
                if (Directory.Exists(taskFolderPath))
                {
                    var serializedDataPath = Path.Combine(taskFolderPath, EventStackArgumentRestoreName);
                    if (File.Exists(serializedDataPath))
                    {
                        var options = new JsonDocumentOptions
                        {
                            AllowTrailingCommas = true
                        };

                        using var stream = File.OpenRead(serializedDataPath);
                        using JsonDocument document = JsonDocument.Parse(stream, options);
                        var taskEvents = Serialization.DeserializeEventsFromJSON(document, ModuleResolver.ModuleLookup);
                        agent.AddEvents(taskEvents);
                    }
                }
                else
                {
                    Directory.CreateDirectory(taskFolderPath);
                }

                lbTaskResponses.ItemsSource = taskResponses;

                async Task StartNew()
                {
                    agent.SetWorkingDirectory(new DirectoryInfo(taskFolderPath));

                    // set a default task identifier
                    agent.SetTaskIdentifier(Guid.NewGuid().ToString());

                    // set the save flags
                    var saveFlags = new SaveFlags(new ISaveFlagSet[]
                    {
                    new StandardFlagSet(new StandardFlagItem[]
                    {
                        StandardFlagItem.SaveTaskName
                    }),
                    new AWSFlagSet(new AWSFlagItem[]
                    {
                        AWSFlagItem.TranscribeSaveJSONTranscript,
                        AWSFlagItem.TranscribeSaveDefaultTranscript,
                        AWSFlagItem.TranslateSaveTranslation
                    })
                    });
                    agent.SetSaveFlags(saveFlags);

                    await RunTask().ConfigureAwait(false);
                }

                if (this.taskLogger.IsLoggingEnabled)
                {
                    //var logFilePath = Path.Combine(taskFolderPath, "637354693450070938-log.bin");
                    //var logFilePath = Path.Combine(taskFolderPath, "637354676138930293-log.bin");
                    var logFilePath = Path.Combine(taskFolderPath, "637354733302481717-log.bin");
                    if (File.Exists(logFilePath))
                    {
                        UnLogEvents(logFilePath);

                        // check the queue for new task function specifiers
                        await CheckQueue().ConfigureAwait(false);
                    }
                    else
                    {
                        await StartNew().ConfigureAwait(false);
                    }
                }
                else
                {
                    await StartNew().ConfigureAwait(false);
                }
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Mouse.OverrideCursor = null;
                });
            }
        }

        private async Task RunTask()
        {
            // generate an arguments stack (by default an infinite enumerable of Nothing arguments)
            var args = new InfiniteList<MaybeResponse>(MaybeResponse.Nothing);

            agent.PrepareFunctionArguments(args);

            notificationsList.Clear();      // cleared for each function invocation
            var responseStream = TaskFunction(args);

            var currentTask = new TaskItem(this.TaskSpecifier.ModuleName, this.TaskSpecifier.TaskName, string.Empty);
            await agent.RunTask(currentTask, responseStream).ConfigureAwait(false);

            if (this.taskLogger.IsLoggingEnabled)
            {
                // update the log file
                await LogEventsAsync().ConfigureAwait(false);
            }

            // Once the previous call to RunTask() has run to completion start the next task (if any)
            await CheckQueue().ConfigureAwait(false);
        }

        private async Task CheckQueue()
        {
            if (taskQueue.Count > 0)
            {
                var nextTaskSpecifier = taskQueue.Dequeue();
                await Dispatcher.InvokeAsync(async () =>
                {
                    this.TaskSpecifier = nextTaskSpecifier;
                    await RunTask().ConfigureAwait(false);
                });
            }
            else
            {
                // the task is either complete OR waiting on a response to be resolved via the UI (e.g. RequestArgument, TaskMultiSelect)
                // if complete then stop logging
                if (!agent.IsAwaitingResponse)
                {
                    this.taskLogger.StopLogging();
                }
            }
        }

        private async void Agent_NewUIResponse(object? sender, TaskResponse response)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                taskResponses.Add(response);
                lbTaskResponses.ScrollIntoView(response);
            });
        }

        private async void Agent_SaveEvents(object? sender, TaskEvent[] events)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // by the time this is invoked, the events stack may be in the process of being modified via new incoming responses
                // therefore pass a copy to iterate over
                SaveArgumentsAsJSON(events);
            });
        }

        private void Agent_CallTask(object? sender, TaskItem task)
        {
            // Callback from the current call to agent.RunTask()
            // Note that the current call must run to completion for the system to work correctly (ie the agent must run just one task function at a time)
            // Although calling Dispatcher InvokeAsync will wait until the call has finished, it will also allow multiple subtasks to run simultaneously
            // Instead, just enqueue the next task specifier and run the task later (see RunTask)
            var taskPath = TaskFunctionSpecifier.FullPathFromTaskItem(task);
            taskQueue.Enqueue(this.taskFunctionLookup[taskPath]);      // will throw if task path is unknown
        }

        private async void Agent_ConvertToBinary(object? sender, JsonDocument document)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var taskEvents = Serialization.DeserializeEventsFromJSON(document, ModuleResolver.ModuleLookup);
                var blocks = Serialization.SerializeEventsAsBytes(taskEvents, 0);
                var data = EventLoggingUtilities.BlockArrayToByteArray(blocks);
                agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetLogFormatEvents(data))));
            });
        }

        private async void Agent_ConvertToJson(object? sender, byte[] data)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var blocks = EventLoggingUtilities.ByteArrayToBlockArray(data);
                var taskEvents = Serialization.DeserializeEventsFromBytes(blocks, ModuleResolver.ModuleLookup);
                var serializedData = Serialization.SerializeEventsAsJSON(taskEvents);
                agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetJsonEvents(serializedData))));
            });
        }

        private async void Agent_Error(object? sender, Notification errorInfo)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ShowGlobalError(errorInfo);
            });
        }

        private void StartMiniTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void StartMiniTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            static T? CastAsInterface<T>(MiniTasks.MiniTaskContext? context) where T : class
            {
                return context switch
                {
                    MiniTasks.MiniTaskContext bucketItemViewModel => bucketItemViewModel.Item as T,
                    _ => null
                };
            }

            /// Add to local notifications list (and spill-over any additional notifications to the global notifications list)
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

            static MiniTasks.MiniTaskContext? GetContext(object eventSource)
            {
                static MiniTasks.MiniTaskContext? GetResponseContext(TaskResponse? response)
                {
                    var showValue = response switch
                    {
                        TaskResponse.ShowValue argResponse => argResponse.Item,
                        _ => throw new ArgumentException($"Unexpected TaskResponse {response}; expected a ShowValue IShowValue response")
                    };

                    if (showValue is AWSShowIntraModule)
                    {
                        var awsShowIntraModule = showValue as AWSShowIntraModule;
                        return awsShowIntraModule?.Argument switch
                        {
                            AWSDisplayValue.DisplayBucketItemsModel bucketItemsModel => MiniTasks.MiniTaskContext.NewBucketItemsModel(bucketItemsModel.Item),
                            _ => null
                        };
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected TaskResponse {response}; expected a ShowValue AWSShowIntraModule response");
                    }
                }
                return eventSource switch
                {
                    S3ItemManagement itemManagement => GetResponseContext(itemManagement.DataContext as TaskResponse),
                    S3BucketSelector bucketSelector => GetResponseContext(bucketSelector.DataContext as TaskResponse),
                    _ => throw new ArgumentException($"StartMiniTask: Unknown data context")
                };
            }

            void RunDeleteMiniTask(MiniTasks.MiniTaskContext? dataContext, AWSMiniTasks.MiniTaskArguments parameterInfo)
            {
                // create a new notifications list for each operation
                var (notifications, success, key) = AWSMiniTasks.Delete(awsInterface, new NotificationsList(), dataContext, parameterInfo.TaskArguments.ToArray());
                if (success)
                {
                    // deletion from the remote source was successful; now delete from the local view model
                    var deleteItemInterface = CastAsInterface<IDeletableViewModelItem>(dataContext);
                    deleteItemInterface?.DeleteItem(key);
                }

                // add to notifications
                var notifiableInterface = CastAsInterface<INotifiableViewModel<Notification>>(dataContext);
                var applicationNotifications = this.FindResource("applicationNotifications") as NotificationsList;
                RouteNotifications(notifications, applicationNotifications, notifiableInterface);
            }

            void RunDownloadMiniTask(MiniTasks.MiniTaskContext? dataContext, AWSMiniTasks.MiniTaskArguments parameterInfo)
            {
                var (notifications, success, _, _) = AWSMiniTasks.Download(awsInterface, new NotificationsList(), dataContext, parameterInfo.TaskArguments.ToArray());
                if (success)
                {
                    // add to notifications
                    var notifiableInterface = CastAsInterface<INotifiableViewModel<Notification>>(dataContext);
                    var applicationNotifications = this.FindResource("applicationNotifications") as NotificationsList;
                    RouteNotifications(notifications, applicationNotifications, notifiableInterface);
                }
            }

            var parameterInfo = e.Parameter as AWSMiniTasks.MiniTaskArguments;

            switch (parameterInfo?.Mode.Tag)
            {
                case MiniTasks.MiniTaskMode.Tags.Delete:
                    RunDeleteMiniTask(GetContext(e.OriginalSource), parameterInfo);
                    break;
                case MiniTasks.MiniTaskMode.Tags.Download:
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
            async Task RunUIResponseSelectAsync(UITaskArguments parameterInfo)
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
                    case RequestTranscriptionDefaultTranscript ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestVocabularyName ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestTranslationTargetLanguages ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case RequestTranslationTerminologyNames ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                }

                // the user has selected an item that sets an argument
                // first check if the task is complete
                if (agent.HasFunctionCompleted())
                {
                    // how many responses ago was the last TaskSelect?:
                    //  pare back a copy of the responses collection to the last TaskSelect
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

                        // clear and reinitialize the arguments on the events stack (common arguments) before adding SetArgument below
                        //var commonArgs = moduleLookup[TaskSpecifier.ModuleName];
                        agent.NewSelection(lastResponse);
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
                    case UITaskArgument.Bucket bucketArg:
                        var bucket = bucketArg.Item;
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetBucket(bucket))));
                        break;
                    //case UITaskArgument.FilePath filePathArg:
                    //    var filePath = filePathArg.Item;
                    //    agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetFilePath(filePath))));
                    //    break;
                    case UITaskArgument.FileMediaReference mediaReferenceArg:
                        var mediaReference = mediaReferenceArg.Item;
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetFileMediaReference(mediaReference))));
                        break;
                    case UITaskArgument.TranscriptionLanguageCode transcriptionLanguageCodeArg:
                        var transcriptionLanguageCode = transcriptionLanguageCodeArg.Item;
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionLanguageCode(transcriptionLanguageCode))));
                        break;
                    case UITaskArgument.TranscriptionDefaultTranscript transcriptionDefaultTranscriptArg:
                        var transcriptionDefaultTranscript = transcriptionDefaultTranscriptArg.Item;
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcriptionDefaultTranscript))));
                        break;
                    case UITaskArgument.TranslationLanguageCodeSource translationLanguageCodeArg:
                        var translationLanguageCode = translationLanguageCodeArg.Item;
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationLanguageCodeSource(translationLanguageCode))));
                        break;
                    case UITaskArgument.TranscriptionVocabularyName vocabularyNameArg:
                        var vocabularyName = vocabularyNameArg.Item;
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionVocabularyName(vocabularyName))));
                        break;
                    case UITaskArgument.TranslationTargetLanguages translationTargetLanguagesArg:
                        var translationTargetLanguages = new RetainingStack<LanguageCode>(translationTargetLanguagesArg.Item);
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(translationTargetLanguages))));
                        break;
                    case UITaskArgument.TranslationTerminologyNames translationTerminologyNamesArg:
                        var translationTerminologyNames = new List<string>(translationTerminologyNamesArg.Item);
                        agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(translationTerminologyNames))));
                        break;
                    default:
                        throw new ArgumentException($"RunSelectBucketMiniTask: Unknown argument type");
                }

                await Dispatcher.InvokeAsync(async () =>
                {
                    await RunTask().ConfigureAwait(false);
                });
            }

            async Task RunUIResponseContinueAsync()
            {
                // disable the Continue button and restart the task
                if (e.OriginalSource is TaskContinue ctrl) ctrl.IsButtonEnabled = false;
                await Dispatcher.InvokeAsync(async () =>
                {
                    await RunTask().ConfigureAwait(false);
                });
            }

            async Task RunUIResponseForEachIndependantTaskAsync(UITaskArguments parameterInfo)
            {
                // expecting a single argument (an IEnumerable<MultiSelectArgument>)
                var subtasks = parameterInfo.TaskArguments.First() switch
                {
                    UITaskArgument.ForEach args => new RetainingStack<TaskItem>(args.Item, RetainingStack<TaskItem>.ItemOrdering.Independant),
                    _ => throw new ArgumentException($"Unknown argument type")
                };

                // now that the user has made their selections, add those selections to the event source for later reference
                agent.AddEvent(TaskEvent.NewForEachTask(subtasks));

                // pop the first task item and set an argument on the event source
                if (subtasks.Count > 0)
                {
                    agent.StartNewTask(subtasks);       // this will invoke the callback that adds the next task to the queue
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        var nextTaskSpecifier = taskQueue.Dequeue();
                        this.TaskSpecifier = nextTaskSpecifier;

                        await RunTask().ConfigureAwait(false);
                    });
                }
            }

            var parameterInfo = e.Parameter as UITaskArguments;

            switch (parameterInfo?.Mode.Tag)
            {
                case UITaskMode.Tags.Select:
                    await RunUIResponseSelectAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.Tags.Continue:
                    await RunUIResponseContinueAsync().ConfigureAwait(false);
                    break;
                case UITaskMode.Tags.ForEachIndependantTask:
                    await RunUIResponseForEachIndependantTaskAsync(parameterInfo).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentException($"UIResponse_Executed: unknown mode for parameterInfo");
            }
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
