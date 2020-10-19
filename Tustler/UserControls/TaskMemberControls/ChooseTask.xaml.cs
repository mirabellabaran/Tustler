using CloudWeaver.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tustler.Helpers;
using TustlerServicesLib;
using TustlerUIShared;

namespace Tustler.UserControls.TaskMemberControls
{
    public class IndexedSpecifier: IEqualityComparer    //, IComparable
    {
        public IndexedSpecifier(int index, TaskFunctionSpecifier specifier)
        {
            Index = index;
            FunctionSpecifier = specifier;
        }

        public int Index { get; }

        public TaskFunctionSpecifier FunctionSpecifier { get; }

        public int CompareTo(object o)
        {
            if (o is IndexedSpecifier idxSpec)
            {
                return this.Index.CompareTo(idxSpec.Index);
            }
            else
                throw new ArgumentNullException(nameof(o));
        }

        #region IEqualityComparer (on Index; used for HashSet)

        public new bool Equals(object x, object y)
        {
            if ((x is IndexedSpecifier xx) && (y is IndexedSpecifier yy))
                return xx.Index.Equals(yy.Index);
            else
                throw new ArgumentNullException($"{nameof(x)} and {nameof(y)} must not be null");
        }

        public int GetHashCode(object obj)
        {
            if (obj is IndexedSpecifier idxSpec)
                return idxSpec.Index.GetHashCode();
            else
                throw new ArgumentNullException(nameof(obj));
        }

        #endregion

        //#region IComparable (on TaskName; used for sorting)

        //public override int GetHashCode()
        //{
        //    return HashCode.Combine(FunctionSpecifier.TaskName);
        //}

        //public override bool Equals(object obj)
        //{
        //    if (ReferenceEquals(this, obj))
        //    {
        //        return true;
        //    }

        //    if (obj is null)
        //    {
        //        return false;
        //    }

        //    if (obj is IndexedSpecifier idxSpec)
        //        return this.FunctionSpecifier.TaskName.Equals(idxSpec.FunctionSpecifier.TaskName, StringComparison.InvariantCulture);
        //    else
        //        return false;
        //}

        //public static bool operator ==(IndexedSpecifier left, IndexedSpecifier right)
        //{
        //    if (left is null)
        //    {
        //        return right is null;
        //    }

        //    return left.Equals(right);
        //}

        //public static bool operator !=(IndexedSpecifier left, IndexedSpecifier right)
        //{
        //    return !(left == right);
        //}

        //public static bool operator <(IndexedSpecifier left, IndexedSpecifier right)
        //{
        //    return left is null ? right is object : left.CompareTo(right) < 0;
        //}

        //public static bool operator <=(IndexedSpecifier left, IndexedSpecifier right)
        //{
        //    return left is null || left.CompareTo(right) <= 0;
        //}

        //public static bool operator >(IndexedSpecifier left, IndexedSpecifier right)
        //{
        //    return left is object && left.CompareTo(right) > 0;
        //}

        //public static bool operator >=(IndexedSpecifier left, IndexedSpecifier right)
        //{
        //    return left is null ? right is null : left.CompareTo(right) >= 0;
        //}

        //#endregion
    }

