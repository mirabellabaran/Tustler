using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Tustler.Helpers
{
    /// <summary>
    /// Modified from https://stackoverflow.com/questions/3480966/display-hourglass-when-application-is-busy
    /// </summary>
    /// <see cref="T.J.Kjaer"/>
    public static class UIServices
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

    /// <summary>
    /// Modified from https://stackoverflow.com/questions/58510/using-net-how-can-you-find-the-mime-type-of-a-file-based-on-the-file-signature/9435701#9435701
    /// </summary>
    /// <see cref="Frederick Samson"/>
    public static class FileServices
    {
        public static string GetMimeType(string sFilePath)
        {
            string sMimeType = TustlerServicesLib.MimeTypeDictionary.GetMimeTypeFromList(sFilePath);

            if (String.IsNullOrEmpty(sMimeType))
            {
                sMimeType = TustlerWinPlatformLib.NativeMethods.GetMimeTypeFromFile(sFilePath);

                if (String.IsNullOrEmpty(sMimeType))
                {
                    sMimeType = TustlerWinPlatformLib.RegistryServices.GetMimeTypeFromRegistry(sFilePath);
                }
            }

            return sMimeType;
        }
    }

}

