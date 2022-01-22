using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace realmon.Utilities
{
    /// <summary>
    /// An interface covering the console output needs of the program.
    /// </summary>
    internal interface IConsoleOut
    {
        void WriteProcessInfo(string processName, int pid);
        void WriteRow(TraceGC gc);
        void WriteTableHeaders();
        Task PrintLastStatsAsync(CapturedGCEvent lastGC);
        Task PrintCallStack(TraceCallStack callstack, string eventName);
        void WriteStatsUsage();
        int WritePromptForMultipleProcessesAndReturnChosenProcessId(string promptTitle, IEnumerable<string> processChoices);
    }
}