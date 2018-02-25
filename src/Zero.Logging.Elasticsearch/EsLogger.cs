using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Zero.Logging.Commom;

namespace Zero.Logging.Elasticsearch
{
    public class EsLogger : ILogger
    {
        private readonly BatchingLoggerProvider _provider;
        private readonly string _category;

        public EsLogger(BatchingLoggerProvider loggerProvider, string categoryName)
        {
            _provider = loggerProvider;
            _category = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _provider.IsEnabled;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(DateTimeOffset.Now, logLevel, eventId, state, exception, formatter);
        }

        public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var jsonData = new { timestamp = timestamp, level = logLevel.ToString(), category = _category, message = formatter(state, exception), exceptions = new List<ExceptionModel>() };
            if (exception != null)
            {
                WriteSingleException(jsonData.exceptions, exception, 0);
            }
            _provider.AddMessage(timestamp, Newtonsoft.Json.JsonConvert.SerializeObject(jsonData));
        }

        private void WriteException(List<ExceptionModel> exceptionList, Exception exception, int depth)
        {
            WriteSingleException(exceptionList, exception, depth);
            if (exception.InnerException != null && depth < 20)
                WriteException(exceptionList, exception.InnerException, ++depth);
        }

        private void WriteSingleException(dynamic exceptionList, Exception exception, int depth)
        {
            exceptionList.Add(new ExceptionModel
            {
                depth = depth,
                message = exception.Message,
                source = exception.Source,
                stackTrace = exception.StackTrace,
                hResult = exception.HResult,
                helpLink = exception.HelpLink
            });
        }

        internal class ExceptionModel
        {
            public int depth { get; set; }
            public string message { get; set; }
            public string source { get; set; }
            public string stackTrace { get; set; }
            public int hResult { get; set; }
            public string helpLink { get; set; }
        }
    }
}
