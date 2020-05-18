using System;
using System.Windows;
using System.Windows.Controls;

namespace Tustler.Helpers
{
    /// <summary>
    /// Causes the owner of the attached Items property to scroll the last item into view
    /// </summary>
    /// Modified from <see cref="https://stackoverflow.com/questions/33597483/wpf-mvvm-scrollintoview"/>
    public static class ScrollToLastItemBehavior
    {
        // attach to Items property as it changes on every new item
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.RegisterAttached(
            "Items",
            typeof(ItemCollection),
            typeof(ScrollToLastItemBehavior),
            new PropertyMetadata(null, OnItemsChange));

        public static void SetItems(DependencyObject source, object value)
        {
            source.SetValue(ItemsProperty, value);
        }

        public static object GetItems(DependencyObject source)
        {
            return (object)source.GetValue(ItemsProperty);
        }

        private static void OnItemsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ItemCollection items)
            {
                if (items.Count > 0)
                {
                    var listbox = d as ListBox;

                    var lastItem = items[items.Count - 1];
                    listbox.ScrollIntoView(lastItem);
                }
            }
        }
    }
}