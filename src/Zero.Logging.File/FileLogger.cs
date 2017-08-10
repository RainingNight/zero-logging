using Microsoft.Extensions.Logging;
using System;
using System.Text;
using Zero.Logging.File.Internal;

namespace Zero.Logging.File
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _category;
        public IMessageWriter _writer;
        private Func<string, LogLevel, bool> _filter;

        public FileLogger(IMessageWriter writer, string category, Func<string, LogLevel, bool> filter)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _category = category ?? throw new ArgumentNullException(nameof(category));
            Filter = filter ?? ((cate, loglv) => true);
        }

        public Func<string, LogLevel, bool> Filter
        {
            get { return _filter; }
            set
            {
                _filter = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            var builder = new StringBuilder();
            builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            builder.Append(" [");
            builder.Append(GetLogLevelString(logLevel));
            builder.Append("] ");
            builder.Append(_category);
            builder.Append("[");
            builder.Append(eventId);
            builder.Append("]");
            builder.Append(": ");
            builder.AppendLine(formatter(state, exception));
            if (exception != null)
            {
                builder.AppendLine(exception.ToString());
            }
            _writer.WriteMessagesAsync(builder.ToString()).Wait();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Filter(_category, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            return LogScope.Push(_category, state);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}