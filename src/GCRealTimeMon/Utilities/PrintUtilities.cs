using Microsoft.Diagnostics.Tracing.Analysis.GC;

using Spectre.Console;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
            foreach (var columnName in configuration.Columns)
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


        /// <summary>
        /// Gets the header of the monitor table based on the configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static List<string> GetHeader2(Configuration.Configuration configuration)
        {
            List<string> headerColumns = new List<string>(configuration.Columns.Count + 1);

            // Add the `index` column.
            headerColumns.Add($"[bold yellow]GC#[/]");

            // Iterate through all columns in the config.
            foreach (var columnName in configuration.Columns)
            {
                //headerColumns.Add($"{FormatBasedOnColumnName(columnName)}");
                headerColumns.Add($"[bold yellow]{columnName}[/]");
            }

            return headerColumns;
        }

        /// <summary>
        /// Gets the string that'll be printed based on a trace event and the configuration.
        /// </summary>
        /// <param name="traceEvent"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static List<string> GetRowDetails2(TraceGC traceEvent, Configuration.Configuration configuration)
        {
            List<string> rowDetails = new List<string>();
            // Add the `index` column.
            rowDetails.Add($"{FormatBasedOnColumnAndGCEvent2(traceEvent, "index")}");

            // Iterate through all columns in the config.
            foreach (var columnName in configuration.Columns)
            {
                rowDetails.Add($" {FormatBasedOnColumnAndGCEvent2(traceEvent, columnName)}");
            }

            return rowDetails;
        }

        public static string FormatBasedOnColumnAndGCEvent2(TraceGC traceEvent, string columnName)
        {
            string color = traceEvent.Generation switch
            {
                0 => "[skyblue1]",
                1 => "[lightskyblue1]",
                2 => "[deepskyblue1]",
                _ => "[]"
            };

            if (ColumnInfoMap.Map.TryGetValue(columnName, out var columnInfo))
            {
                // Full Format: {index, alignment: format}
                //string format = "{0," + columnInfo.Alignment + (string.IsNullOrEmpty(columnInfo.Format) ? string.Empty : $":{columnInfo.Format}") + "}";
                string format = $"{color}{{0" + (string.IsNullOrEmpty(columnInfo.Format) ? string.Empty : $":{columnInfo.Format}") + "}[/]";
                string formattedString = string.Format(format, columnInfo.GetColumnValueFromEvent(traceEvent));
                return formattedString;
            }

            throw new ArgumentException($"Column Name: {columnName} not registed in the ColumnInfoMap.");
        }
    }

    internal class LiveOutputTable
    {
        private readonly Channel<string[]> channel;
        private readonly Configuration.Configuration configuration;
        private Task runningLiveTable;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cts;

        public LiveOutputTable(Configuration.Configuration configuration)
        {
            channel = Channel.CreateUnbounded<string[]>();
            this.ResetCancellation();
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private void ResetCancellation()
        {
            this.cts?.Dispose();
            this.cts = new CancellationTokenSource();
            this.cancellationToken = cts.Token;
        }

        public void StartLive()
        {
            Table table = new Table();
            AddHeaders(table);
            runningLiveTable = AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                while (!this.cancellationToken.IsCancellationRequested)
                {
                    var newRow = await channel.Reader.ReadAsync(this.cancellationToken);
                    table.AddRow(newRow);
                    ctx.Refresh();
                }
            });
        }

        private void AddHeaders(Table table)
        {
            var header = PrintUtilities.GetHeader2(this.configuration);
            foreach (var column in header)
            {
                table.AddColumn(column);
            }
        }

        internal void WriteRow(TraceGC gc, Configuration.Configuration configuration)
        {
            List<string> rowDetails = PrintUtilities.GetRowDetails2(gc, configuration);
            // todo - bad
            channel.Writer.WriteAsync(rowDetails.ToArray()).AsTask().Wait();
        }

        internal async Task Stop()
        {
            this.cts.Cancel();
            try
            {
                await runningLiveTable;
            }
            catch (OperationCanceledException)
            {
            }
        }

        internal void Restart()
        {
            this.ResetCancellation();
            StartLive();
        }
    }
}
