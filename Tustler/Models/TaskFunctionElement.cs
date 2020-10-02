using CloudWeaver.Types;
using System;
using System.Collections.Generic;
using System.Text;
using TustlerServicesLib;

namespace Tustler.Models
{
    /// <summary>
    /// Specifies the name and path (assembly and module) to a task function
    /// </summary>
    public class TaskFunctionElement : IElementTag
    {
        public TaskFunctionElement(TaskFunctionSpecifier specifier)
        {
            this.TaskFunctionSpecifier = specifier;
        }

        public string TagDescription => "taskfunction";

        public string TaskName {
            get
            {
                return TaskFunctionSpecifier.TaskName;
            }
        }

        internal TaskFunctionSpecifier TaskFunctionSpecifier { get; }
    }
}
