using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
            var divisions = new (string name, string tag)[] { ("Credentials Management", "credentials"), ("Setting B", "b"), ("Setting C", "c") };
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
            var divisions = new (string name, string tag)[] { ("Polly", "polly"), ("Translate", "translate"), ("Function C", "c") };
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
            var tasks = new (string name, string tag)[] { ("Task A", "a"), ("Task B", "b"), ("Task C", "c") };
            var divisionItems = from task in tasks select new TreeViewItemData { Name = task.name, Tag = task.tag, HasChildren = false };

            this.TreeViewItemDataCollection = new ObservableCollection<TreeViewItemData>(divisionItems);
        }
    }
}
