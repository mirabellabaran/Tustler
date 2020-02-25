using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Tustler.Models
{
    /// <summary>
    /// Provides a list of string items that summarize the current selection for a multi-select listbox
    /// </summary>
    public class SelectedItemsViewModel
    {
        public ObservableCollection<string> Items
        {
            get;
            private set;
        }

        public SelectedItemsViewModel()
        {
            this.Items = new ObservableCollection<string>();
        }

        public void Update(IEnumerable<object> items)
        {
            Items.Clear();
            foreach (var item in Convert(items))
            {
                Items.Add(item);
            }
        }

        public static IEnumerable<string> Convert(IEnumerable<object> objects)
        {
            return objects.Select(o =>
            {
                if (o is LanguageCode)
                {
                    return (o as LanguageCode).Code;
                }
                else if (o is Terminology)
                {
                    return (o as Terminology).Name;
                }
                else
                {
                    return o.ToString();
                }
            });
        }
    }
}
