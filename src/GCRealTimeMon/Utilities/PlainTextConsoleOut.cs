using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sharprompt;

namespace realmon.Utilities
{
    using Configuration = realmon.Configuration.Configuration;

    /// <summary>
    /// Utility for writing plain text console output
    /// </summary>
    internal class PlainTextConsoleOut : IConsoleOut
    {
        // This lock is used in places where multiple methods may be trying to write to console out at the same time.
        static object writerLock = new object();

        private readonly Configuration configuration;

        public PlainTextConsoleOut(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void WriteStatsUsage()
        {
            Console.WriteLine("------- press s for current stats or any other key to exit -------");
        }

        public void WriteProcessInfo(string processName, int pid)
        {
            Console.WriteLine($"Monitoring process with name: {processName} and pid: {pid}");
        }

        public void WriteRow(TraceGC gc)
        {
            lock (writerLock)
            {
                Console.WriteLine(PrintUtilities.GetRowDetails(gc, configuration));
            }
        }

        public void WriteTableHeaders()
        {
            Console.WriteLine(PrintUtilities.GetHeader(configuration));
            Console.WriteLine(PrintUtilities.GetLineSeparator(configuration));
        }

        public Task PrintLastStatsAsync(CapturedGCEvent lastGC)
        {
            if (lastGC == null)
            {
                Console.WriteLine("No stats collected yet.");
            }
            else
            {
                var t = lastGC.Data; // capture, since this could tear
                var s = lastGC.Data.HeapStats;
                lock (writerLock)
                {
                    Console.WriteLine(PrintUtilities.LineSeparator);
                    Console.WriteLine("Heap Stats as of {0:u} (Run {1} for gen {2}):", lastGC.Time, t.Number, t.Generation);
                    Console.WriteLine("  Heaps: {0:N0}", t.HeapCount);
                    Console.WriteLine("  Handles: {0:N0}", s.GCHandleCount);
                    Console.WriteLine("  Pinned Obj Count: {0:N0}", s.PinnedObjectCount);
                    Console.WriteLine("  Last Run Stats:");
                    Console.WriteLine("    Total Heap: {0:N0} Bytes", s.TotalHeapSize);
                    Console.WriteLine("      Gen 0: {0,17:N0} Bytes", s.GenerationSize0);
                    Console.WriteLine("      Gen 1: {0,17:N0} Bytes", s.GenerationSize1);
                    Console.WriteLine("      Gen 2: {0,17:N0} Bytes", s.GenerationSize2);
                    Console.WriteLine("      Gen 3: {0,17:N0} Bytes", s.GenerationSize3);
                    Console.WriteLine("      Gen 4: {0,17:N0} Bytes", s.GenerationSize4);
                    Console.WriteLine(PrintUtilities.LineSeparator);
                }
            }

            return Task.CompletedTask;
        }

        public Task PrintCallStack(TraceCallStack callstack, string eventName)
        {
            lock (writerLock)
            {
                Console.WriteLine(PrintUtilities.LineSeparator);
                Console.WriteLine($"CallStack For {eventName}:");
                while (callstack != null)
                {
                    var codeAddress = callstack.CodeAddress;

                    // Like WinDbg, display unresolved modules with the address in Hex form.
                    if (codeAddress.ModuleFile == null)
                    {
                        Console.WriteLine("0x{0:x}", codeAddress.Address);
                    }
                    else
                    {
                        Console.WriteLine($"{codeAddress.ModuleName}!{codeAddress.FullMethodName}");
                    }

                    callstack = callstack.Caller;
                }

                Console.WriteLine(PrintUtilities.LineSeparator);
            }

            return Task.CompletedTask;
        }

        public int WritePromptForMultipleProcessesAndReturnChosenProcessId(string promptTitle, IEnumerable<string> processChoices)
        {
            string selectedProcess = Prompt.Select(promptTitle, processChoices);
            return PrintUtilities.ParseProcessIdFromMultiProcessPrompt(selectedProcess);
        }
    }
}
