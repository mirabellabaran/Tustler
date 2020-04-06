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
                    UserControl uc = requiredMemberControlTag switch
                    {
                        "taskName" => new TaskMemberControls.TaskName(),
                        "mediaRef" => new TaskMemberControls.MediaReference(),
                        "filePath" => new TaskMemberControls.FilePath(),
                        "languageCode" => new TaskMemberControls.LanguageCode(),
                        "vocabularyName" => new TaskMemberControls.VocabularyName(),
                        _ => throw new ArgumentException("Unknown Task Member Control tag.")
                    };

                    if (uc is TaskMemberControls.FilePath)
                        (uc as TaskMemberControls.FilePath).Command = TaskCommands.UpdateTaskArguments;
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

        private void UpdateTaskArguments_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void UpdateTaskArguments_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var data = e.Parameter as TaskArgumentMember;
            this.TaskArguments.SetValue(data);
        }

        private async Task RunTaskAsync()
        {
            //"TranscribeAudio1",
            //new MediaReference("tator", "TranscribeAudio1", "audio/wav", "wav"),
            //@"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\SallyRide2.wav",
            //"en-US", "Test")

            var responseStream = this.TaskFunction(this.TaskArguments);
            var collection = new ObservableCollection<TaskResponse>();
            collection.CollectionChanged += Collection_CollectionChanged;

            await TaskQueue.Run(responseStream, collection).ConfigureAwait(true);
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

        public static readonly RoutedUICommand UpdateTaskArguments = new RoutedUICommand
            (
                "UpdateTaskArguments",
                "UpdateTaskArguments",
                typeof(TaskCommands),
                null
            );
    }

}
