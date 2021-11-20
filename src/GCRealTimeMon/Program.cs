using System;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Threading;
using CommandLine;
using System.Diagnostics;
using System.Threading.Tasks;
using realmon.Configuration;
using realmon.Utilities;

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

            [Option(shortName: 'c',
                    longName: "configPath",
                    Required = false,
                    HelpText = "The path to the YAML configuration file that is read in.")]
            public string PathToConfigurationFile { get; set; } = "./DefaultConfig.yaml";

            [Option(shortName: 'g',
                    longName: "createConfigPath",
                    Required = false,
                    HelpText = "The path of the config to be created via the command line.")]
            public string PathToNewConfigurationFile { get; set; } = null;
        }

        static IDisposable session;
        static Timer heapStatsTimer;
        static DateTime lastGCTime;
        static TraceGC lastGC;
        static object writerLock = new object();

        public static void RealTimeProcessingByProcessName(Options options, Configuration.Configuration configuration)
        {
            Process[] processes = Process.GetProcessesByName(options.ProcessName);
            if (processes.Length == 0)
            {
                throw new ArgumentException($"No Processes found with name: {options.ProcessName}");
            }

            else
            {
                RealTimeProcessingByProcessId(processes[0].Id, options, configuration);
            }
        }


        public static void RealTimeProcessingByProcessId(int pid, Options options, Configuration.Configuration configuration)
        {
            Console.WriteLine();
            Process process = Process.GetProcessById(pid);
            double? minDurationForGCPausesInMSec = options.MinDurationForGCPausesMSec;
            Console.WriteLine($"Monitoring process with name: {process.ProcessName} and pid: {pid}");
            Console.WriteLine(PrintUtilities.GetHeader(configuration));
            Console.WriteLine(PrintUtilities.GetLineSeparator(configuration));

            {
                var source = PlatformUtilities.GetTraceEventDispatcherBasedOnPlatform(pid, out session);

                source.NeedLoadedDotNetRuntimes();
                source.AddCallbackOnProcessStart(delegate (TraceProcess proc)
                {
                    proc.AddCallbackOnDotNetRuntimeLoad(delegate (TraceLoadedDotNetRuntime runtime)
                    {
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
                                        Console.WriteLine(PrintUtilities.GetRowDetails(gc, configuration));
                                    }
                                }
                            }
                        };
                    });
                });


                source.Process();
            }
        }

        private static void SetupHeapStatsTimerIfEnabled(Configuration.Configuration configuration)
        {
            if (configuration.StatsMode == null)
            {
                return;
            }

            // Setup timer for Heap Stats.
            // At this point of time, all validations are successful => get exactly what we need without checking.
            string timerAsString = configuration.StatsMode["timer"];
            int period = int.Parse(timerAsString[0..^1]);
            char periodType = timerAsString[^1];
            period = periodType switch
            {
                // minutes.
                'm' => period * 60 * 1000,
                // seconds.
                's' => period * 1000,
                // default, shouldn't get here as the validation takes care of this.
                _ => throw new NotImplementedException()
            };

            TimerCallback timerCallback = (_) =>
            {
                if (lastGC != null)
                {
                    PrintLastStats();
                }
            };

            // If ``stats_mode`` is enabled, the lifetime of this timer should be that of the process.
            heapStatsTimer = new Timer(callback: timerCallback, 
                                       dueTime: 0, 
                                       state: null,
                                       period: period);
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
                    Console.WriteLine(PrintUtilities.HeapStatsLineSeparator);
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
                    Console.WriteLine(PrintUtilities.HeapStatsLineSeparator);
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

        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
              .MapResult(async options =>
              {
                  // The process id / process name must always be provided.
                  if (options.ProcessId == -1 && string.IsNullOrEmpty(options.ProcessName))
                  {
                      throw new ArgumentException("Specify a process Id using: -p or a process name by using -n.");
                  }

                  // Next, deal with configuration.
                  Configuration.Configuration configuration = null; 

                  // Case 1: Create a new configuration file, persist to the given path and use it for this session.
                  if (!string.IsNullOrWhiteSpace(options.PathToNewConfigurationFile))
                  {
                      configuration = await NewConfigurationManager.CreateAndReturnNewConfiguration(options.PathToNewConfigurationFile);
                  }

                  // Case 2: Use the path to serialize the configuration to be used for this session.
                  else
                  {
                      configuration = await ConfigurationReader.ReadConfigurationAsync(options.PathToConfigurationFile);
                  }

                  // Start the monitor thread after the configuration has been parsed to avoid issues with the prompt.
                  ThreadStart ts = new ThreadStart(RunTest);
                  Thread monitorThread = new Thread(ts);
                  monitorThread.Start();

                  SetupHeapStatsTimerIfEnabled(configuration);

                  // Process the session based on the id / process name.
                  if (options.ProcessId != -1)
                  {
                      RealTimeProcessingByProcessId(options.ProcessId, options, configuration);
                  }

                  else if (!string.IsNullOrEmpty(options.ProcessName))
                  {
                      RealTimeProcessingByProcessName(options, configuration);
                  }
              },
              errors => Task.FromResult(errors));
        }
    }
}
