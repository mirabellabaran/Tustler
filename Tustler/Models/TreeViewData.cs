using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TustlerWinPlatformLib;

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

    public class ScriptsTreeViewDataModel
    {
        public ObservableCollection<TreeViewItemData> TreeViewItemDataCollection
        {
            get;
            private set;
        }

        public ScriptsTreeViewDataModel()
        {
            TreeViewItemDataCollection = new ObservableCollection<TreeViewItemData>();
        }

        public async Task InitializeAsync()
        {
            var treeViewItems = await FetchScriptsAsync().ConfigureAwait(false);
            foreach (var item in treeViewItems)
            {
                TreeViewItemDataCollection.Add(item);
            }
        }

        private async Task<IEnumerable<TreeViewItemData>> FetchScriptsAsync()
        {
            var scripts = await Task.Run(() => GetScriptNames()).ConfigureAwait(false);
            var divisionItems = from script in scripts select new TreeViewItemData { Name = script.name, Tag = script.tag, HasChildren = false };

            return divisionItems;
        }

        private static IEnumerable<(string name, string tag)> GetScriptNames()
        {
            return Directory.EnumerateFiles(ApplicationSettings.ScriptsDirectoryPath, "*.fsx", SearchOption.TopDirectoryOnly)
                            .Select(scriptPath => {
                                var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                                return (scriptName, scriptName);
                                });
        }
    }
}
