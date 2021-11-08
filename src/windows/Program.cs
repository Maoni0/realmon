using System;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Threading;
using CommandLine;
using System.Diagnostics;

namespace realmon
{
    class Program
    {
        public class Options
        {
            [Option(shortName: 'n',
                    longName: "processName",
                    Required = false,
                    HelpText = "The process name for which the GC Monitoring will take place for - the first process is chosen if there are multiple.")]
            public string ProcessName { get; set; } = null;

            [Option(shortName: 'p',
                    longName: "processId",
                    Required = false,
                    HelpText = "The process id for which the GC Monitoring will take place for.")]
            public int ProcessId { get; set; } = -1;

            [Option(shortName: 'm',
                    longName: "minDurationForGCPauseMSec",
                    Required = false,
                    HelpText = "The minimum duration in Ms for GC Pause Duration. Any GCs below this will not be considered.")]
            public double? MinDurationForGCPausesMSec { get; set; } = null;
        }

        static TraceEventSession session;
        static DateTime lastGCTime;
        static TraceGC lastGC;
        static object writerLock = new object();

        public static void RealTimeProcessing(string processName, double? minDurationForGCPausesInMSec)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                throw new ArgumentException($"No Processes found with name: {processName}");
            }

            else
            {
                RealTimeProcessing(processes[0].Id, minDurationForGCPausesInMSec);
            }
        }

        private const string LineSeparator = "------------------------------------------------------------------------------";

        public static void RealTimeProcessing(int pid, double? minDurationForGCPausesInMSec)
        {
            Console.WriteLine();
            Process process = Process.GetProcessById(pid);
            Console.WriteLine($"Monitoring process with name: {process.ProcessName} and pid: {pid}");
            Console.WriteLine("GC#{0,10} | {1,15} | {2,5} | {3,10} | {4, 21} |", "index", "type", "gen", "pause (ms)", "reason");
            Console.WriteLine(LineSeparator);

            session = new TraceEventSession("MySession");
            {
                var source = session.Source;
                source.NeedLoadedDotNetRuntimes();
                source.AddCallbackOnProcessStart(delegate (TraceProcess proc)
                {
                    proc.AddCallbackOnDotNetRuntimeLoad(delegate (TraceLoadedDotNetRuntime runtime)
                    {
                        runtime.GCStart += delegate (TraceProcess p, TraceGC gc)
                        {
                            if (p.ProcessID == pid)
                            {
                                //Console.WriteLine("GC#{0:5} {1} gen{2} start at {3:10.00}ms", gc.Number, gc.Type, gc.Generation, gc.PauseStartRelativeMSec);
                            }
                        };
                        runtime.GCEnd += delegate (TraceProcess p, TraceGC gc)
                        {
                            if (p.ProcessID == pid)
                            {
                                // If no min duration is specified or if the min duration specified is less than the pause duration, log the event.
                                if (!minDurationForGCPausesInMSec.HasValue ||
                                   (minDurationForGCPausesInMSec.HasValue && minDurationForGCPausesInMSec.Value < gc.PauseDurationMSec))
                                {
                                    lastGCTime = DateTime.UtcNow;
                                    lastGC = gc;

                                    lock (writerLock) {
                                        Console.WriteLine("GC#{0,10} | {1,15} | {2,5} | {3,10:N2} | {4, 21} |",
                                            gc.Number, 
                                            gc.Type, 
                                            gc.Generation, 
                                            gc.PauseDurationMSec,
                                            gc.Reason);
                                    }
                                }
                            }
                        };
                    });
                });

                session.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrTraceEventParser.Keywords.GC);

                source.Process();
            }
        }

        private static void PrintLastStats()
        {
            if (lastGC == null)
            {
                Console.WriteLine("No stats collected yet.");
            }
            else 
            {
                var t = lastGC; // capture, since this could tear
                var s = t.HeapStats;
                lock (writerLock)
                {
                    Console.WriteLine(LineSeparator);
                    Console.WriteLine("Heap Stats as of {0:u} (Run {1} for gen {2}):", lastGCTime, t.Number, t.Generation);
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
                    Console.WriteLine(LineSeparator);
                }
            }
        }

        static void RunTest()
        {
            Console.WriteLine("------- press s for current stats or any other key to exit -------");
            var k = Console.ReadKey(true);

            while (k.Key == ConsoleKey.S)
            {
                PrintLastStats();
                k = Console.ReadKey(true);
            }
            session.Dispose();
        }

        static void Main(string[] args)
        {
            ThreadStart ts = new ThreadStart(RunTest);
            Thread monitorThread = new Thread(ts);
            monitorThread.Start();

            Parser.Default.ParseArguments<Options>(args)
                  .WithParsed<Options>(o =>
                  {
                      double? minDurationForGCPauses = o.MinDurationForGCPausesMSec;

                      if (o.ProcessId != -1)
                      {
                          RealTimeProcessing(o.ProcessId, minDurationForGCPauses);
                      }

                      else if (!string.IsNullOrEmpty(o.ProcessName))
                      {
                          RealTimeProcessing(o.ProcessName, o.MinDurationForGCPausesMSec);
                      }

                      else
                      {
                          throw new ArgumentException("Specify a process Id using: -p or a process name by using -n.");
                      }
                  });
        }
    }
}
