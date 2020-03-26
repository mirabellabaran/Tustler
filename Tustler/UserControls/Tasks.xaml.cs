using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(Tasks), new PropertyMetadata("", PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var ctrl = dependencyObject as Tasks;
            if (ctrl != null)
            {
                //if (dependencyPropertyChangedEventArgs.NewValue != null)
                //    ctrl.ReportViewerLoad(dependencyPropertyChangedEventArgs.NewValue.ToString());
            }
        }

        public string ScriptName
        {
            get { return (string) GetValue(ScriptNameProperty); }
            set { SetValue(ScriptNameProperty, value); }
        }

        public Tasks()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.ShowMessage("Parameter set", $"Script name set to {ScriptName}");

            var task = AWSTasks.S3FetchItems();
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

        private async void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems as System.Collections.IEnumerable;
            foreach (var response in newItems.Cast<TaskResponse>())
            {
                switch (response)
                {
                    case TaskResponse.TaskNotification note:
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
                    case TaskResponse.TaskBucket taskBucket:
                        await AddControlAsync(taskBucket.Item.Name, false).ConfigureAwait(false);
                        break;
                    case TaskResponse.TaskBucketItem taskBucketItem:
                        await AddControlAsync(taskBucketItem.Item.Key, false).ConfigureAwait(false);
                        break;
                }
            }
        }
    }
}
