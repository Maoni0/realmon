using System;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Collections.Generic;
using System.Threading;

namespace realmon
{
    class Program
    {
        static TraceEventSession session;

        public static void RealTimeProcessing(int pid)
        {
            Console.WriteLine();
            Console.WriteLine("Monitoring process {0}", pid);
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
                                Console.WriteLine("GC#{0,10} | {1,15} | {2,5} | {3,10:N2}",
                                    gc.Number, gc.Type, gc.Generation, gc.PauseDurationMSec);
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
            RealTimeProcessing(int.Parse(args[0]));
        }
    }
}
