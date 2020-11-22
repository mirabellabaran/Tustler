using CloudWeaver.Types;
using System;

namespace TustlerUIShared
{
    /// <summary>
    /// An 'enum' of possible modes; SetArgument and RestartTask modes take an argument
    /// </summary>
    public class UITaskMode
    {
        private UITaskMode(int tag)
        {
            Tag = tag;
        }

        public static UITaskMode SelectTask { get { return new UITaskMode(UITaskMode.Tags.SelectTask); } }
        public static UITaskMode InsertTask { get { return new UITaskMode(UITaskMode.Tags.InsertTask); } }
        public static UITaskMode TransformSetArgument { get { return new UITaskMode(UITaskMode.Tags.TransformSetArgument); } }
        public static UITaskMode SelectDefaultArguments { get { return new UITaskMode(UITaskMode.Tags.SelectDefaultArguments); } }
        public static UITaskMode Continue { get { return new UITaskMode(UITaskMode.Tags.Continue); } }
        public static UITaskMode ForEachIndependantTask { get { return new UITaskMode(UITaskMode.Tags.ForEachIndependantTask); } }

        public int Tag { get; }

        public static class Tags
        {
            public const int SelectTask = 0;             // choose a task from a list of tasks
            public const int RestartTask = 1;            // restart a completed task
            public const int InsertTask = 2;             // add a task before the current one
            public const int SetArgument = 3;            // set an argument on the agent
            public const int TransformSetArgument = 4;   // transform the argument and then set the argument on the agent
            public const int SelectDefaultArguments = 5; // select which of the default arguments should be passed to the agent
            public const int Continue = 6;               // continue running the task following a prompt
            public const int ForEachIndependantTask = 7;  // collect a list of independant tasks to run
        }

        public static UITaskMode NewSetArgument(IRequestIntraModule request)
        {
            return new UITaskMode.SetArgument(request, UITaskMode.Tags.SetArgument);
        }

        public class SetArgument : UITaskMode
        {
            internal SetArgument(IRequestIntraModule request, int tag) : base(tag)
            {
                Item = request;
            }

            public IRequestIntraModule Item { get; private set; }
        }

        public static UITaskMode NewRestartTask(IRequestIntraModule request)
        {
            return new UITaskMode.RestartTask(request, UITaskMode.Tags.RestartTask);
        }

        public class RestartTask : UITaskMode
        {
            internal RestartTask(IRequestIntraModule request, int tag) : base(tag)
            {
                Item = request;
            }

            public IRequestIntraModule Item { get; private set; }
        }
    }

    //public enum UITaskMode
    //{
    //    SelectTask,             // choose a task from a list of tasks
    //    RestartTask,            // restart a completed task
    //    InsertTask,             // add a task before the current one
    //    SetArgument,            // set an argument on the agent
    //    TransformSetArgument,   // transform the argument and then set the argument on the agent
    //    SelectDefaultArguments, // select which of the default arguments should be passed to the agent
    //    Continue,               // continue running the task following a prompt
    //    ForEachIndependantTask  // collect a list of independant tasks to run
    //}

    /// <summary>
    /// Defines an underlying type or argument that is being set via the UI
    /// </summary>
    public class UITaskArguments
    {
        public UITaskArguments(UITaskMode mode, string moduleName, string propertyName, byte[] argument)
        {
            TaskMode = mode;
            ModuleName = moduleName;
            PropertyName = propertyName;
            SerializedArgument = argument;
        }

        public UITaskMode TaskMode { get; }

        /// <summary>
        /// The name of the CloudWeaver module that contains the underlying type
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        /// The name of the argument being set
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The UTF8-encoded serialized argument
        /// </summary>
        public byte[] SerializedArgument { get; }
    }
}
