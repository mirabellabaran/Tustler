using CloudWeaver;
using CloudWeaver.AWS;
using CloudWeaver.MediaServices;
using CloudWeaver.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tustler.Helpers;
using TustlerServicesLib;
using TustlerUIShared;

namespace Tustler.UserControls.TaskMemberControls
{
    /// <summary>
    /// Interaction logic for DefaultResponseHandler.xaml
    /// </summary>
    public partial class DefaultResponseHandler : UserControl, ICommandSource
    {
        string requestModuleName;
        string requestResponseName;

        #region IsButtonEnabled DependencyProperty
        public static readonly DependencyProperty IsButtonEnabledProperty =
            DependencyProperty.Register("IsButtonEnabled", typeof(bool), typeof(DefaultResponseHandler), new PropertyMetadata(true, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is DefaultResponseHandler ctrl)
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

        public DefaultResponseHandler()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var wrapper = this.DataContext as ResponseWrapper;
            var response = wrapper.TaskResponse;

            switch (response)
            {
                case TaskResponse.RequestArgument arg:
                    HandleRequestArgument(arg);
                    break;
                case TaskResponse.SetArgument arg:
                    HandleSetArgument(arg);
                    break;
                default:
                    tbInfo.Text = $"Unknown response: {response}";
                    break;
            }
        }

        private static void AddObjectChild(TreeViewItem item, JsonProperty property)
        {
            foreach (var childProperty in property.Value.EnumerateObject())
            {
                item.Items.Add(CreateTreeItem(childProperty));
            }
        }

        private static void AddArrayChild(TreeViewItem item, JsonElement element)
        {
            var index = 0;

            foreach (var childElement in element.EnumerateArray())
            {
                var arrayIndex = new TreeViewItem() { Header = $"[{index}]" };
                item.Items.Add(arrayIndex);

                switch (childElement.ValueKind)
                {
                    case JsonValueKind.Array:
                        AddArrayChild(arrayIndex, childElement);
                        break;
                    case JsonValueKind.Object:
                        foreach (var childProperty in childElement.EnumerateObject())
                        {
                            arrayIndex.Items.Add(CreateTreeItem(childProperty));
                        }
                        break;
                    default:
                        arrayIndex.Header = $"{arrayIndex.Header}: {childElement.GetRawText()}";
                        break;
                }

                index++;
            }
        }

        private static TreeViewItem CreateTreeItem(JsonProperty property)
        {
            TreeViewItem item = new TreeViewItem();

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Array:
                    item.Header = property.Name;
                    AddArrayChild(item, property.Value);
                    break;
                case JsonValueKind.Object:
                    item.Header = property.Name;
                    AddObjectChild(item, property);
                    break;
                case JsonValueKind.Null:
                    item.Header = $"{property.Name} : None";
                    break;
                default:
                    item.Header = $"{property.Name} : {property.Value.GetRawText() }";
                    break;
            }

            return item;
        }

        private object GetData(IShareIntraModule intraModule)
        {
            return intraModule switch
            {
                StandardShareIntraModule stdModule =>
                    stdModule.Argument switch
                    {
                        //StandardArgument
                        _ => tbInfo.Text = $"Unexpected Standard Module Response Argument: {stdModule.Argument}",
                    },
                AVShareIntraModule avModule =>
                    avModule.Argument switch
                    {
                        AVArgument.SetCodecInfo codecInfo => codecInfo.Item,
                        AVArgument.SetMediaInfo mediaInfo => mediaInfo.Item,
                        _ => tbInfo.Text = $"Unexpected AV Module Response Argument: {avModule.Argument}",
                    },
                AWSShareIntraModule awsModule =>
                    awsModule.Argument switch
                    {
                        _ => tbInfo.Text = $"Unexpected AWS Module Response Argument: {awsModule.Argument}",
                    },
                _ => tbInfo.Text = $"Unknown module: {nameof(intraModule)}"
            };
        }

