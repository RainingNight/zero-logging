using System;
using Microsoft.Extensions.Logging;
using Zero.Logging.Commom;

namespace Zero.Logging.File
{
    public class FileLoggerOptions : BatchingLoggerOptions
    {
        private long? _fileSizeLimit = 1L * 1024 * 1024 * 1024;
        private int? _retainedFileCountLimit = 31; // A long month of logs
        private string _logDirectory = "logs";
        private string _fileName = "log";

        /// <summary>
        /// Gets or sets value indicating if logger accepts and queues writes.
        /// </summary>
        public bool IsEnabledBatching { get; set; } = true;

        /// <summary>
        /// Gets or sets value indicating log write directory.
        /// </summary>
        public string LogDirectory
        {
            get { return _logDirectory; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(nameof(value));
                }
                _logDirectory = value;
            }
        }

        /// <summary>
        /// Gets or sets value indicating log write file name.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(nameof(value));
                }
                _fileName = value;
            }
        }

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum log size in bytes or null for no limit.
        /// Once the log is full, no more messages will be appended.
        /// Defaults to <c>1GB</c>.
        /// </summary>
        public long? FileSizeLimit
        {
            get { return _fileSizeLimit; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FileSizeLimit)} must be positive.");
                }
                _fileSizeLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum retained file count or null for no limit.
        /// Defaults to <c>31</c>.
        /// </summary>
        public int? RetainedFileCountLimit
        {
            get { return _retainedFileCountLimit; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(RetainedFileCountLimit)} must be positive.");
                }
                _retainedFileCountLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the frequency at which the log file should roll.
        /// </summary>
        public RollingIntervalEnum RollingInterval { get; set; }

        /// <summary>
        /// Gets or sets the log filter.
        /// </summary>
        public Func<string, LogLevel, bool> Filter { get; set; }
    }
}