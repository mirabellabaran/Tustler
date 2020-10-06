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

namespace Tustler.Models
{
    public interface IElementTag
    {
        public string TagDescription { get; }
    }

    /// <summary>
    /// The default tag for menu and treeview items just wraps a string
    /// </summary>
    /// <seealso cref="TaskFunctionSpecifier"/>
    public class DefaultTag : IElementTag
    {
        private readonly string description;

        public DefaultTag(string description)
        {
            this.description = description;
        }

        public string TagDescription => description;
    }

    public class TreeViewItemData
    {
        public string Name
        {
            get;
            set;
        }

        public IElementTag Tag
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
            //set;
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
            var divisionItems = from division in divisions select new TreeViewItemData { Name = division.name, Tag = new DefaultTag(division.tag), HasChildren = false };

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
            var divisionItems = from division in divisions select new TreeViewItemData { Name = division.name, Tag = new DefaultTag(division.tag), HasChildren = false };

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

        public TasksTreeViewDataModel(TaskFunctionElement[] taskFunctions)
        {
            var divisionItems = from taskFunction in taskFunctions select new TreeViewItemData { Name = taskFunction.TaskName, Tag = taskFunction, HasChildren = false };
            TreeViewItemDataCollection = new ObservableCollection<TreeViewItemData>(divisionItems);
        }
    }
}
