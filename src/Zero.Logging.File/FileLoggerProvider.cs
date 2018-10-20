using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zero.Logging.Batching;

namespace Zero.Logging.File
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : BatchingLoggerProvider
    {
        private readonly string _path;
        private readonly string _fileName;
        private readonly long? _maxFileSize;
        private readonly int? _maxRetainedFiles;
        private readonly RollingIntervalEnum _rollingInterval;

        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options) : base(options)
        {
            var loggerOptions = options.CurrentValue;
            _path = loggerOptions.LogDirectory;
            _fileName = loggerOptions.FileName;
            _maxFileSize = loggerOptions.FileSizeLimit;
            _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
            _rollingInterval = loggerOptions.RollingInterval;
        }

        protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_path);

            foreach (var group in messages.GroupBy(GetGrouping))
            {
                var fullName = Path.Combine(_path, _fileName + "-" + group.Key + ".txt");
                var fileInfo = new FileInfo(fullName);
                if (_maxFileSize > 0 && fileInfo.Exists && fileInfo.Length > _maxFileSize)
                {
                    return;
                }
                using (var streamWriter = System.IO.File.AppendText(fullName))
                {
                    foreach (var item in group)
                    {
                        await streamWriter.WriteAsync(item.Message);
                    }
                    //await streamWriter.FlushAsync();
                }
            }

            RollFiles();
        }

        protected string GetGrouping(LogMessage message)
        {
            return message.Timestamp.ToString(_rollingInterval.GetFormat());
        }

        protected void RollFiles()
        {
            if (_maxRetainedFiles > 0)
            {
                var files = new DirectoryInfo(_path)
                    .GetFiles(_fileName + "*")
                    .OrderByDescending(f => f.Name)
                    .Skip(_maxRetainedFiles.Value);

                foreach (var item in files)
                {
                    item.Delete();
                }
            }
        }
    }
}
