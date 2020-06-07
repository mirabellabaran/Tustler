using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Tustler.Models;

namespace Tustler.Helpers.UIServices
{
    /// <summary>
    /// Modified from https://stackoverflow.com/questions/3480966/display-hourglass-when-application-is-busy
    /// </summary>
    /// <see cref="T.J.Kjaer"/>
    public static class MouseTImer
    {
        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        private static bool IsBusy;

        /// <summary>
        /// Set the busystate as busy
        /// </summary>
        public static void SetBusyState()
        {
            SetBusyState(true);
        }

        /// <summary>
        /// Set the busystate to busy or not busy
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        private static void SetBusyState(bool busy)
        {
            if (busy != IsBusy)
            {
                IsBusy = busy;
                Mouse.OverrideCursor = busy ? Cursors.Wait : null;

                if (IsBusy)
                {
                    var _ = new DispatcherTimer(TimeSpan.FromSeconds(0), DispatcherPriority.ApplicationIdle, dispatcherTimer_Tick, Application.Current.Dispatcher);
                }
            }
        }

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var dispatcherTimer = sender as DispatcherTimer;
            if (dispatcherTimer != null)
            {
                SetBusyState(false);
                dispatcherTimer.Stop();
            }
        }
    }

    public static class UIHelpers
    {
        /// <summary>
        /// Extract a selected field from the list of selected items in a ListBox
        /// </summary>
        /// <param name="chkIncludeNames">If set and true, return the selected items</param>
        /// <param name="lbNames">A listbox with items selected</param>
        /// <returns></returns>
        public static List<string> GetFieldFromListBoxSelectedItems<T>(CheckBox chkIncludeNames, ListBox lbNames, Func<T, string> selector)
        {
            if (chkIncludeNames is null) throw new ArgumentNullException(nameof(chkIncludeNames));
            if (lbNames is null) throw new ArgumentNullException(nameof(lbNames));

            if (chkIncludeNames.IsChecked.HasValue && chkIncludeNames.IsChecked.Value && lbNames.SelectedItems.Count > 0)
            {
                var selectedObjects = (lbNames.SelectedItems as IEnumerable<object>).Cast<T>();
                return selectedObjects.Select<T, string>(selector).ToList();
            }
            else
            {
                return null;
            }
        }

        public static void ScrollIntoView(this ItemsControl control, object item)
        {
            if (control is null) throw new ArgumentNullException(nameof(control));

            if (!(control.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement element)) { return; }

            element.BringIntoView();
        }

        public static void ScrollIntoView(this ItemsControl control)
        {
            if (control is null) throw new ArgumentNullException(nameof(control));
            int count = control.Items.Count;

            if (count > 0)
            {
                object item = control.Items[count - 1];
                control.ScrollIntoView(item);
            }
        }
    }
}
