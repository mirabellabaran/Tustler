﻿#nullable enable

using CloudWeaver;
using CloudWeaver.AWS;
using CloudWeaver.Foundation.Types;
using CloudWeaver.MediaServices;
using CloudWeaver.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
using TustlerFFMPEG;
using TustlerInterfaces;
using TustlerModels;
using TustlerServicesLib;
using TustlerUIShared;
using AWSMiniTasks = CloudWeaver.AWS.MiniTasks;
//using AWSTasks = CloudWeaver.AWS.Tasks;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Tasks.xaml
    /// </summary>
    public partial class TasksManager : UserControl, IOwnerType
    {
        private enum RunMode
        {
            Init,
            Running,
            Stopped
        }

        private const string EventStackArgumentRestoreName = "defaultargs.json";

        private RunMode runMode;

        private readonly TaskFunctionResolver taskFunctionResolver;

        private readonly AmazonWebServiceInterface awsInterface;

        private readonly Agent agent;                                           // the executor that manages Task Function execution
        private readonly ObservableCollection<IResponseWrapper> taskResponses;  // the sequence of UI responses generated by a call to a Task Function; bound to the UI

        public static readonly DependencyProperty RootTaskSpecifierProperty = DependencyProperty.Register("RootTaskSpecifier", typeof(TaskFunctionSpecifier), typeof(TasksManager));

        public TaskFunctionSpecifier RootTaskSpecifier
        {
            get { return (TaskFunctionSpecifier)GetValue(RootTaskSpecifierProperty); }
            set { SetValue(RootTaskSpecifierProperty, value); }
        }

        // IOwnerType
        public IEnumerable<TaskFunctionSpecifier> TaskFunctions
        {
            get
            {
                return agent.TaskFunctions;
            }
        }

        public TasksManager(AmazonWebServiceInterface awsInterface, FFMPEGServiceInterface avInterface, TaskFunctionSpecifier rootSpecifier)
        {
            InitializeComponent();

            if (awsInterface is null) throw new ArgumentNullException(nameof(awsInterface));
            if (avInterface is null) throw new ArgumentNullException(nameof(avInterface));
            if (rootSpecifier is null) throw new ArgumentNullException(nameof(rootSpecifier));

            this.awsInterface = awsInterface;

            var app = Application.Current as App;
            var serviceProvider = app!.ServiceProvider;
            this.taskFunctionResolver = serviceProvider.GetRequiredService<TaskFunctionResolver>();
            var logger = serviceProvider.GetRequiredService<TaskLogger>();

            this.RootTaskSpecifier = rootSpecifier;

            this.taskResponses = new ObservableCollection<IResponseWrapper>();

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));
            knownArguments.AddModule(new AVKnownArguments(avInterface));

            agent = new Agent(knownArguments, taskFunctionResolver, logger, false);

            agent.TaskComplete += Agent_TaskComplete;
            agent.NewUIResponse += Agent_NewUIResponse;
            agent.SaveEvents += Agent_SaveEvents;
            agent.ConvertToJson += Agent_ConvertToJson;
            agent.ConvertToBinary += Agent_ConvertToBinary;
            agent.Error += Agent_Error;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ShowGlobalMessage("Parameter set", $"Task name set to {RootTaskSpecifier.TaskName}");

            rbRunLog.IsEnabled = this.RootTaskSpecifier.IsLoggingEnabled;
        }

        private void LogFiles_DropDownOpened(object sender, EventArgs e)
        {
            if (lbLogFiles.Items.Count == 0)
            {
                // check for log files
                var loggedTaskName = this.RootTaskSpecifier.TaskName;
                var taskFolderPath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, loggedTaskName);
                if (Directory.Exists(taskFolderPath))
                {
                    static string CreateDescription(string filePath)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(new ReadOnlySpan<char>(filePath.ToCharArray()));
                        var endIndex = fileName.LastIndexOf('-');
                        if ((endIndex > 0) && (long.TryParse(fileName.Slice(0, endIndex), out long ticks)))
                        {
                            var dt = new DateTime(ticks);
                            return dt.ToString("dddd, MMM dd hh:mm", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            return fileName.ToString();
                        }
                    }

                    var filePaths = Directory.EnumerateFiles(taskFolderPath, "*-log.bin", SearchOption.TopDirectoryOnly);
                    var listData = filePaths.Select(filePath => new LogFile { FilePath = filePath, TaskName = loggedTaskName, Description = CreateDescription(filePath) });
                    lbLogFiles.ItemsSource = listData;
                }
                else
                {
                    lbLogFiles.Items.Clear();
                }
            }
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
            var taskFolderPath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, this.RootTaskSpecifier.TaskName);
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

        private void UnLogEvents(string logFilePath)
        {
            var data = File.ReadAllBytes(logFilePath);
            var blocks = EventLoggingUtilities.ByteArrayToBlockArray(data);
            var loggedEvents = Serialization.DeserializeEventsFromBytes(blocks);
            agent.ContinueWith(loggedEvents);
        }

        private void StartTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var isNewRunMode = rbNewTaskRun.IsChecked.HasValue && rbNewTaskRun.IsChecked.Value;
            var isRunLogMode = (rbRunLog.IsChecked.HasValue && rbRunLog.IsChecked.Value) && (lbLogFiles.SelectedItem is object);
            e.CanExecute = (isNewRunMode || isRunLogMode);
        }

        private static TaskEvent[] ReadDefaultArguments(string taskFolderPath)
        {
            TaskEvent[] taskEvents;

            // attempt to restore event stack arguments from a previous session
            var serializedDataPath = Path.Combine(taskFolderPath, EventStackArgumentRestoreName);
            if (File.Exists(serializedDataPath))
            {
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true
                };

                using var stream = File.OpenRead(serializedDataPath);
                using JsonDocument document = JsonDocument.Parse(stream, options);
                taskEvents = Serialization.DeserializeEventsFromJSON(document);
            }
            else
            {
                taskEvents = Array.Empty<TaskEvent>();
            }

            return taskEvents;
        }

        private void SetStandardVariables(string taskFolderPath)
        {
            // these next three arguments are standard internally resolvable arguments

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
        }

        private bool PrepareTaskFirstRun(string taskFolderPath)
        {
            var runImmediately = true;

            if (Directory.Exists(taskFolderPath))
            {
                var taskEvents = ReadDefaultArguments(taskFolderPath);
                if (taskEvents.Length > 0)
                {
                    // create a list of descriptions for each SetArgument
                    List<string> descriptions = new List<string>(taskEvents.Length);
                    foreach (var evt in taskEvents)
                    {
                        if (evt is TaskEvent.SetArgument arg)
                        {
                            if (arg.Item is TaskResponse.SetArgument response)
                            {
                                descriptions.Add(response.Item.Description());
                            }
                        }
                    }

                    // wait on user selection of defaults
                    runImmediately = false;
                    taskResponses.Add(new DescriptionWrapper(this, descriptions));
                }
            }
            else
            {
                Directory.CreateDirectory(taskFolderPath);
            }

            SetStandardVariables(taskFolderPath);

            // set the root task
            agent.PushRootTask(TustlerServicesLib.ApplicationSettings.FileCachePath, this.RootTaskSpecifier);

            return runImmediately;
        }

        async Task StartNewAsync(string taskFolderPath)
        {
            var runImmediately = PrepareTaskFirstRun(taskFolderPath);

            if (runImmediately)
            {
                await agent.RunNext().ConfigureAwait(false);
            }
        }

        private async Task FirstRun()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var taskFolderPath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, this.RootTaskSpecifier.TaskName);

                lbTaskResponses.ItemsSource = taskResponses;

                var logMode = (rbRunLog.IsChecked.HasValue && rbRunLog.IsChecked.Value) && (lbLogFiles.SelectedItem is object);
                if (logMode)
                {
                    if (lbLogFiles.SelectedItem is LogFile selectedLogFile && File.Exists(selectedLogFile.FilePath))
                    {
                        // MG TODO diagnose 12 Nov 3:13pm log file
                        UnLogEvents(selectedLogFile.FilePath);
                        SetStandardVariables(taskFolderPath);

                        // check the queue for new task function specifiers
                        await agent.RunNext().ConfigureAwait(false);
                    }
                    else
                    {
                        await StartNewAsync(taskFolderPath).ConfigureAwait(false);
                    }
                }
                else
                {
                    await StartNewAsync(taskFolderPath).ConfigureAwait(false);
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

        private async void StartTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // In Stopped mode, a new Agent instance is created and that last log file is run (assuming logging is enabled)

            switch (runMode)
            {
                case RunMode.Init:
                    // Start the task
                    btnStartTask.Content = "Stop";
                    runMode = RunMode.Running;
                    await FirstRun().ConfigureAwait(false);
                    break;
                case RunMode.Running:
                    // Stop the task
                    runMode = RunMode.Stopped;
                    agent.Enabled = false;
                    btnStartTask.Content = "Restart Task";
                    if (agent.IsLoggingEnabled)
                    {
                        // log any unlogged events and close the log file in preparation for reopening
                        agent.CloseLog();
                    }
                    //else
                    //{
                    //    btnStartTask.IsEnabled = false;         // no log file to restart
                    //}
                    break;
                case RunMode.Stopped:
                    // Restart the task
                    btnStartTask.Content = "Stop";
                    runMode = RunMode.Running;
                    agent.RestartLogging();
                    agent.Enabled = true;

                    // if not waiting on user selection of default arguments
                    if (!(taskResponses.Last() is DescriptionWrapper))
                    {
                        if (agent.WaitingOnResponse)
                        {
                            // the last task response requires user input to proceed (e.g. a request for argument or prompt for continuation)
                            var index = taskResponses.Count - 1;
                            if (taskResponses[index] is ResponseWrapper responseWrapper)
                            {
                                // remove last task response and reapply
                                var lastResponse = responseWrapper.TaskResponse;
                                taskResponses.RemoveAt(index);

                                Agent_NewUIResponse(this, lastResponse);
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }
                        }
                        else
                        {
                            // not waiting on a response or on default arguments
                            // therefore just continue
                            await agent.RunCurrent().ConfigureAwait(false);
                        }
                    }
                    break;
            }
        }

        private async void Agent_TaskComplete(object? sender, EventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                btnStartTask.Content = "Run Task";
                btnStartTask.IsEnabled = false;
            });
        }

        private async void Agent_NewUIResponse(object? sender, TaskResponse response)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var wrapper = new ResponseWrapper(this, response);
                taskResponses.Add(wrapper);
                lbTaskResponses.ScrollIntoView(wrapper);
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

        //private void Agent_CallTask(object? sender, TaskItem task)
        //{
        //    // Callback from the current call to agent.RunTask()
        //    // Note that the current call must run to completion for the system to work correctly (ie the agent must run just one task function at a time)
        //    // Although calling Dispatcher InvokeAsync will wait until the call has finished, it will also allow multiple subtasks to run simultaneously
        //    // Instead, just enqueue the next task specifier and run the task later (see RunTask)
        //    taskQueue.Enqueue(this.taskFunctionLookup[task.FullPath]);      // will throw if task path is unknown
        //}

        private async void Agent_ConvertToBinary(object? sender, JsonDocument document)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var taskEvents = Serialization.DeserializeEventsFromJSON(document);
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
                var taskEvents = Serialization.DeserializeEventsFromBytes(blocks);
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

        //private static TaskItem CreateTaskFromTaskFunctionSpecifier(TaskFunctionSpecifier taskFunctionSpecifier)
        //{
        //    return new TaskItem(taskFunctionSpecifier.ModuleName, taskFunctionSpecifier.TaskName, string.Empty);
        //}

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
                static MiniTasks.MiniTaskContext? GetResponseContext(ResponseWrapper? wrapper)
                {
                    if (wrapper is object)
                    {
                        var response = wrapper.TaskResponse;
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
                    else
                    {
                        throw new ArgumentException("Response wrapper is null; check the data context of this mini task.");
                    }
                }
                return eventSource switch
                {
                    S3ItemManagement itemManagement => GetResponseContext(itemManagement.DataContext as ResponseWrapper),
                    S3BucketSelector bucketSelector => GetResponseContext(bucketSelector.DataContext as ResponseWrapper),
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
            async Task RunUIResponseSelectTasksAsync(UITaskArguments parameterInfo)
            {
                if (e.OriginalSource is ChooseTask ctrl)
                {
                    ctrl.IsButtonEnabled = false;
                }

                var tasks = JsonSerializer.Deserialize<IEnumerable<TaskItem>>(new ReadOnlySpan<byte>(parameterInfo.SerializedArgument));
                agent.PushTasks(tasks, ItemOrdering.Sequential);

                await agent.RunNext().ConfigureAwait(false);
            }

            async Task RunUIResponseSelectDefaultArgumentsAsync(UITaskArguments parameterInfo)
            {
                if (e.OriginalSource is SelectDefaultArguments ctrl)
                {
                    ctrl.IsButtonEnabled = false;
                }

                var selectedArguments = JsonSerializer.Deserialize<IEnumerable<bool>>(new ReadOnlySpan<byte>(parameterInfo.SerializedArgument)).ToArray();

                // re-read the default arguments list and add only the user-selected items to the agent
                var taskFolderPath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, this.RootTaskSpecifier.TaskName);
                var taskEvents = ReadDefaultArguments(taskFolderPath);

                if (taskEvents.Length > 0)
                {
                    if (selectedArguments.Length != taskEvents.Length)
                    {
                        throw new InvalidOperationException($"Default arguments processing: User selection ({selectedArguments.Length} items) does not match length of events list ({taskEvents.Length} items)");
                    }

                    var selectedEvents = taskEvents.Zip(selectedArguments).Where(item => item.Second).Select(item => item.First);
                    agent.AddEvents(selectedEvents);
                }

                await agent.RunNext().ConfigureAwait(false);
            }

            /// A UI component has been selected that restarts an already completed task e.g. S3FetchItems
            async Task RunUIResponseRestartTaskAsync(UITaskArguments parameterInfo)
            {
                // the user has selected an item that sets an argument
                // first check if the task is complete
                var hasCompleted = agent.HasFunctionCompleted();

                if (hasCompleted)
                {
                    // how many responses ago was the last TaskSelect?:
                    //  pare back a copy of the responses collection to the last TaskSelect
                    var tempStack = new Stack<TaskResponse>(taskResponses.Select(wrapper =>
                        wrapper switch
                        {
                            ResponseWrapper rw => rw.TaskResponse,
                            DescriptionWrapper dw => TaskResponse.NewTaskInfo("Description wrapper placeholder"),
                            _ => throw new ArgumentException("Unknown IResponseWrapper type", nameof(wrapper))
                        }));
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
                        agent.NewSelection(lastResponse);
                    }

                    // clearing the ObservableCollection will disconnect the data bindings (the DataContext on the ItemsControl item containers)
                    // instead, just remove the last numItems items
                    var numItems = taskResponses.Count - tempStack.Count;
                    for (int i = 0; i < numItems; i++)
                    {
                        taskResponses.RemoveAt(taskResponses.Count - 1);
                    }

                    // reset the current task (pushes the current task on the queue)
                    agent.PushTask(this.RootTaskSpecifier);
                }

                // Add a SetArgument event to the events list and reinvoke the function
                agent.AddArgument(parameterInfo.ModuleName, parameterInfo.PropertyName, parameterInfo.SerializedArgument);

                if (hasCompleted)
                {
                    // current task has been reset (pushes the current task on the queue)
                    //await CheckQueue().ConfigureAwait(false);
                    await agent.RunNext().ConfigureAwait(false);
                }
                else
                {
                    // current task not yet complete
                    await agent.RunCurrent().ConfigureAwait(false);
                }
            }

            /// The user has selected a task function suggested by the default response handler
            async Task RunUIResponseInsertTaskAsync(UITaskArguments parameterInfo)
            {
                var selectedTaskPath = JsonSerializer.Deserialize<string>(new ReadOnlySpan<byte>(parameterInfo.SerializedArgument));

                agent.InsertTaskBeforeCurrent(selectedTaskPath);
                await agent.RunNext().ConfigureAwait(false);
            }

            /// The argument needs transforming before setting an argument on the agent
            async Task RunUIResponseTransformArgumentAsync(UITaskArguments parameterInfo)
            {
                var bucketItem = JsonSerializer.Deserialize<BucketItem>(new ReadOnlySpan<byte>(parameterInfo.SerializedArgument));

                var s3URI = $"https://s3.ap-southeast-2.amazonaws.com/{bucketItem.BucketName}/{bucketItem.Key}";
                var data = JsonSerializer.SerializeToUtf8Bytes<string>(s3URI);

                var parameters = new UITaskArguments(UITaskMode.SetArgument, parameterInfo.ModuleName, "SetTranscriptURI", data);
                await RunUIResponseSetArgumentAsync(parameters).ConfigureAwait(false);
            }

            /// Set an argument on the agent
            async Task RunUIResponseSetArgumentAsync(UITaskArguments parameterInfo)
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
                    case RequestS3BucketItem ctrl:
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
                    case RequestFilePath ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                    case DefaultResponseHandler ctrl:
                        ctrl.IsButtonEnabled = false;
                        break;
                }

                // Add a SetArgument event to the events list and reinvoke the function
                agent.AddArgument(parameterInfo.ModuleName, parameterInfo.PropertyName, parameterInfo.SerializedArgument);

                await agent.RunCurrent().ConfigureAwait(false);
            }

            async Task RunUIResponseContinueAsync()
            {
                // disable the Continue button and restart the task
                if (e.OriginalSource is TaskContinue ctrl) ctrl.IsButtonEnabled = false;
                await agent.RunCurrent().ConfigureAwait(false);
            }

            async Task RunUIResponseForEachIndependantTaskAsync(UITaskArguments parameterInfo)
            {
                // expecting an IEnumerable<TaskItem>
                var subtasks = JsonSerializer.Deserialize< IEnumerable<TaskItem>>(new ReadOnlySpan<byte>(parameterInfo.SerializedArgument));

                // now that the user has made their selections, push the new tasks on the execution stack
                // this attempts to pop the first task item (if any) and invoke the callback that adds the next task to the queue
                if (agent.Enabled)
                {
                    agent.PushTasks(subtasks, ItemOrdering.Independant);
                    await agent.RunNext().ConfigureAwait(false);
                }
            }

            var parameterInfo = e.Parameter as UITaskArguments;

            switch (parameterInfo?.TaskMode)
            {
                case UITaskMode.SelectTask:
                    await RunUIResponseSelectTasksAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.RestartTask:
                    await RunUIResponseRestartTaskAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.InsertTask:
                    await RunUIResponseInsertTaskAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.SetArgument:
                    await RunUIResponseSetArgumentAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.TransformSetArgument:
                    await RunUIResponseTransformArgumentAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.SelectDefaultArguments:
                    await RunUIResponseSelectDefaultArgumentsAsync(parameterInfo).ConfigureAwait(false);
                    break;
                case UITaskMode.Continue:
                    await RunUIResponseContinueAsync().ConfigureAwait(false);
                    break;
                case UITaskMode.ForEachIndependantTask:
                    await RunUIResponseForEachIndependantTaskAsync(parameterInfo).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentException($"UIResponse_Executed: unknown mode for parameterInfo");
            }
        }

        private void SelectRunMode_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SelectRunMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var radioButton = e.OriginalSource as RadioButton;

            lbLogFiles.IsEnabled = (radioButton!.Name == "rbRunLog");
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

        public static readonly RoutedUICommand SelectRunMode = new RoutedUICommand
            (
                "SelectRunMode",
                "SelectRunMode",
                typeof(TaskCommands),
                null
            );
    }
}
