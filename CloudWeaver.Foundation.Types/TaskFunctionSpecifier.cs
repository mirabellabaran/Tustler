using System;
using System.Collections.Generic;
using System.Text;

namespace CloudWeaver.Foundation.Types
{
    /// <summary>
    /// Specifies the name and path (assembly and module) to a task function
    /// </summary>
    public class TaskFunctionSpecifier
    {
        public TaskFunctionSpecifier(string assemblyName, string moduleName, string taskName, bool isRootTask, bool enableLogging)
        {
            this.AssemblyName = assemblyName;
            this.ModuleName = moduleName;
            this.TaskName = taskName;
            this.IsRootTask = isRootTask;
            this.IsLoggingEnabled = enableLogging;
        }

        public string TagDescription => "taskfunction";

        public string AssemblyName { get; }

        public string ModuleName { get; }

        public string TaskName { get; }

        /// <summary>
        /// Root tasks encompass a sequence of sub-tasks
        /// </summary>
        /// <remarks>Setting this to true causes the Agent to pre-evaluate the inputs of the constituent tasks</remarks>
        public bool IsRootTask { get; }

        public bool IsLoggingEnabled { get; }

        public string TaskFullPath
        {
            get
            {
                return $"{ModuleName}.{TaskName}";
            }
        }
    }
}
