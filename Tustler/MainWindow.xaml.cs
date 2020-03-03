using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tustler.Models;
using Tustler.UserControls;

namespace Tustler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Event Handlers

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // look for status changes in the notifications listbox so that it can scroll new items into view
            lbNotifications.ItemContainerGenerator.ItemsChanged += lbNotifications_ItemsChanged;

            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "S3 Management", Tag = "s3management", HasChildren = false }));
            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Settings", Tag = "settings", HasChildren = true }));

            var functionSubTree = CreateTreeItem(new TreeViewItemData { Name = "Individual Functions", Tag = "functions", HasChildren = true });
            tvActions.Items.Add(functionSubTree);
            await CreateSubTree(functionSubTree).ConfigureAwait(true);
            functionSubTree.IsExpanded = true;

            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Tasks", Tag = "tasks", HasChildren = true }));

            menuTasks.Items.Add(CreateMenuItem(new TreeViewItemData { Name = "Tasks", Tag = "tasks", HasChildren = true }));

            var accessKey = TustlerAWSLib.Utilities.CheckCredentials();
            if (accessKey != null)  // MG change to ==
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

                // find the credentials tag (throws InvalidOperationException if not found)
                var credentialsMenuItem = settingsMenuItem.Items.Cast<TreeViewItem>().First(item => (item.Tag as string) == "credentials");

                // set focus so that the command can execute
                credentialsMenuItem.Focus();

                // invoke the bound command
                if (MainWindowCommands.Switch.CanExecute(null, tvActions))
                {
                    MainWindowCommands.Switch.Execute(null, tvActions);
                }
            }
        }

        private void lbNotifications_ItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs e)
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

        private void SwitchCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (tvActions?.SelectedItem != null);
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
            await Task.Delay(2000).ConfigureAwait(false);
            MenuItem item = e.OriginalSource as MenuItem;
            if ((item.Items.Count == 1) && (item.Items[0] is string))
            {
                item.Items.Clear();

                var tasks = new TasksTreeViewDataModel();
                AddItems<MenuItem>(CreateMenuItem, item, tasks.TreeViewItemDataCollection);
            }
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

        private static async Task CreateSubTree(TreeViewItem parentItem)
        {
            static async Task<ObservableCollection<TreeViewItemData>> GetTasks()
            {
                await Task.Delay(2000).ConfigureAwait(false);

                var tasks = new TasksTreeViewDataModel();
                return tasks.TreeViewItemDataCollection;
            }

            var collection = (parentItem.Tag) switch
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
            string tag = (item.Tag ?? "do-not-handle") as string;
            bool handled = false;

            switch (tag)
            {
                case "s3management":
                    // fallthru
                case "credentials":
                // fallthru
                case "translate":
                // fallthru
                case "polly":
                    SwitchForm(tag);
                    handled = true;
                    break;
                case "do-not-handle":
                    break;
                default:
                    FormattableString message = $"Menu or Tree Item {item.Header} {tag}";
                    MessageBox.Show(FormattableString.Invariant(message));
                    break;
            }

            return handled;
        }

        private void SwitchForm(string tag)
        {
            panControlsContainer.Children.Clear();
            switch (tag)
            {
                case "s3management":
                    panControlsContainer.Children.Add(new S3Management());
                    break;
                case "credentials":
                    panControlsContainer.Children.Add(new Credentials());
                    break;
                case "polly":
                    panControlsContainer.Children.Add(new PollyFunctions());
                    break;
                case "translate":
                    panControlsContainer.Children.Add(new TranslateFunctions());
                    break;
            }
        }
    }

    public static class MainWindowCommands
    {
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
    }
}
