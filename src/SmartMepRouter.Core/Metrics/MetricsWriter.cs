using System;
using System.IO;
using System.Text.Json;

namespace SmartMepRouter.Core.Metrics
{
    /// <summary>
    /// Serializes and writes run metrics JSON records per FR-14.
    /// </summary>
    public static class MetricsWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string ToJson(RunMetrics metrics)
        {
            return JsonSerializer.Serialize(metrics, JsonOptions);
        }

        public static void WriteToFile(RunMetrics metrics, string filePath)
        {
            string json = ToJson(metrics);
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, json);
        }

        public static string WriteToDefaultDirectory(RunMetrics metrics, string? baseDirectory = null)
        {
            string appData = baseDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SmartMepRouter",
                "runs");

            string fileName = $"run_{metrics.TimestampUtc:yyyyMMdd_HHmmss}_{metrics.RunId.Substring(0, 8)}.json";
            string fullPath = Path.Combine(appData, fileName);
            WriteToFile(metrics, fullPath);
            return fullPath;
        }
    }
}
