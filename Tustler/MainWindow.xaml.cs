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

            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "S3 Management", Tag = "s3management", HasChildren = false }));
            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Settings", Tag = "settings", HasChildren = true }));
            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Individual Functions", Tag = "functions", HasChildren = true }));
            tvActions.Items.Add(CreateTreeItem(new TreeViewItemData { Name = "Tasks", Tag = "tasks", HasChildren = true }));

            menuTasks.Items.Add(CreateMenuItem(new TreeViewItemData { Name = "Tasks", Tag = "tasks", HasChildren = true }));
        }

        #region Event Handlers

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TreeView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var tree = sender as TreeView;

            if (tree.SelectedItem is TreeViewItem item)
            {
                //FormattableString message = $"Tree Item {item.Header} {item.Tag}";
                FormattableString message = $"Tree Item {item.Header}";
                MessageBox.Show(FormattableString.Invariant(message));
            }
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if ((item.Items.Count == 1) && (item.Items[0] is string))
            {
                static async Task<ObservableCollection<TreeViewItemData>> GetTasks()
                {
                    await Task.Delay(2000);

                    var tasks = new TasksTreeViewDataModel();
                    return tasks.TreeViewItemDataCollection;
                }

                var collection = (item.Tag) switch
                {
                    "settings" => new SettingsTreeViewDataModel().TreeViewItemDataCollection,
                    "functions" => new FunctionsTreeViewDataModel().TreeViewItemDataCollection,
                    "tasks" => await GetTasks(),
                    _ => throw new ArgumentException("TreeView Expansion: Unexpected item tag")
                };

                item.Items.Clear();
                AddItems<TreeViewItem>(CreateTreeItem, item, collection);
            }
        }

        private async void MenuItem_SubmenuOpenedAsync(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);
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
            // Note: menuitem may be 'Loading...'
            var item = e.OriginalSource as MenuItem;
            FormattableString message = $"Menu Item: {item.Header} with tag: {item.Tag}";
            MessageBox.Show(FormattableString.Invariant(message));
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
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand
            (
                "Exit",
                "Exit",
                typeof(CustomCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.F4, ModifierKeys.Alt)
                }
            );

        //Define more commands here, just like the one above
    }
}
