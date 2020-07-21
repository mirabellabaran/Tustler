using CloudWeaver.Types;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TustlerFSharpPlatform;

namespace Tustler
{
    public class TreeViewItemData
    {
        public string Name
        {
            get;
            set;
        }

        public string Tag
        {
            get;
            set;
        }

        public bool HasChildren
        {
            get;
            set;
        }

        public ObservableCollection<TreeViewItemData> ChildItemDataCollection
        {
            get;
            set;
        }
    }

    public class SettingsTreeViewDataModel
    {
        public ObservableCollection<TreeViewItemData> TreeViewItemDataCollection
        {
            get;
            private set;
        }

        public SettingsTreeViewDataModel()
        {
            var divisions = new (string name, string tag)[] { ("Credentials", "credentials"), ("Application Settings", "appSettings") };
            var divisionItems = from division in divisions select new TreeViewItemData { Name = division.name, Tag = division.tag, HasChildren = false };

            this.TreeViewItemDataCollection = new ObservableCollection<TreeViewItemData>(divisionItems);
        }
    }

    public class FunctionsTreeViewDataModel
    {
        public ObservableCollection<TreeViewItemData> TreeViewItemDataCollection
        {
            get;
            private set;
        }

        public FunctionsTreeViewDataModel()
        {
            var divisions = new (string name, string tag)[] { ("Polly", "polly"), ("Translate", "translate"), ("Transcribe", "transcribe") };
            var divisionItems = from division in divisions select new TreeViewItemData { Name = division.name, Tag = division.tag, HasChildren = false };

            this.TreeViewItemDataCollection = new ObservableCollection<TreeViewItemData>(divisionItems);
        }
    }

    public class TasksTreeViewDataModel
    {
        public ObservableCollection<TreeViewItemData> TreeViewItemDataCollection
        {
            get;
            private set;
        }

        public TasksTreeViewDataModel()
        {
            TreeViewItemDataCollection = new ObservableCollection<TreeViewItemData>();
        }

        public async Task InitializeAsync()
        {
            var treeViewItems = await FetchTasksAsync().ConfigureAwait(false);
            foreach (var item in treeViewItems)
            {
                TreeViewItemDataCollection.Add(item);
            }
        }

        private static async Task<IEnumerable<TreeViewItemData>> FetchTasksAsync()
        {
            var tasks = await Task.Run(() => GetTaskNames()).ConfigureAwait(false);
            var divisionItems = from task in tasks select new TreeViewItemData { Name = task.name, Tag = task.tag, HasChildren = false };

            return divisionItems;
        }

        private static IEnumerable<(string name, string tag)> GetTaskNames()
        {
            var asm = Assembly.Load("CloudWeaver.AWS");
            var tasksModule = asm.GetType("CloudWeaver.AWS.Tasks");
            var methods = tasksModule.GetMethods(BindingFlags.Public | BindingFlags.Static);

            return methods.Where(mi => !Attribute.IsDefined(mi, typeof(HideFromUI))).Select(mi => (mi.Name, mi.Name));
        }
    }
}
