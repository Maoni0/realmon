namespace realmon.Utilities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Diagnostics.Tracing.Analysis.GC;

    internal interface IConsoleOut
    {
        void WriteProcessInfo(string processName, int pid);
        void WriteRow(TraceGC gc);
        void WriteRule();
        void WriteRule(string ruleMessage);
        void WriteTableHeaders();
        void WriteWarning(string warningMessage);
        Task PrintLastStatsAsync(CapturedGCEvent lastGC);
    }
}