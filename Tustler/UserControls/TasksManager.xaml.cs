﻿using System;
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
        private readonly ObservableCollection<TaskResponse> taskResponses;

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

        public Func<ITaskArgumentCollection, IEnumerable<TaskResponse>> TaskFunction
        {
            get;
            internal set;
        }

        public TasksManager(AmazonWebServiceInterface awsInterface)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
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
            foreach (var item in newItems.Cast<TaskResponse>())
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    taskResponses.Add(item);
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
                var responseStream = TaskFunction(TaskArguments);
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
            static MiniTaskParameter? GenerateParameter(object? model, string typeName, string itemName)
            {
                var modelType = model?.GetType();
                var itemType = Type.GetType(typeName);

                MiniTaskParameter? result = null;

                if (modelType is object && itemType is object)
                {
                    var propertyInfo = modelType.GetProperty(itemName, itemType);
                    var value = propertyInfo?.GetValue(model);

                    if (value is object)
                    {
                        result = itemType?.FullName switch
                        {
                            "System.Boolean" => MiniTaskParameter.NewBool((bool)value),
                            "System.Int" => MiniTaskParameter.NewInt((int)value),
                            "System.String" => MiniTaskParameter.NewString((string)value),
                            _ => null
                        };
                    }
                }

                return result;
            }
#nullable disable

            // transform each TaskManagerCommandParameterArgument into a MiniTaskArgument
            MiniTaskParameter[] GenerateParameterList(TaskManagerCommandParameterValue parameterValue)
            {
                var boundResponse = (e.OriginalSource as Button).DataContext as TaskResponse;
                var model = boundResponse switch
                {
                    TaskResponse.BucketItem taskBucketItem => taskBucketItem.Item as object,
                    _ => null
                };

                return parameterValue.Items.Cast<TaskManagerCommandParameterArgument>()
                    .Select(arg => GenerateParameter(model, arg.ItemType, arg.ItemKey))
                    .ToArray();
            }

            var taskUpdate = (e.Parameter as TaskManagerCommandParameterValue) switch
            {
                TaskManagerCommandParameterValue parameterValue when parameterValue.ParameterType == "Delete" => AWSMiniTasks.Delete(awsInterface, new NotificationsList(), GenerateParameterList(parameterValue)),
                _ => null
            };

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
