using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Zero.Logging.File.Internal
{
    public class FileLoggerConfigureOptions : IConfigureOptions<FileLoggerOptions>
    {
        private readonly IConfiguration _switches;
        private static readonly Func<string, LogLevel, bool> falseFilter = (cat, level) => false;

        public FileLoggerConfigureOptions(IConfiguration configuration)
        {
            _switches = configuration.GetSection("LogLevel");
        }

        public void Configure(FileLoggerOptions options)
        {
            options.Filter = (n, l) =>
            {
                foreach (var prefix in GetKeyPrefixes(n))
                {
                    if (TryGetSwitch(prefix, out LogLevel level))
                    {
                        return l >= level;
                    }
                }
                return true;
            };
        }

        private bool TryGetSwitch(string name, out LogLevel level)
        {
            if (_switches == null)
            {
                level = LogLevel.None;
                return false;
            }

            var value = _switches[name];
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse<LogLevel>(value, true, out level))
            {
                return true;
            }
            else
            {
                var message = $"Configuration value '{value}' for category '{name}' is not supported.";
                throw new InvalidOperationException(message);
            }
        }

        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }
    }
}