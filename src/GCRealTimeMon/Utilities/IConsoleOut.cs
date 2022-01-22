using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Analysis.GC;

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
        void WriteStatsUsage();
        int WritePromptForMultipleProcessesAndReturnChosenProcessId(string promptTitle, IEnumerable<string> processChoices);
    }
}