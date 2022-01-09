namespace realmon.Utilities
{
    using System.Threading.Tasks;
    using Microsoft.Diagnostics.Tracing.Analysis.GC;
    using realmon.Configuration.Theme;
    using Spectre.Console;

    /// <summary>
    /// Utility for writing formatted console output
    /// </summary>
    internal class PlainConsoleOut : IConsoleOut
    {
        /// <summary>
        /// Writes a horizontal rule line with a message in the center to the console.
        /// </summary>
        /// <param name="ruleMessage">The message to put in the center of the rule line.</param>
        public void WriteRule(string ruleMessage)
        {
            AnsiConsole.Write(new Rule(ruleMessage).RuleStyle(Style.Parse(ThemeConfig.Current.MessageRuleColor)));
        }

        /// <summary>
        /// Writes a horizontal rule line to the console.
        /// </summary>
        public void WriteRule()
        {
            AnsiConsole.Write(new Rule().RuleStyle(Style.Parse(ThemeConfig.Current.MessageRuleColor)));
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

        public void WriteProcessInfo(string processName, int pid)
        {
            throw new System.NotImplementedException();
        }

        public void WriteRow(TraceGC gc)
        {
            throw new System.NotImplementedException();
        }

        public void WriteTableHeaders()
        {
            throw new System.NotImplementedException();
        }

        public Task PrintLastStatsAsync(CapturedGCEvent lastGC)
        {
            throw new System.NotImplementedException();
        }
    }
}