        /// <summary>
        /// Suggest task functions that generate the requested output
        /// </summary>
        /// <param name="response"></param>
        private void HandleRequestArgument(TaskResponse.RequestArgument response)
        {
            var defaultRepresentation = DefaultRepresentationGenerator.GetRepresentationFor(response.Item);
            if (defaultRepresentation is object)
            {
                // generate a UI for the underlying type of this request
                var jsonDoc = JsonDocument.Parse(defaultRepresentation);
                var elements = jsonDoc.RootElement.EnumerateObject().Select(property => property.Value).ToArray();
                this.requestModuleName = elements[0].GetString();
                this.requestResponseName = elements[2].GetString();
                var valueElement = elements[3];
                tbInfo.Text = elements[4].GetString();
                var controlEnabled = false;
                switch (valueElement.ValueKind)
                {
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        // show a control to capture a boolean
                        ShowBooleanControlContainer.Visibility = Visibility.Visible;
                        controlEnabled = true;
                        break;
                    case JsonValueKind.Number:
                        // show a control to capture a string
                        ShowNumericalControlContainer.Visibility = Visibility.Visible;
                        controlEnabled = true;
                        break;
                    case JsonValueKind.String:
                        // show a control to capture a string
                        ShowTextControlContainer.Visibility = Visibility.Visible;
                        controlEnabled = true;
                        break;
                    default:
                        tbInfo.Text = $"Unknown request: {response}";
                        break;
                }

                if (controlEnabled)
                {
                    ShowControlsContainer.Visibility = Visibility.Visible;
                }
            }
            else
            {
                tbInfo.Text = $"Unknown request: {response}";

                var serviceProvider = (Application.Current as App).ServiceProvider;
                var taskFunctionResolver = serviceProvider.GetRequiredService<TaskFunctionResolver>();

                var suggestedTaskFunctions = taskFunctionResolver.FindTaskFunctionsWithOutput(response.Item).ToArray();

                icSuggestedTaskFunctions.ItemsSource = suggestedTaskFunctions;

                innerBorderContainer.Width = 400.0;
                SuggestTaskFunctionsContainer.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Display the serialized response as a tree of properties
        /// </summary>
        /// <param name="response"></param>
        private void HandleSetArgument(TaskResponse.SetArgument response)
        {
            object data = GetData(response.Item);

            // set the title
            tbInfo.Text = data.GetType().Name;

            // serialize the object and load into JsonDocument
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            var serialized = JsonSerializer.Serialize(data, options);
            var jsonDoc = JsonDocument.Parse(serialized);

            // display the document as a tree
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                var item = CreateTreeItem(property);
                tvData.Items.Add(item);
            }

            DisplayObjectContainer.Visibility = Visibility.Visible;
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
                typeof(DefaultResponseHandler),
                new PropertyMetadata((ICommand)null,
                new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefaultResponseHandler ctrl = (DefaultResponseHandler)d;
            ctrl.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        public object CommandParameter
        {
            get;
            internal set;
        }

        #endregion

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

        private void Select_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Select_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedTaskPath = (e.OriginalSource as Button).Content as string;

            var data = JsonSerializer.SerializeToUtf8Bytes(selectedTaskPath);

            CommandParameter = new UITaskArguments(UITaskMode.InsertTask, "", "", data);

            ExecuteCommand();
        }

        private void Continue_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Continue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var visibleControl =
                new StackPanel[] { ShowBooleanControlContainer, ShowNumericalControlContainer, ShowTextControlContainer }
                .First(panel => panel.Visibility == Visibility.Visible);

            var data = visibleControl.Name switch
            {
                "ShowBooleanControlContainer" => JsonSerializer.SerializeToUtf8Bytes(rbControl.IsChecked ?? false),
                "ShowNumericalControlContainer" => JsonSerializer.SerializeToUtf8Bytes(nudControl.Value),
                "ShowTextControlContainer" => JsonSerializer.SerializeToUtf8Bytes(txtControl.Text),
                _ => throw new NotImplementedException()
            };

            CommandParameter = new UITaskArguments(UITaskMode.SetArgument, this.requestModuleName, this.requestResponseName, data);

            ExecuteCommand();
        }
    }

    public static class DefaultResponseHandlerCommands
    {
        public static readonly RoutedUICommand Select = new RoutedUICommand
            (
                "Select",
                "Select",
                typeof(DefaultResponseHandlerCommands),
                null
            );

        public static readonly RoutedUICommand Continue = new RoutedUICommand
            (
                "Continue",
                "Continue",
                typeof(DefaultResponseHandlerCommands),
                null
            );
    }
}