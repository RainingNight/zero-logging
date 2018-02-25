using System;

namespace Zero.Logging.File
{
    internal static class RollingIntervalEnumExtensions
    {
        public static string GetFormat(this RollingIntervalEnum interval)
        {
            switch (interval)
            {
                case RollingIntervalEnum.Year:
                    return "yyyy";
                case RollingIntervalEnum.Month:
                    return "yyyyMM";
                case RollingIntervalEnum.Day:
                    return "yyyyMMdd";
                case RollingIntervalEnum.Hour:
                    return "yyyyMMddHH";
                case RollingIntervalEnum.Minute:
                    return "yyyyMMddHHmm";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }
    }
}
