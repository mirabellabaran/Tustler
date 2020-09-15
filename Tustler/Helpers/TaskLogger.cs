#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tustler.Models;

namespace Tustler.Helpers
{
    public class TaskLogger :IDisposable
    {
        private const string LogFileName = "log.bin";

        private FileStream? logFile;
        private TaskFunctionSpecifier? taskSpecifier;

        public TaskLogger()
        {
            this.logFile = null;
            this.taskSpecifier = null;
            this.IsLoggingEnabled = false;
        }

        public bool IsLoggingEnabled
        {
            get;
            internal set;
        }

        /// <summary>
        /// Start logging on the specified root task
        /// </summary>
        /// <param name="specifier">The specification of a root task (may or may not reference sub-tasks)</param>
        /// <returns>true if logging was enabled</returns>
        /// <remarks>Only root tasks own a folder within the File Cache; this is where the log file is written</remarks>
        public bool StartLogging(TaskFunctionSpecifier specifier)
        {
            if (specifier is object && specifier.IsLoggingEnabled)
            {
                this.taskSpecifier = specifier;
                IsLoggingEnabled = true;
            }

            return IsLoggingEnabled;
        }

        public void StopLogging()
        {
            this.taskSpecifier = null;
            CloseLogFile();
        }

        public void AddToLog(byte[] data)
        {
            if (IsLoggingEnabled)   // then taskSpecifier must be set
            {
                if (logFile is null)
                {
                    var logFileName = $"{DateTime.Now.Ticks}-{LogFileName}";
                    var logFilePath = Path.Combine(TustlerServicesLib.ApplicationSettings.FileCachePath, this.taskSpecifier!.TaskName, logFileName);
                    logFile = File.Open(logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
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

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

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
