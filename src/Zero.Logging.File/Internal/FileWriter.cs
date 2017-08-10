using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Logging.File.Internal
{
    /// <summary>
    /// Write log message to a disk file.
    /// </summary>
    public class FileWriter : IMessageWriter, IDisposable
    {
        private readonly long? _maxFileSize;
        private readonly FileStream _underlyingStream;
        private readonly TextWriter _output;

        public FileWriter(string path, long? fileSizeLimit = null)
        {
            _maxFileSize = fileSizeLimit;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _underlyingStream = System.IO.File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _output = new StreamWriter(_underlyingStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public async Task WriteMessagesAsync(string message, CancellationToken cancellationToken)
        {
            if (_maxFileSize > 0 && _underlyingStream.Length > _maxFileSize)
            {
                return;
            }
            await _output.WriteAsync(message);
            FlushToDisk();
        }

        public void FlushToDisk()
        {
            _output.Flush();
            _underlyingStream.Flush(true);
        }

        public void Dispose()
        {
            _output.Dispose();
            _underlyingStream.Dispose();
        }
    }
}
