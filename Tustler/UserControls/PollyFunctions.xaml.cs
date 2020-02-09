﻿using System;
using System.Collections.Generic;
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
using Tustler.Models;

namespace Tustler.UserControls
{
    /// <summary>
    /// Interaction logic for PollyFunctions.xaml
    /// </summary>
    public partial class PollyFunctions : UserControl
    {
        private readonly NotificationsList notifications;

        public PollyFunctions()
        {
            InitializeComponent();

            notifications = this.FindResource("applicationNotifications") as NotificationsList;
        }

        private void ListVoices_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListVoices_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var voicesInstance = this.FindResource("voicesInstance") as VoicesViewModel;

            string languageCode = null;

            // refresh and then enable the headers
            await voicesInstance.Refresh(notifications, languageCode)
                .ContinueWith(task => dgVoices.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }

        private void GetLexicon_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(tbLexiconName.Text);
        }

        private async void GetLexicon_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var attributesInstance = this.FindResource("lexiconAttributesInstance") as LexiconAttributesViewModel;
            
            await attributesInstance.Refresh(notifications, tbLexiconName.Text)
                .ContinueWith(task => dgLexiconAttributes.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }

        private void ListLexicons_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void ListLexicons_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var lexiconsInstance = this.FindResource("lexiconsInstance") as LexiconsViewModel;

            await lexiconsInstance.Refresh(notifications)
                .ContinueWith(task => dgLexicons.HeadersVisibility = DataGridHeadersVisibility.All, TaskScheduler.FromCurrentSynchronizationContext()).ConfigureAwait(true);
        }
    }

    public static class PollyCommands
    {
        public static readonly RoutedUICommand ListVoices = new RoutedUICommand
            (
                "ListVoices",
                "ListVoices",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand GetLexicon = new RoutedUICommand
            (
                "GetLexicon",
                "GetLexicon",
                typeof(PollyCommands),
                null
            );

        public static readonly RoutedUICommand ListLexicons = new RoutedUICommand
            (
                "ListLexicons",
                "ListLexicons",
                typeof(PollyCommands),
                null
            );

    }
}
