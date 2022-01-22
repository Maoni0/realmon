using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using realmon.Configuration.Theme;

namespace realmon.Utilities
{
    internal static class PrintUtilities
    {
        public const string LineSeparator = "------------------------------------------------------------------------------";

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
            foreach (var columnName in configuration.Columns)
            {
                stringBuilder.Append($" {FormatBasedOnColumnName(columnName)} |");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the header of the monitor table based on the configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static List<string> GetHeaderList(Configuration.Configuration configuration)
        {
            List<string> headerColumns = new List<string>(configuration.Columns.Count + 1);

            // Add the `index` column.
            headerColumns.Add(ThemeConfig.ToHeader("GC#"));

            // Iterate through all columns in the config.
            foreach (var columnName in configuration.Columns)
            {
                headerColumns.Add(ThemeConfig.ToHeader(columnName));
            }

            return headerColumns;
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

            foreach (var column in configuration.Columns)
            {
                if (ColumnInfoMap.Map.TryGetValue(column, out var columnInfo))
                {
                    repeatCount += columnInfo.Alignment + 3; // +3 is for the | and the enclosing space.
                }
            }

            string lineSeparator = "";
            for (int i = 0; i < repeatCount; i++)
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
            stringBuilder.Append($"GC#{FormatBasedOnColumnAndGCEvent(traceEvent, "index")} |");

            // Iterate through all columns in the config.
            foreach (var columnName in configuration.Columns)
            {
                stringBuilder.Append($" {FormatBasedOnColumnAndGCEvent(traceEvent, columnName)} |");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the a list of strings that'll be printed based on a trace event and the configuration. Each string in the list is for a column in configuration.Columns.
        /// </summary>
        /// <param name="traceEvent"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static List<string> GetRowDetailsList(TraceGC traceEvent, Configuration.Configuration configuration)
        {
            List<string> rowDetails = new List<string>();
            // Add the `index` column.
            rowDetails.Add($"{FormatThemeBasedOnColumnAndGCEvent(traceEvent, "index")}");

            // Iterate through all columns in the config.
            foreach (var columnName in configuration.Columns)
            {
                rowDetails.Add($" {FormatThemeBasedOnColumnAndGCEvent(traceEvent, columnName)}");
            }

            return rowDetails;
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

            throw new ArgumentException($"Column Name: {columnName} not registered in the ColumnInfoMap.");
        }

        /// <summary>
        /// Returns a formatted string of the GC event in the specified column that uses the color theming defined by <see cref="Theme"/>
        /// </summary>
        /// <param name="traceEvent"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string FormatThemeBasedOnColumnAndGCEvent(TraceGC traceEvent, string columnName)
        {
            string color = traceEvent.Generation switch
            {
                0 => $"[{ThemeConfig.Current.Gen0RowColor}]",
                1 => $"[{ThemeConfig.Current.Gen1RowColor}]",
                2 => $"[{ThemeConfig.Current.Gen2RowColor}]",
                _ => "[]"
            };

            if (ColumnInfoMap.Map.TryGetValue(columnName, out var columnInfo))
            {
                // No alignment here as that is taken care of by the table formatting
                string format = $"{color}{{0" + (string.IsNullOrEmpty(columnInfo.Format) ? string.Empty : $":{columnInfo.Format}") + "}[/]";
                string formattedString = string.Format(format, columnInfo.GetColumnValueFromEvent(traceEvent));
                return formattedString;
            }

            throw new ArgumentException($"Column Name: {columnName} not registered in the ColumnInfoMap.");
        }

        public static int ParseProcessIdFromMultiProcessPrompt(string processDetailsChosenFromMultiProcessPrompt)
        {
            string pidAsString = processDetailsChosenFromMultiProcessPrompt.Split('|')[0].Split(':')[1].Trim();
            return int.Parse(pidAsString);
        }
    }
}
