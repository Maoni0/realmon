using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System;
using System.Text;

namespace realmon.Utilities
{
    internal static class PrintUtilities
    {
        public const string HeapStatsLineSeparator = "------------------------------------------------------------------------------";

        /// <summary>
        /// Gets the header of the monitor table based on the configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the line separator based on the configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static string GetLineSeparator(Configuration.Configuration configuration)
        {
            // GC# = 3 + alignment(index) 
            int repeatCount = 5; // GC#, | and extra 3 spaces.
            repeatCount += ColumnInfoMap.Map["index"].Alignment;

            foreach(var column in configuration.Columns)
            {
                if (ColumnInfoMap.Map.TryGetValue(column, out var columnInfo))
                {
                    repeatCount += columnInfo.Alignment + 3; // +3 is for the | and the enclosing space.
                }
            }

            string lineSeparator = "";
            for(int i = 0; i < repeatCount; i++)
            {
                lineSeparator = string.Concat(lineSeparator, "-");
            }

            return lineSeparator;
        }

        /// <summary>
        /// Helper method that returns the format based on the column name.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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

        /// <summary>
        /// Gets the string that'll be printed based on a trace event and the configuration.
        /// </summary>
        /// <param name="traceEvent"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Helper method that returns the format based on the column name and trace event.
        /// </summary>
        /// <param name="traceEvent"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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
