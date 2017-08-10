using System;
using System.Linq;

namespace Zero.Logging.File.Internal
{
    public class RollingFrequency
    {
        public static readonly RollingFrequency Date = new RollingFrequency("Date", "yyyyMMdd", TimeSpan.FromDays(1));
        public static readonly RollingFrequency Hour = new RollingFrequency("Hour", "yyyyMMddHH", TimeSpan.FromHours(1));

        public string Name { get; }
        public string Format { get; }
        public TimeSpan Interval { get; }

        RollingFrequency(string name, string format, TimeSpan interval)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Format = format ?? throw new ArgumentNullException(nameof(format));
            Name = "{" + name + "}";
            Interval = interval;
        }

        public DateTime GetCurrentCheckpoint(DateTime instant)
        {
            if (this == Hour)
            {
                return instant.Date.AddHours(instant.Hour);
            }
            return instant.Date;
        }

        public DateTime GetNextCheckpoint(DateTime instant) => GetCurrentCheckpoint(instant).Add(Interval);

        public static bool TryGetRollingFrequency(string pathTemplate, out RollingFrequency specifier)
        {
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            var frequencies = new[] { Date, Hour }.Where(s => pathTemplate.Contains(s.Name)).ToArray();
            specifier = frequencies.LastOrDefault();
            return specifier != null;
        }
    }
}