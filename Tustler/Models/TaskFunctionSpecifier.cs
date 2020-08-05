using CloudWeaver.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tustler.Models
{
    /// <summary>
    /// Specifies the name and path (assembly and module) to a task function
    /// </summary>
    public class TaskFunctionSpecifier : IElementTag
    {
        public TaskFunctionSpecifier(string assemblyName, string moduleName, string taskName)
        {
            this.AssemblyName = assemblyName;
            this.ModuleName = moduleName;
            this.TaskName = taskName;
        }

        public string TagDescription => "taskfunction";

        public string AssemblyName { get; }

        public string ModuleName { get; }

        public string TaskName { get; }

        public string TaskFullPath
        {
            get
            {
                return $"{ModuleName}.{TaskName}";
            }
        }

        public static string FullPathFromTaskItem(TaskItem task)
        {
            if (task is object)
                return $"{task.ModuleName}.{task.TaskName}";
            else
                throw new ArgumentException("Expecting a task item");
        }
    }
}
