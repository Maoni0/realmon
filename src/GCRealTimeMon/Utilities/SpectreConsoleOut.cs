namespace realmon.Utilities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Diagnostics.Tracing.Analysis.GC;
    using realmon.Configuration;
    using realmon.Configuration.Theme;
    using Spectre.Console;

    /// <summary>
    /// Utility for writing formatted console output
    /// </summary>
    internal class SpectreConsoleOut : IConsoleOut
    {
        LiveOutputTable liveOutputTable;

        public SpectreConsoleOut(Configuration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            liveOutputTable = new LiveOutputTable(configuration);
        }

        public void WriteProcessInfo(string processName, int pid)
        {
            WriteRule($"[{ThemeConfig.Current.MessageColor}]Monitoring process with name: [{ThemeConfig.Current.HighlightColor}]{processName}[/] and pid: [{ThemeConfig.Current.HighlightColor}]{pid}[/][/]");
        }

        public void WriteTableHeaders() => liveOutputTable.Start();

        public void WriteRow(TraceGC gc) => liveOutputTable.WriteRow(gc);

        /// <summary>
        /// Writes a horizontal rule line with a message in the center to the console.
        /// </summary>
        /// <param name="ruleMessage">The message to put in the center of the rule line.</param>
        public void WriteRule(string ruleMessage)
        {
            AnsiConsole.Write(new Rule(ruleMessage).RuleStyle(Style.Parse(ThemeConfig.Current.MessageColor)));
        }

        /// <summary>
        /// Writes a horizontal rule line to the console.
        /// </summary>
        public void WriteRule()
        {
            AnsiConsole.Write(new Rule().RuleStyle(Style.Parse(ThemeConfig.Current.MessageColor)));
        }

        /// <summary>
        /// Writes a warning message to the console.
        /// </summary>
        /// <param name="warningMessage">The message to write.</param>
        public void WriteWarning(string warningMessage)
        {
            AnsiConsole.MarkupLine(ThemeConfig.ToWarning(warningMessage));
        }

        public void PrintLastSatats(TraceGC traceGC, GCHeapStats heapStats)
        {

        }

        public async Task PrintLastStatsAsync(CapturedGCEvent lastGC)
        {
            await liveOutputTable.StopAsync();

            if (lastGC == null)
            {
                WriteWarning("No stats collected yet.");
            }
            else
            {
                WriteRule();
                GCHeapStats heapStats = lastGC.Data.HeapStats;

                Table table = new Table().HideHeaders();
                table.Title = new TableTitle($"[{ThemeConfig.Current.GCTableHeaderColor}]Heap Stats as of {lastGC.Time:u} (Run {lastGC.Data.Number} for gen {lastGC.Data.Generation}):[/]\n");
                table.AddColumn(new TableColumn("Results")); // name is hidden
                table.AddRow(
                    new Table().HideHeaders()
                    .AddColumn("Name", config => config.Alignment(Justify.Left)) // name is hidden
                    .AddColumn("Value", config => config.Alignment(Justify.Right)) // name is hidden
                    .AddRow(ThemeConfig.ToHeader("Heaps:"), string.Format("{0:N0}", lastGC.Data.HeapCount))
                    .AddRow(ThemeConfig.ToHeader("Handles:"), string.Format("{0:N0}", heapStats.GCHandleCount))
                    .AddRow(ThemeConfig.ToHeader("Pinned Obj Count:"), string.Format("{0:N0}", heapStats.PinnedObjectCount)));

                table.AddRow(
                     new Panel(
                         new Table().HideHeaders().NoBorder() // lets us render two "rows" for stats table and then breakdown chart
                         .AddColumn("Col1") // name is hidden
                         .AddRow(new Table().HideHeaders()
                            .AddColumn("Name", config => config.Alignment(Justify.Left)) // name is hidden
                            .AddColumn("Value", config => config.Alignment(Justify.Right)) // name is hidden
                            .AddRow(ThemeConfig.TotalHeap, string.Format("{0,17:N0} Bytes", heapStats.TotalHeapSize))
                            .AddRow(ThemeConfig.Gen0Heap, string.Format("{0,17:N0} Bytes", heapStats.GenerationSize0))
                            .AddRow(ThemeConfig.Gen1Heap, string.Format("{0,17:N0} Bytes", heapStats.GenerationSize1))
                            .AddRow(ThemeConfig.Gen2Heap, string.Format("{0,17:N0} Bytes", heapStats.GenerationSize2))
                            .AddRow(ThemeConfig.Gen3Heap, string.Format("{0,17:N0} Bytes", heapStats.GenerationSize3))
                            .AddRow(ThemeConfig.Gen4Heap, string.Format("{0,17:N0} Bytes", heapStats.GenerationSize4)))
                         .AddRow(new BreakdownChart()
                            .FullSize()
                            .Width(60)
                            .ShowPercentage()
                             .AddItem("Gen 0", Math.Round(100 * (heapStats.GenerationSize0 / (double)heapStats.TotalHeapSize), 2), Style.Parse(ThemeConfig.Current.Gen0HeapColor).Foreground)
                             .AddItem("Gen 1", Math.Round(100 * (heapStats.GenerationSize1 / (double)heapStats.TotalHeapSize), 2), Style.Parse(ThemeConfig.Current.Gen1HeapColor).Foreground)
                             .AddItem("Gen 2", Math.Round(100 * (heapStats.GenerationSize2 / (double)heapStats.TotalHeapSize), 2), Style.Parse(ThemeConfig.Current.Gen2HeapColor).Foreground)
                             .AddItem("Gen 3", Math.Round(100 * (heapStats.GenerationSize3 / (double)heapStats.TotalHeapSize), 2), Style.Parse(ThemeConfig.Current.Gen3HeapColor).Foreground)
                             .AddItem("Gen 4", Math.Round(100 * (heapStats.GenerationSize4 / (double)heapStats.TotalHeapSize), 2), Style.Parse(ThemeConfig.Current.Gen4HeapColor).Foreground))
                      )
                     .Header(ThemeConfig.ToMessage("Last Run Stats:")));

                AnsiConsole.Write(table);
                WriteRule();
            }

            liveOutputTable.Start();
        }

        public void WriteStatsUsage()
        {
            WriteRule($"[{ThemeConfig.Current.MessageColor}]press [{ThemeConfig.Current.HighlightColor}]s[/] for current stats or any other key to exit[/]");
        }
    }
}
