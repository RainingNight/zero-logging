using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Logging.File.Internal
{
    public class RollingFileWriter : IMessageWriter, IDisposable
    {
        private readonly RollingFilePath _roller;
        private readonly long? _maxfileSizeLimit;
        private readonly int? _maxRetainedFiles;

        private DateTime? _nextCheckpoint;
        private IMessageWriter _currentFileWriter;

        public RollingFileWriter(string pathFormat, long? maxFileSizeLimit = null, int? maxRetainedFiles = null)
        {
            _roller = new RollingFilePath(pathFormat);
            _maxfileSizeLimit = maxFileSizeLimit;
            _maxRetainedFiles = maxRetainedFiles;
        }

        public Task WriteMessagesAsync(string message, CancellationToken cancellationToken)
        {
            AlignFileWriter();
            return _currentFileWriter.WriteMessagesAsync(message, cancellationToken);
        }

        private void AlignFileWriter()
        {
            DateTime now = DateTime.Now;
            if (!_nextCheckpoint.HasValue)
            {
                OpenFileWriter(now);
            }
            else if (now >= _nextCheckpoint.Value)
            {
                CloseFileWriter();
                OpenFileWriter(now);
            }
        }

        private void OpenFileWriter(DateTime now)
        {
            var currentCheckpoint = _roller.GetCurrentCheckpoint(now);
            _nextCheckpoint = _roller.GetNextCheckpoint(now);

            var existingFiles = Enumerable.Empty<string>();
            try
            {
                existingFiles = Directory.GetFiles(_roller.LogFileDirectory, _roller.FileSearchPattern).Select(Path.GetFileName);
            }
            catch (DirectoryNotFoundException) { }

            var latestForThisCheckpoint = _roller
                .SelectMatches(existingFiles)
                .Where(m => m.DateTime == currentCheckpoint)
                .OrderByDescending(m => m.SequenceNumber)
                .FirstOrDefault();

            var sequence = latestForThisCheckpoint != null ? latestForThisCheckpoint.SequenceNumber : 0;

            const int maxAttempts = 3;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                string path = _roller.GetLogFilePath(now, sequence);
                try
                {
                    _currentFileWriter = new FileWriter(path, _maxfileSizeLimit);
                }
                catch (IOException)
                {
                    sequence++;
                    continue;
                }
                RollFiles(path);
                return;
            }
        }

        private void RollFiles(string currentFilePath)
        {
            if (_maxRetainedFiles > 0)
            {
                var potentialMatches = Directory.GetFiles(_roller.LogFileDirectory, _roller.FileSearchPattern)
                    .Select(Path.GetFileName);
                var moveFiles = _roller
                    .SelectMatches(potentialMatches)
                    .OrderByDescending(m => m.DateTime)
                    .ThenByDescending(m => m.SequenceNumber)
                    .Skip(_maxRetainedFiles.Value)
                    .Select(m => m.Filename);
                foreach (var obsolete in moveFiles)
                {
                    System.IO.File.Delete(Path.Combine(_roller.LogFileDirectory, obsolete));
                }
            }
        }

        private void CloseFileWriter()
        {
            if (_currentFileWriter != null)
            {
                _currentFileWriter.Dispose();
                _currentFileWriter = null;
            }
            _nextCheckpoint = null;
        }

        public void Dispose()
        {
            if (_currentFileWriter == null) return;
            CloseFileWriter();
        }
    }
}
