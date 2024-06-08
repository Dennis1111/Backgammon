using Serilog;
using System.Globalization;

namespace Backgammon.Util
{
    public static class LogManager
    {
        public static void ClearOldLogFiles(string logDirectory, string logFilePrefix, int retentionDays)
        {
            var now = DateTime.UtcNow;
            foreach (var filePath in Directory.GetFiles(logDirectory, $"{logFilePrefix}-*.txt"))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var datePart = fileName.Substring(logFilePrefix.Length + 1); // Adjust based on your actual prefix
                if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, DateTimeStyles.None, out var fileDate))
                {
                    if ((now - fileDate).TotalDays > retentionDays)
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }

        public static ILogger CreateLogger(string logFilePath)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