    /// <summary>
    /// Interaction logic for ChooseTask.xaml
    /// </summary>
    public partial class ChooseTask : UserControl, ICommandSource
    {
        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(ChooseTask), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is ChooseTask ctrl)
            {
                if (dependencyPropertyChangedEventArgs.NewValue != null)
                {
                    var newState = (bool)dependencyPropertyChangedEventArgs.NewValue;
                    ctrl.btnContinue.IsEnabled = newState;
                }
            }
        }

        /// <summary>
        ///  Enables or disables the Continue button
        /// </summary>
        public bool IsButtonEnabled
        {
            get { return (bool)GetValue(IsButtonEnabledProperty); }
            set { SetValue(IsButtonEnabledProperty, value); }
        }
        #endregion

        #region Available DependencyProperty
        public static readonly DependencyProperty AvailableProperty =
            DependencyProperty.Register("Available", typeof(IEnumerable<IndexedSpecifier>), typeof(ChooseTask));

        /// <summary>
        ///  Available (non-selected) Task Functions
        /// </summary>
        public IEnumerable<IndexedSpecifier> Available
        {
            get { return (IEnumerable<IndexedSpecifier>) GetValue(AvailableProperty); }
            set { SetValue(AvailableProperty, value); }
        }
        #endregion

        #region Selected DependencyProperty
        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(IEnumerable<IndexedSpecifier>), typeof(ChooseTask));

        /// <summary>
        ///  Selected Task Functions
        /// </summary>
        public IEnumerable<IndexedSpecifier> Selected
        {
            get { return (IEnumerable<IndexedSpecifier>)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }
        #endregion

        public ChooseTask()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;      // child elements of LayoutRoot control use this as the context to access Available and Selected properties
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var wrapper = this.DataContext as ResponseWrapper;
            var taskFunctions = wrapper.Owner.TaskFunctions.Select((specifier, index) => new IndexedSpecifier(index, specifier));

            this.Available = taskFunctions.OrderBy(idxSpec => idxSpec.FunctionSpecifier.TaskName);
            this.Selected = Enumerable.Empty<IndexedSpecifier>();
        }

        #region ICommandSource

        #region ICommandSource Common Elements

        public IInputElement CommandTarget => this;

        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
        {
            if (oldCommand != null)
            {
                RemoveCommand(oldCommand);
            }

            AddCommand(newCommand);
        }

        // Remove an old command from the Command Property
        private void RemoveCommand(ICommand oldCommand)
        {
            EventHandler handler = CanExecuteChanged;
            oldCommand.CanExecuteChanged -= handler;
        }

        // Add the command
        private void AddCommand(ICommand newCommand)
        {
            EventHandler handler = new EventHandler(CanExecuteChanged);

            if (newCommand != null)
            {
                newCommand.CanExecuteChanged += handler;
            }
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            if (this.Command != null)
            {
                if (this.Command is RoutedCommand command)
                {
                    if (command.CanExecute(CommandParameter, CommandTarget))
                    {
                        this.IsEnabled = true;
                    }
                    else
                    {
                        this.IsEnabled = false;
                    }
                }
                else
                {
                    if (Command.CanExecute(CommandParameter))
                    {
                        this.IsEnabled = true;
                    }
                    else
                    {
                        this.IsEnabled = false;
                    }
                }
            }
        }

        #endregion

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(ChooseTask),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChooseTask ctrl = (ChooseTask)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get;
            internal set;
        }

        #endregion

        private void ExecuteAction(Action<ImmutableHashSet<IndexedSpecifier>, ImmutableHashSet<IndexedSpecifier>> action)
        {
            var available = lbAvailable.Items.Cast<IndexedSpecifier>().ToImmutableHashSet();
            var selected = lbSelected.Items.Cast<IndexedSpecifier>().ToImmutableHashSet();

            action(available, selected);
        }

        private void Select_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = lbAvailable is object && lbAvailable.SelectedItems.Count > 0;
        }

        private void Select_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentSelection = lbAvailable.SelectedItems.Cast<IndexedSpecifier>().ToImmutableHashSet();

            SelectItems(currentSelection);
        }

        private void Unselect_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = lbSelected is object && lbSelected.SelectedItems.Count > 0;
        }

        private void Unselect_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentSelection = lbSelected.SelectedItems.Cast<IndexedSpecifier>().ToImmutableHashSet();

            UnselectItems(currentSelection);
        }

        private void MoveUp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbSelected is object) && (lbSelected.SelectedItems.Count == 1)
                && (lbSelected.SelectedIndex > 0);
        }

        private void MoveUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentIndex = lbSelected.SelectedIndex;

            if (currentIndex > 0)
            {
                var currentItem = lbSelected.SelectedItem as IndexedSpecifier;

                var selected = lbSelected.Items.Cast<IndexedSpecifier>().ToImmutableArray();

                var builder = ImmutableArray.CreateBuilder<IndexedSpecifier>(selected.Length);
                builder.AddRange(selected);
                builder.Remove(currentItem);
                builder.Insert(currentIndex - 1, currentItem);

                this.Selected = builder;
            }
        }

        private void MoveDown_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbSelected is object) && (lbSelected.SelectedItems.Count == 1)
                && (lbSelected.SelectedIndex < lbSelected.Items.Count - 1);
        }

        private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentIndex = lbSelected.SelectedIndex;
            var length = lbSelected.Items.Count;

            if (currentIndex >= 0 && currentIndex < length - 1)
            {
                var currentItem = lbSelected.SelectedItem as IndexedSpecifier;

                var selected = lbSelected.Items.Cast<IndexedSpecifier>().ToImmutableArray();

                var builder = ImmutableArray.CreateBuilder<IndexedSpecifier>(selected.Length);
                builder.AddRange(selected);
                builder.RemoveAt(currentIndex);
                builder.Insert(currentIndex + 1, currentItem);

                this.Selected = builder;
            }
        }

        private void ExecuteCommand()
        {
            if (this.Command != null)
            {
                if (Command is RoutedCommand command)
                {
                    command.Execute(CommandParameter, CommandTarget);
                }
                else
                {
                    ((ICommand)Command).Execute(CommandParameter);
                }
            }
        }

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbSelected is object) && (lbSelected.Items.Count > 0);
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItems = lbSelected.Items.Cast<IndexedSpecifier>();

            var taskItems = selectedItems.Select(idxSpec => idxSpec.FunctionSpecifier);

            var data = CloudWeaver.SerializableTypeGenerator.CreateTaskItems(taskItems);

            CommandParameter = new UITaskArguments(UITaskMode.SelectTask, "StandardShareIntraModule", "SetTaskItem", data);

            ExecuteCommand();
        }

        private void Available_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GetBoundValue(e.Source, SelectItems);
        }

        private void Selected_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GetBoundValue(e.Source, UnselectItems);
        }

        private void GetBoundValue(object clickedItem, Action<ImmutableHashSet<IndexedSpecifier>> action)
        {
            if (clickedItem is Label lbl)
            {
                if (lbl.TemplatedParent is ContentPresenter cp)
                {
                    if (cp.TemplatedParent is ListBoxItem lbi)
                    {
                        if (lbi.DataContext is IndexedSpecifier idxSpec)
                        {
                            var clickedIndexSpecifier = ImmutableHashSet.Create(idxSpec);
                            action(clickedIndexSpecifier);
                        }
                    }
                }
            }
        }

        private void SelectItems(ImmutableHashSet<IndexedSpecifier> currentSelection)
        {
            ExecuteAction((available, selected) =>
            {
                this.Available = available.Except(currentSelection).OrderBy(idxSpec => idxSpec.FunctionSpecifier.TaskName);
                this.Selected = selected.Concat(currentSelection);
            });
        }

        private void UnselectItems(ImmutableHashSet<IndexedSpecifier> currentSelection)
        {
            ExecuteAction((available, selected) =>
            {
                this.Available = available.Concat(currentSelection).OrderBy(idxSpec => idxSpec.FunctionSpecifier.TaskName);
                this.Selected = selected.Except(currentSelection);
            });
        }
    }

    public static class ChooseTaskCommands
    {
        public static readonly RoutedUICommand Select = new RoutedUICommand
            (
                "Select",
                "Select",
                typeof(ChooseTaskCommands),
                null
            );

        public static readonly RoutedUICommand Unselect = new RoutedUICommand
            (
                "Unselect",
                "Unselect",
                typeof(ChooseTaskCommands),
                null
            );

        public static readonly RoutedUICommand MoveUp = new RoutedUICommand
            (
                "MoveUp",
                "MoveUp",
                typeof(ChooseTaskCommands),
                null
            );

        public static readonly RoutedUICommand MoveDown = new RoutedUICommand
            (
                "MoveDown",
                "MoveDown",
                typeof(ChooseTaskCommands),
                null
            );

        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(ChooseTaskCommands),
                null
            );
    }
}
