using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Zero.Logging.File.Internal
{
    public class RollingFilePath
    {
        const string DefaultSeparator = "-";

        const string SpecifierMatchGroup = "specifier";
        const string SequenceNumberMatchGroup = "sequence";

        readonly string _pathTemplate;
        readonly Regex _filenameMatcher;
        readonly RollingFrequency _frequency = null;

        // Rolls files based on the current date, using a path formatting pattern like: Logs/log-{Date}.txt
        public RollingFilePath(string pathTemplate)
        {
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));

            var filenameTemplate = Path.GetFileName(pathTemplate);
            if (!RollingFrequency.TryGetRollingFrequency(filenameTemplate, out _frequency))
            {
                _frequency = RollingFrequency.Date;
                filenameTemplate = Path.GetFileNameWithoutExtension(filenameTemplate) + DefaultSeparator +
                    _frequency.Name + Path.GetExtension(filenameTemplate);
            }

            var indexOfSpecifier = filenameTemplate.IndexOf(_frequency.Name, StringComparison.Ordinal);
            var prefix = filenameTemplate.Substring(0, indexOfSpecifier);
            var suffix = filenameTemplate.Substring(indexOfSpecifier + _frequency.Name.Length);
            _filenameMatcher = new Regex(
                "^" +
                Regex.Escape(prefix) +
                "(?<" + SpecifierMatchGroup + ">\\d{" + _frequency.Format.Length + "})" +
                "(?<" + SequenceNumberMatchGroup + ">_[0-9]{3,}){0,1}" +
                Regex.Escape(suffix) +
                "$");

            FileSearchPattern = filenameTemplate.Replace(_frequency.Name, "*");
            var directory = Path.GetDirectoryName(pathTemplate);
            if (string.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();
            else
                directory = Path.GetFullPath(directory);
            LogFileDirectory = directory;
            _pathTemplate = Path.Combine(LogFileDirectory, filenameTemplate);
        }

        public string LogFileDirectory { get; }

        public string FileSearchPattern { get; }

        public string GetLogFilePath(DateTime date, int sequenceNumber)
        {
            var currentCheckpoint = GetCurrentCheckpoint(date);
            var tok = currentCheckpoint.ToString(_frequency.Format, CultureInfo.InvariantCulture);
            if (sequenceNumber != 0)
                tok += "_" + sequenceNumber.ToString("000", CultureInfo.InvariantCulture);
            return _pathTemplate.Replace(_frequency.Name, tok);
        }

        public IEnumerable<RollingFile> SelectMatches(IEnumerable<string> filenames)
        {
            foreach (var filename in filenames)
            {
                var match = _filenameMatcher.Match(filename);
                if (match.Success)
                {
                    var inc = 0;
                    var incGroup = match.Groups[SequenceNumberMatchGroup];
                    if (incGroup.Captures.Count != 0)
                    {
                        var incPart = incGroup.Captures[0].Value.Substring(1);
                        inc = int.Parse(incPart, CultureInfo.InvariantCulture);
                    }

                    var dateTimePart = match.Groups[SpecifierMatchGroup].Captures[0].Value;
                    if (!DateTime.TryParseExact(
                        dateTimePart,
                        _frequency.Format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime dateTime))
                        continue;

                    yield return new RollingFile(filename, dateTime, inc);
                }
            }
        }

        public DateTime GetCurrentCheckpoint(DateTime instant) => _frequency.GetCurrentCheckpoint(instant);

        public DateTime GetNextCheckpoint(DateTime instant) => _frequency.GetNextCheckpoint(instant);
    }
}