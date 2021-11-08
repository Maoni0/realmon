using GCRealTimeMon.Utilities;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System;
using System.Text;

namespace realmon.Utilities
{
    public static class PrintUtilities
    {
        public const string LineSeparator = "------------------------------------------------------------------------------";
        public static string GetHeader(Configuration.Configuration configuration)
        {
            StringBuilder stringBuilder = new StringBuilder();
            // Add the `index` column.
            stringBuilder.Append($"GC#{FormatBasedOnColumnName("index")} |");

            // Iterate through all columns in the config.
            foreach(var columnName in configuration.Columns)
            {
                stringBuilder.Append($" {FormatBasedOnColumnName(columnName)} |");
            }

            return stringBuilder.ToString(); 
        }

        public static string FormatBasedOnColumnName(string columnName)
        {
            if (ColumnInfoMap.Map.TryGetValue(columnName, out var columnInfo))
            {
                string format = "{0," + columnInfo.Alignment + (string.IsNullOrEmpty(columnInfo.Format) ? string.Empty : $":{columnInfo.Format}") + "}";
                string formattedString = string.Format(format, columnName);
                return formattedString;
            }

            throw new ArgumentException($"Column Name: {columnName} not registed in the ColumnInfoMap.");
        }

        public static string GetRowDetails(TraceGC traceEvent, Configuration.Configuration configuration)
        {
            StringBuilder stringBuilder = new StringBuilder();
            // Add the `index` column.
            stringBuilder.Append($"GC#{FormatBasedOnColumnAndGCEvent(traceEvent,"index")} |");

            // Iterate through all columns in the config.
            foreach(var columnName in configuration.Columns)
            {
                stringBuilder.Append($" {FormatBasedOnColumnAndGCEvent(traceEvent, columnName)} |");
            }

            return stringBuilder.ToString();
        }

        public static string FormatBasedOnColumnAndGCEvent(TraceGC traceEvent, string columnName)
        {
            if (ColumnInfoMap.Map.TryGetValue(columnName, out var columnInfo))
            {
                // Full Format: {index, alignment: format}
                string format = "{0," + columnInfo.Alignment + (string.IsNullOrEmpty(columnInfo.Format) ? string.Empty : $":{columnInfo.Format}") + "}";
                string formattedString = string.Format(format, columnInfo.GetColumnValueFromEvent(traceEvent));
                return formattedString;
            }

            throw new ArgumentException($"Column Name: {columnName} not registed in the ColumnInfoMap.");
        }
    }
}
