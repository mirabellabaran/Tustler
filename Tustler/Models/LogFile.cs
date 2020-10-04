﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Tustler.Models
{
    /// <summary>
    /// Represents the log file generated by running a task function
    /// </summary>
    public class LogFile
    {
        public string FilePath { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
    }
}