using System;

namespace TustlerUIShared
{
    public enum UITaskMode
    {
        SelectTask,             // choose a task from a list of tasks
        RestartTask,            // restart a completed task
        SetArgument,            // set an argument on the agent
        SelectDefaultArguments, // select which of the default arguments should be passed to the agent
        Continue,               // continue running the task following a prompt
        ForEachIndependantTask  // collect a list of independant tasks to run
    }

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
