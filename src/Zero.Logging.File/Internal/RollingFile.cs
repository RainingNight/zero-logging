using System;

namespace Zero.Logging.File.Internal
{
    public class RollingFile
    {
        public RollingFile(string filename, DateTime dateTime, int sequenceNumber)
        {
            Filename = filename;
            DateTime = dateTime;
            SequenceNumber = sequenceNumber;
        }

        public string Filename { get; }

        public DateTime DateTime { get; }

        public int SequenceNumber { get; }
    }
}