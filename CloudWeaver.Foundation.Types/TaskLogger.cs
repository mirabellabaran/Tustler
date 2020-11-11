#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CloudWeaver.Foundation.Types
{
    public class TaskLogger :IDisposable
    {
        private const string LogFileName = "log.bin";

        private TaskFunctionSpecifier? taskSpecifier;

        private FileStream? logFile;

        public TaskLogger()
        {
            this.IsLoggingEnabled = false;

            this.taskSpecifier = null;
            this.LogFilePath = null;
            this.logFile = null;
        }

        public string? LoggedTaskName
        {
            get
            {
                return this.taskSpecifier is object? this.taskSpecifier.TaskName : null;
            }
        }

        public FileInfo? LogFilePath
        {
            get;
            internal set;
        }

        public bool IsLoggingEnabled
        {
            get;
            internal set;
        }

        /// <summary>
        /// Start logging on the specified root task
        /// </summary>
        /// <returns>true if logging was enabled</returns>
        /// <remarks>Only root tasks own a folder within the File Cache; this is where the log file is written</remarks>
        public bool StartLogging(string rootFolder, TaskFunctionSpecifier taskSpecifier)
        {
            this.taskSpecifier = taskSpecifier;

            if (this.taskSpecifier is object && this.taskSpecifier.IsLoggingEnabled)
            {
                var logFileName = $"{DateTime.Now.Ticks}-{LogFileName}";
                var filePath = Path.Combine(rootFolder, this.taskSpecifier.TaskName, logFileName);
                this.LogFilePath = new FileInfo(filePath);

                IsLoggingEnabled = true;
            }

            return IsLoggingEnabled;
        }

        public void RestartLogging()
        {
            if (this.taskSpecifier is object && this.taskSpecifier.IsLoggingEnabled && this.LogFilePath is object)
            {
                IsLoggingEnabled = true;
            }
        }

        public void StopLogging()
        {
            if (IsLoggingEnabled)
            {
                CloseLogFile();
            }
        }

        public void AddToLog(byte[] data)
        {
            if (IsLoggingEnabled)   // then LogFilePath is set
            {
                if (logFile is null)
                {
                    // append to the file if it already exists
                    logFile = File.Open(LogFilePath!.FullName, FileMode.Append, FileAccess.Write, FileShare.None);
                }

                logFile.Write(new ReadOnlySpan<byte>(data));
            }
        }

        private void CloseLogFile()
        {
            if (logFile is object)
            {
                logFile.Close();
                logFile = null;
                IsLoggingEnabled = false;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CloseLogFile();
                }

                disposedValue = true;
            }
        }

        ~TaskLogger()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
