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
                    longName: "minDurationForGCPauseMs",
                    Required = false,
                    HelpText = "The minimum duration in Ms for GC Pause Duration. Any GCs below this will not be considered.")]
            public double? MinDurationForGCPausesMs { get; set; } = null;
        }

        static TraceEventSession session;

        public static void RealTimeProcessing(string processName, double? minDurationForGCPausesInMs)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                throw new ArgumentException($"No Processes found with name: {processName}");
            }

            else
            {
                RealTimeProcessing(processes[0].Id, minDurationForGCPausesInMs);
            }
        }

        public static void RealTimeProcessing(int pid, double? minDurationForGCPausesInMs)
        {
            Console.WriteLine();
            Process process = Process.GetProcessById(pid);
            Console.WriteLine($"Monitoring process with name: {process.ProcessName} and id: {pid}");
            Console.WriteLine("GC#{0,10} | {1,15} | {2,5} | {3,10}", "index", "type", "gen", "pause (ms)");
            Console.WriteLine("----------------------------------------------------");

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
                                if (!minDurationForGCPausesInMs.HasValue ||
                                   (minDurationForGCPausesInMs.HasValue && minDurationForGCPausesInMs.Value < gc.PauseDurationMSec))
                                {
                                    Console.WriteLine("GC#{0,10} | {1,15} | {2,5} | {3,10:N2}",
                                        gc.Number, gc.Type, gc.Generation, gc.PauseDurationMSec);
                                }
                            }
                        };
                    });
                });

                session.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrTraceEventParser.Keywords.GC);

                source.Process();
            }
        }

        static void RunTest()
        {
            Console.WriteLine("-------press any key to exit {0}-------", (char)1);
            Console.ReadLine();
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
                      double? minDurationForGCPauses = o.MinDurationForGCPausesMs;

                      if (o.ProcessId != -1)
                      {
                          RealTimeProcessing(o.ProcessId, minDurationForGCPauses);
                      }

                      else if (!string.IsNullOrEmpty(o.ProcessName))
                      {
                          RealTimeProcessing(o.ProcessName, o.MinDurationForGCPausesMs);
                      }

                      else
                      {
                          throw new ArgumentException("Specify a process Id using: -p or a process name by using -n.");
                      }
                  });
        }
    }
}
