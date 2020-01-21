using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Tustler
{
    public class TreeViewItem
    {
        public string Name
        {
            get;
            set;
        }

        public ObservableCollection<TreeViewItem> ChildItems
        {
            get;
            set;
        }
    }

    public class SettingsTreeViewDataModel
    {
        //enum SectionType
        //{
        //    Settings,
        //    IndividualFunctions,
        //    Tasks
        //}

        public ObservableCollection<TreeViewItem> TreeViewItems
        {
            get;
            set;
        }

        public SettingsTreeViewDataModel()
        {
            var divisions = new string[] { "Setting A", "Setting B", "Setting C" };
            var divisionItems = from division in divisions select new TreeViewItem { Name = division };

            //ObservableCollection<TreeViewItem> itemSelector(SectionType header)
            //{
            //    return header switch
            //    {
            //        SectionType.Settings => new ObservableCollection<TreeViewItem>(divisionItems),
            //        SectionType.IndividualFunctions => new ObservableCollection<TreeViewItem>(divisionItems),
            //        SectionType.Tasks => new ObservableCollection<TreeViewItem>(divisionItems),
            //        _ => throw new ArgumentOutOfRangeException(),
            //    };
            //}
            //var sectionInfo = new (string name, SectionType type)[] { ("Settings", SectionType.Settings), ("Individual Functions", SectionType.IndividualFunctions), ("Tasks", SectionType.Tasks) };
            //var sections = from info in sectionInfo
            //               select new TreeViewItem { Name = info.name, ChildItems = itemSelector(info.type) };

            this.TreeViewItems = new ObservableCollection<TreeViewItem>(divisionItems);
        }
    }

    public class FunctionsTreeViewDataModel
    {
        public ObservableCollection<TreeViewItem> TreeViewItems
        {
            get;
            set;
        }

        public FunctionsTreeViewDataModel()
        {
            var divisions = new string[] { "Function A", "Function B", "Function C" };
            var divisionItems = from division in divisions select new TreeViewItem { Name = division };

            this.TreeViewItems = new ObservableCollection<TreeViewItem>(divisionItems);
        }
    }

    public class TasksTreeViewDataModel
    {
        public ObservableCollection<TreeViewItem> TreeViewItems
        {
            get;
            set;
        }

        public TasksTreeViewDataModel()
        {
            var divisions = new string[] { "Task A", "Task B", "Task C" };
            var divisionItems = from division in divisions select new TreeViewItem { Name = division };

            this.TreeViewItems = new ObservableCollection<TreeViewItem>(divisionItems);
        }
    }
}
