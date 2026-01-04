using System;
using System.IO;
using System.Text;

namespace MESUploadSystem.Services
{
    public static class LogService
    {
        public static event Action<string, bool> OnLog; // message, isError
        private static readonly object _lock = new object();
        private static readonly string LogDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Logs");

        public static void Log(string message, bool isError = false)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            OnLog?.Invoke(logMessage, isError);

            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(LogDir))
                        Directory.CreateDirectory(LogDir);

                    var logFile = Path.Combine(LogDir, $"{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(logFile, logMessage + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        public static void Error(string message) => Log(message, true);
        public static void Info(string message) => Log(message, false);
    }
}