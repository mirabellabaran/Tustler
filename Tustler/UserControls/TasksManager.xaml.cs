using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Tustler.Models;
using TustlerFSharpPlatform;
using TustlerModels;
using TustlerServicesLib;
using static TustlerFSharpPlatform.TaskArguments;
using AWSTasks = TustlerFSharpPlatform.Tasks;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for Tasks.xaml
    /// </summary>
    public partial class TasksManager : UserControl
    {
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
                        case "S3FetchItems":
                            ctrl.TaskArguments = new TaskArguments.NotificationsOnlyArguments(new NotificationsList());
                            ctrl.TaskFunction = AWSTasks.S3FetchItems;
                            break;
                        case "TranscribeAudio":
                            ctrl.TaskArguments = new TaskArguments.TranscribeAudioArguments(new NotificationsList());
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

        public TasksManager()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.ShowMessage("Parameter set", $"Task name set to {TaskName}");

            var requiredMembersOption = this.TaskArguments.GetRequiredMembers();
            if (requiredMembersOption.IsRequired)
            {
                // add the required number of rows
                var numMembers = requiredMembersOption.Members.Length;
                var numRows = (numMembers / 2) + 1;
                grdMembers.RowDefinitions.Clear();
                for (int i = 0; i < numRows; i++)
                {
                    grdMembers.RowDefinitions.Add(new RowDefinition());
                }

                for (var i = 0; i < numMembers; i++)
                {
                    var requiredMemberControlTag = requiredMembersOption.Members[i];
                    var rowNum = i / 2;
                    var colNum = (i % 2) == 0 ? 0 : 1;

                    // instantiate the user control and add to grid container in read-order
                    UserControl uc;
                    switch (requiredMemberControlTag)
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
                            var mediaReferenceCtrl = new TaskMemberControls.MediaReference
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
                            var vocabularyNameCtrl = new TaskMemberControls.VocabularyName
                            {
                                Command = TaskCommands.UpdateTaskArguments
                            };
                            uc = vocabularyNameCtrl;
                            break;
                        default:
                            throw new ArgumentException("Unknown Task Member Control tag.");
                    }

                    Grid.SetRow(uc, rowNum);
                    Grid.SetColumn(uc, colNum);
                    grdMembers.Children.Add(uc);
                }
            }
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
                                await AddResponseAsync(errorMsg, true).ConfigureAwait(false);
                                break;
                            case ApplicationMessageInfo msg:
                                var infoMsg = $"{msg.Message}: {msg.Detail}";
                                await AddResponseAsync(infoMsg, true).ConfigureAwait(false);
                                break;
                        }
                        break;
                    case TaskResponse.Bucket taskBucket:
                        await AddResponseAsync(taskBucket.Item.Name, false).ConfigureAwait(false);
                        break;
                    case TaskResponse.BucketItem taskBucketItem:
                        await AddResponseAsync(taskBucketItem.Item.Key, false).ConfigureAwait(false);
                        break;
                }
            }
        }

        private void StartTask_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(TaskArguments is null) && TaskArguments.IsComplete();
        }

        private async void StartTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await RunTaskAsync().ConfigureAwait(true);
        }

        private void UpdateTaskArguments_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void UpdateTaskArguments_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var data = e.Parameter as TaskArgumentMember;
            if (!(data is null))
                this.TaskArguments.SetValue(data);
        }

        private async Task RunTaskAsync()
        {
            //"TranscribeAudio1",
            //new MediaReference("tator", "TranscribeAudio1", "audio/wav", "wav"),
            //@"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\SallyRide2.wav",
            //"en-US", "Test")

            if (TaskArguments.IsComplete())
            {
                var responseStream = TaskFunction(TaskArguments);
                var collection = new ObservableCollection<TaskResponse>();
                collection.CollectionChanged += Collection_CollectionChanged;

                await TaskQueue.Run(responseStream, collection).ConfigureAwait(true);
            }
        }

        private async Task AddResponseAsync(string content, bool showBold)
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

        public static readonly RoutedUICommand UpdateTaskArguments = new RoutedUICommand
            (
                "UpdateTaskArguments",
                "UpdateTaskArguments",
                typeof(TaskCommands),
                null
            );
    }

}
