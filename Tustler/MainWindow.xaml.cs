using CloudWeaver.AWS;
using CloudWeaver.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Tustler.Helpers;
using Tustler.Models;
using Tustler.UserControls;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;
using AppSettings = TustlerServicesLib.ApplicationSettings;
using AppSettingsControl = Tustler.UserControls.ApplicationSettings;

namespace Tustler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string TaskFunctionModulePrefix = "CloudWeaver*.dll";     // the name prefix of assemblies which can be searched for Task Function modules
        private (TaskFunctionSpecifier specifier, bool hideFromUI)[] taskFunctions;     // all known task functions (with the HideFromUI attribute status)

        private readonly AmazonWebServiceInterface awsInterface;
        private readonly TaskLogger taskLogger;

        private bool isCollapsed;  // true if the notifications area is in a collapsed state

        public static readonly DependencyProperty IsMockedProperty = DependencyProperty.Register("IsMocked", typeof(bool), typeof(MainWindow), new PropertyMetadata(false, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is MainWindow ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var isMocked = (bool)dependencyPropertyChangedEventArgs.NewValue;
                    ctrl.bdrStatusBarIsMocked.Background = isMocked ? Brushes.Red : Brushes.Transparent;
                    ctrl.tbStatusBarIsMocked.Text = isMocked ? $"Mocking mode enabled" : "Standard Mode";
                }
            }
        }

        public MainWindow(AmazonWebServiceInterface awsInterface, RuntimeOptions options, TaskLogger logger)
        {
            InitializeComponent();

            this.awsInterface = awsInterface;
            this.taskLogger = logger;

            this.taskFunctions = null;

            this.IsMocked = (options is object) ? options.IsMocked : false;

            this.isCollapsed = false;
        }

        public bool IsMocked
        {
            get { return (bool)GetValue(IsMockedProperty); }
            set { SetValue(IsMockedProperty, value); }
        }

        #region Event Handlers

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // look for status changes in the notifications listbox so that it can scroll new items into view
            lbNotifications.ItemContainerGenerator.ItemsChanged += Notifications_ItemsChanged;

            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "S3 Management", Tag = new DefaultTag("s3management"), HasChildren = false }));
            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Settings", Tag = new DefaultTag("settings"), HasChildren = true }));

            var functionSubTree = CreateTreeItem(new TreeViewItemData { Name = "Individual Functions", Tag = new DefaultTag("functions"), HasChildren = true });
            tvActions.Items.Add(functionSubTree);
            await CreateSubTree(functionSubTree).ConfigureAwait(true);
            functionSubTree.IsExpanded = true;

            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Tasks", Tag = new DefaultTag("tasks"), HasChildren = true }));

            menuTasks.Items.Add(CreateMenuItem(new TreeViewItemData { Name = "Tasks", Tag = new DefaultTag("tasks"), HasChildren = true }));

            var credentials = TustlerAWSLib.Credentials.GetCredentials();
            if (credentials is null)
            {
                SwitchTo("credentials");    // show credentials editor
            }

            // TODO remove MG temporarily enable mocking by default
            ToggleMockingMode(true);

            // check if the FFmpeg library can be loaded
            try
            {
                Unosquare.FFME.Library.LoadFFmpeg();
            }
            catch (FileNotFoundException ex)
            {
                var notifications = this.FindResource("applicationNotifications") as NotificationsList;
                notifications.HandleError("Window_Loaded", "The setting for FFmpegDirectory is incorrect", ex);
                notifications.ShowMessage("FFmpeg shared binaries are required", "The FFmpeg libraries can be downloaded from: https://ffmpeg.zeranoe.com/builds/");

                SwitchTo("appSettings");
            }
        }

        private void Notifications_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var numItems = lbNotifications.Items.Count;
                if (numItems > 0)
                    lbNotifications.ScrollIntoView(lbNotifications.Items[numItems - 1]);
            }
        }

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AboutCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            About aboutDialog = new About
            {
                Owner = this
            };

            aboutDialog.ShowDialog();
        }

        private void SwitchCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool canExecute = false;

            if (tvActions.SelectedItem is TreeViewItem item)
            {
                if (panControlsContainer.Children.Count == 0)
                {
                    canExecute = true;
                }
                else
                {
                    // allows a switch if the current child user control is not the TasksManager or if switching to a new task function
                    // return true to allow a switch
                    static bool CheckTaskFunction(UIElement firstChild, IElementTag tag)
                    {
                        if (firstChild is TasksManager tasksManager)
                        {
                            var currentTask = tasksManager.TaskSpecifier.TaskName;
                            if (tag is TaskFunctionSpecifier specifier)
                            {
                                return currentTask != specifier.TaskName;
                            }
                            else
                            {
                                throw new ArgumentException("TreeViewItems that are children of Tasks should have a TaskFunctionSpecifier tag");
                            }
                        }
                        else
                        {
                            return true;    // can switch to TasksManager
                        }
                    }

                    var firstChild = panControlsContainer.Children[0];

                    var elementTag = (item.Tag ?? new DefaultTag("always-allow-switch")) as IElementTag;
                    string tag = elementTag.TagDescription;

                    canExecute = tag switch
                    {
                        "s3management" => !(firstChild is Tustler.UserControls.S3Management),
                        "credentials" => !(firstChild is Tustler.UserControls.Credentials),
                        "appSettings" => !(firstChild is AppSettingsControl),
                        "translate" => !(firstChild is Tustler.UserControls.TranslateFunctions),
                        "transcribe" => !(firstChild is Tustler.UserControls.TranscribeFunctions),
                        "polly" => !(firstChild is Tustler.UserControls.PollyFunctions),
                        "taskfunction" => CheckTaskFunction(firstChild, elementTag),
                        _ => true
                    };
                }
            }

            e.CanExecute = canExecute;
        }

        private void SwitchCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (tvActions.SelectedItem is TreeViewItem item)
            {
                if (CheckIfHandled(item))
                {
                    e.Handled = true;
                }
            }
        }

        private void ClearNotifications_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ClearNotifications_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var notifications = this.FindResource("applicationNotifications") as NotificationsList;
            notifications.Notifications.Clear();
        }

        private void CopyNotification_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbNotifications.SelectedItems.Count > 0);
        }

        private void CopyNotification_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var notification = (lbNotifications.SelectedItem as Notification);
            if (!(notification is null))
            {
                var content = notification switch
                {
                    ApplicationErrorInfo errorInfo => $"{errorInfo.Context}\n{errorInfo.Message}\nInner: {errorInfo.Exception.Message}\n{errorInfo.Exception.StackTrace}",
                    ApplicationMessageInfo messageInfo => $"{messageInfo.Message}\n{messageInfo.Detail}",
                    _ => throw new ArgumentException($"Unknown notification type: {notification}")
                };
                Clipboard.SetText(content, TextDataFormat.Text);
            }
        }

        private void CollapseNotifications_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CollapseNotifications_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var notificationsStoryboard = this.FindResource("notificationsStoryboard") as Storyboard;
            var doubleAnimation = notificationsStoryboard.Children[0] as DoubleAnimation;

            if (isCollapsed)
            {
                doubleAnimation.From = 0.0;
                doubleAnimation.To = 90.0;
                lbNotifications.Visibility = Visibility.Visible;    // make visible for the animation
            }
            else
            {
                // collapsing
                doubleAnimation.From = 90.0;
                doubleAnimation.To = 0.0;
            }

            notificationsStoryboard.Begin(this);
        }

        private void EnableMocking_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsMocked;
        }

        private void EnableMocking_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleMockingMode(true);
        }

        private void DisableMocking_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsMocked;
        }

        private void DisableMocking_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleMockingMode(false);
        }

        private void ToggleMockingMode(bool isMocked)
        {
            var app = App.Current as App;
            var awsInterface = app.ServiceProvider.GetService(typeof(AmazonWebServiceInterface)) as AmazonWebServiceInterface;
            awsInterface.RuntimeOptions.IsMocked = isMocked;
            awsInterface.Reinitialize();

            this.IsMocked = isMocked;
        }

        private void NotificationsStoryboard_Completed(object sender, EventArgs e)
        {
            var (vis, chevronTag) = isCollapsed switch
            {
                false => (Visibility.Collapsed, "chevron_compact_down"),
                true => (Visibility.Visible, "chevron_compact_up"),
            };

            isCollapsed = !isCollapsed;

            var data = this.FindResource(chevronTag) as StreamGeometry;
            collapseButtonPath.Data = data;
            lbNotifications.Visibility = vis;
        }

        private void TreeView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var tree = sender as TreeView;

            if (tree.SelectedItem is TreeViewItem item)
            {
                if (CheckIfHandled(item))
                {
                    e.Handled = true;
                }
            }
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if ((item.Items.Count == 1) && (item.Items[0] is string))
            {
                await CreateSubTree(item).ConfigureAwait(true);
                tvActions.Focus();  // otherwise it is lost on the async fetch
            }
        }

        private async void MenuItem_SubmenuOpenedAsync(object sender, RoutedEventArgs e)
        {
            // a menu has been clicked (perhaps the Tasks menu)
            MenuItem item = e.OriginalSource as MenuItem;
            if ((item.Items.Count == 1) && (item.Items[0] is string))
            {
                item.Items.Clear();

                if (taskFunctions is null)
                {
                    taskFunctions = await FindAllTaskFunctionModules().ConfigureAwait(true);
                }
                var topLevelFunctions = taskFunctions.Where(data => data.hideFromUI == false).Select(data => new TaskFunctionElement(data.specifier)).ToArray();
                var tasksDataModel = new TasksTreeViewDataModel(topLevelFunctions);
                AddItems<MenuItem>(CreateMenuItem, item, tasksDataModel.TreeViewItemDataCollection);
            }
        }

        private static async Task<(TaskFunctionSpecifier, bool)[]> FindAllTaskFunctionModules()
        {
            static IEnumerable<(TaskFunctionSpecifier, bool)> GetTaskFunctions(Assembly assembly, Type module)
            {
                var methods = module.GetMethods(BindingFlags.Public | BindingFlags.Static);

                return methods
                    .Select(mi => {
                        var enableLogging = Attribute.IsDefined(mi, typeof(EnableLogging));
                        var specifier = new TaskFunctionSpecifier(assembly.GetName().Name, module.FullName, mi.Name, enableLogging);
                        var hideFromUI = Attribute.IsDefined(mi, typeof(HideFromUI));
                        return (specifier, hideFromUI);
                    });
            }

            static void ScanModules(List<(TaskFunctionSpecifier, bool)> taskFunctions)
            {
                var loadedAssemblies = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.FullName));
                var assemblyFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, TaskFunctionModulePrefix, SearchOption.TopDirectoryOnly);

                foreach (var assemblyFile in assemblyFiles)
                {
                    var baseAssemblyName = Path.GetFileNameWithoutExtension(assemblyFile);
                    if (!loadedAssemblies.Any(fullName => fullName.StartsWith(baseAssemblyName, StringComparison.InvariantCulture)))   // skip already loaded assemblies
                    {
                        var assembly = Assembly.Load(baseAssemblyName);

                        foreach (var exportedType in assembly.GetExportedTypes())
                        {
                            if (Attribute.IsDefined(exportedType, typeof(CloudWeaverTaskFunctionModule)))
                            {
                                taskFunctions.AddRange(GetTaskFunctions(assembly, exportedType));
                            }
                        }
                    }
                }
            }

            var taskFunctions = new List<(TaskFunctionSpecifier, bool)>();
            await Task.Run(() => ScanModules(taskFunctions)).ConfigureAwait(true);

            return taskFunctions.ToArray();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //// Note: menuitem may be 'Loading...' or maybe an item on the Edit submenu
            var item = e.OriginalSource as MenuItem;

            if (CheckIfHandled(item)) {
                e.Handled = true;
            }
        }

        #endregion

        private static void AddItems<T>(Func<TreeViewItemData, T> createItem, HeaderedItemsControl item, ObservableCollection<TreeViewItemData> itemDataCollection)
        {
            foreach (var itemData in itemDataCollection)
            {
                item.Items.Add(createItem(itemData));
            }
        }

        private static TreeViewItem CreateTreeItem(TreeViewItemData itemData)
        {
            TreeViewItem item = new TreeViewItem
            {
                Header = itemData.Name,
                Tag = itemData.Tag
            };

            if (itemData.HasChildren)
            {
                item.Items.Add("Loading...");
            }

            return item;
        }

        private async Task CreateSubTree(TreeViewItem parentItem)
        {
            async Task<ObservableCollection<TreeViewItemData>> GetTasks()
            {
                if (taskFunctions is null)
                {
                    taskFunctions = await FindAllTaskFunctionModules().ConfigureAwait(true);
                }
                var topLevelFunctions = taskFunctions.Where(data => data.hideFromUI == false).Select(data => new TaskFunctionElement(data.specifier)).ToArray();
                var tasksDataModel = new TasksTreeViewDataModel(topLevelFunctions);
                return tasksDataModel.TreeViewItemDataCollection;
            }

            var elementTag = parentItem.Tag as IElementTag;
            var collection = (elementTag.TagDescription) switch
            {
                "settings" => new SettingsTreeViewDataModel().TreeViewItemDataCollection,
                "functions" => new FunctionsTreeViewDataModel().TreeViewItemDataCollection,
                "tasks" => await GetTasks().ConfigureAwait(true),
                _ => throw new ArgumentException("TreeView Expansion: Unexpected item tag")
            };

            parentItem.Items.Clear();
            AddItems<TreeViewItem>(CreateTreeItem, parentItem, collection);
        }

        private static MenuItem CreateMenuItem(TreeViewItemData itemData)
        {
            MenuItem item = new MenuItem
            {
                Header = itemData.Name,
                Tag = itemData.Tag
            };

            if (itemData.HasChildren)
            {
                item.Items.Add("Loading...");
            }

            return item;
        }

        private bool CheckIfHandled(HeaderedItemsControl item)
        {
            var elementTag = (item.Tag ?? new DefaultTag("do-not-handle")) as IElementTag;
            string tag = elementTag.TagDescription;
            bool handled = false;

            switch (tag)
            {
                case "s3management":
                    // fallthru
                case "credentials":
                // fallthru
                case "appSettings":
                // fallthru
                case "translate":
                // fallthru
                case "transcribe":
                // fallthru
                case "polly":
                    SwitchForm(tag);
                    handled = true;
                    break;
                case "do-not-handle":
                    break;
                case "tasks":   // Tasks parent item
                    break;
                case "taskfunction":
                    // pass the tag to the Tasks user control
                    SwitchForm("task", elementTag);
                    handled = true;
                    break;
                default:
                    break;
            }

            return handled;
        }

        private void SwitchForm(string tag, IElementTag initial = null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                panControlsContainer.Children.Clear();
                switch (tag)
                {
                    case "s3management":
                        panControlsContainer.Children.Add(new S3Management(awsInterface));
                        break;
                    case "credentials":
                        panControlsContainer.Children.Add(new UserControls.Credentials());
                        break;
                    case "appSettings":
                        panControlsContainer.Children.Add(new AppSettingsControl());
                        break;
                    case "polly":
                        panControlsContainer.Children.Add(new PollyFunctions(awsInterface));
                        break;
                    case "translate":
                        panControlsContainer.Children.Add(new TranslateFunctions(awsInterface));
                        break;
                    case "transcribe":
                        panControlsContainer.Children.Add(new TranscribeFunctions(awsInterface));
                        break;
                    case "task":
                        var allTaskFunctions = taskFunctions.Select(data => data.specifier).ToArray();
                        var taskFunctionElement = (initial as TaskFunctionElement);
                        var uc = new TasksManager(allTaskFunctions, awsInterface, taskLogger, taskFunctionElement.TaskFunctionSpecifier);
                        panControlsContainer.Children.Add(uc);
                        break;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Switch to the required Settings user control
        /// </summary>
        /// <param name="userControlTagName">The tag name of the user control</param>
        private void SwitchTo(string userControlTagName)
        {
            // find the Settings tag
            var settingsMenuItem = tvActions.Items.Cast<TreeViewItem>().First(item => (item.Tag as string) == "settings");

            // expand it if necessary
            if ((settingsMenuItem.Items.Count == 1) && (settingsMenuItem.Items[0] is string))
            {
                settingsMenuItem.Items.Clear();
                AddItems<TreeViewItem>(CreateTreeItem, settingsMenuItem, new SettingsTreeViewDataModel().TreeViewItemDataCollection);

                settingsMenuItem.IsExpanded = true;
            }

            // find the reguired menu item tag (throws InvalidOperationException if not found)
            var requiredMenuItem = settingsMenuItem.Items.Cast<TreeViewItem>().First(item => (item.Tag as string) == userControlTagName);

            // ... and set the focus so that the command can execute
            requiredMenuItem.Focus();

            // invoke the bound command
            if (MainWindowCommands.Switch.CanExecute(null, tvActions))
            {
                MainWindowCommands.Switch.Execute(null, tvActions);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // status bar mode menu has been opened
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public static class MainWindowCommands
    {
        public static readonly RoutedUICommand CollapseNotifications = new RoutedUICommand
            (
                "CollapseNotifications",
                "CollapseNotifications",
                typeof(MainWindowCommands)
            );

        public static readonly RoutedUICommand Exit = new RoutedUICommand
            (
                "Exit",
                "Exit",
                typeof(MainWindowCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.F4, ModifierKeys.Alt)
                }
            );

        public static readonly RoutedUICommand About = new RoutedUICommand
            (
                "About",
                "About",
                typeof(MainWindowCommands)
            );

        public static readonly RoutedUICommand EnableMocking = new RoutedUICommand
            (
                "Enable Mocking",
                "EnableMocking",
                typeof(MainWindowCommands),
                null
            );

        public static readonly RoutedUICommand DisableMocking = new RoutedUICommand
            (
                "Disable Mocking",
                "DisableMocking",
                typeof(MainWindowCommands),
                null
            );

        public static readonly RoutedCommand Switch = new RoutedCommand
            (
                "Switch",
                typeof(MainWindowCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.Enter)
                }
            );

        public static readonly RoutedCommand ClearNotifications = new RoutedCommand
            (
                "ClearNotifications",
                typeof(MainWindowCommands),
                null
            );

        public static readonly RoutedCommand CopyNotification = new RoutedCommand
            (
                "CopyNotification",
                typeof(MainWindowCommands),
                null
            );
    }
}
