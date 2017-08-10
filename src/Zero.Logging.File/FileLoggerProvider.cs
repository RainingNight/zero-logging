using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Zero.Logging.File.Internal;

namespace Zero.Logging.File
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        private IMessageWriter _msgWriter;
        private readonly IDisposable _optionsChangeToken;
        private readonly Func<string, LogLevel, bool> _filter;

        private static readonly Func<string, LogLevel, bool> trueFilter = (cat, level) => true;

        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
        {
            _optionsChangeToken = options.OnChange(UpdateOptions);
            UpdateOptions(options.CurrentValue);
        }

        public FileLoggerProvider(FileLoggerOptions options)
        {
            _filter = options.Filter ?? trueFilter;
            UpdateOptions(options);
        }

        private void UpdateOptions(FileLoggerOptions options)
        {
            if (RollingFrequency.TryGetRollingFrequency(options.Path, out var r))
            {
                _msgWriter = new RollingFileWriter(options.Path, options.FileSizeLimit, options.RetainedFileCountLimit);
            }
            else
            {
                _msgWriter = new FileWriter(options.Path, options.FileSizeLimit);
            }
            if (options.IsEnabledBatching)
            {
                _msgWriter = new BatchingWriter(_msgWriter, options.FlushPeriod, options.BatchSize, options.BackgroundQueueSize);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_msgWriter, categoryName, _filter);
        }

        public void Dispose()
        {
            _optionsChangeToken?.Dispose();
        }
    }
}