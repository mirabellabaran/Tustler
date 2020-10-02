using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerServicesLib
{
    /// <summary>
    /// Specifies the name and path (assembly and module) to a task function
    /// </summary>
    public class TaskFunctionSpecifier
    {
        public TaskFunctionSpecifier(string assemblyName, string moduleName, string taskName, bool enableLogging)
        {
            this.AssemblyName = assemblyName;
            this.ModuleName = moduleName;
            this.TaskName = taskName;
            this.IsLoggingEnabled = enableLogging;
        }

        public string TagDescription => "taskfunction";

        public string AssemblyName { get; }

        public string ModuleName { get; }

        public string TaskName { get; }

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
