using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using CommandLine;
using realmon.Configuration;
using realmon.Utilities;
using Spectre.Console;
using GCRealTimeMon.Configuration.ConsoleOutput;
using GCRealTimeMon.Utilities;

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
            public string PathToConfigurationFile { get; set; } = null;
        }

        static Timer heapStatsTimer;
        static DateTime lastGCTime;
        static TraceGC lastGC;
        static LiveOutputTable liveOutputTable;

        public static void RealTimeProcessing(int pid, Options options, Configuration.Configuration configuration)
        {
            Process process = Process.GetProcessById(pid);
            double? minDurationForGCPausesInMSec = options.MinDurationForGCPausesMSec;

            ConsoleOut.WriteRule($"[{Theme.Constants.MessageColor}]Monitoring process with name: [{Theme.Constants.HighlightColor}]{process.ProcessName}[/] and pid: [{Theme.Constants.HighlightColor}]{pid}[/][/]");

            liveOutputTable = new LiveOutputTable(configuration);
            liveOutputTable.Start();
            //Console.WriteLine(PrintUtilities.GetLineSeparator(configuration));

            var source = PlatformUtilities.GetTraceEventDispatcherBasedOnPlatform(pid, out var session);

            // this thread is responsible for listening to user input on the console and dispose the session accordingly
            Thread monitorThread = new Thread(() => HandleConsoleInput(session));
            monitorThread.Start();

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

                                liveOutputTable.WriteRow(gc, configuration).Wait();
                            }
                        }
                    };
                });
            });

            // blocking call on the main thread until the session gets disposed upon user action
            source.Process();
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
                    // Discard the task so that any exception doesn't crash the process but fires the TaskScheduler.UnobservedTaskException event
                    _ = PrintLastStats();
                }
            };

            // If ``stats_mode`` is enabled, the lifetime of this timer should be that of the process.
            heapStatsTimer = new Timer(callback: timerCallback,
                                       dueTime: 0,
                                       state: null,
                                       period: period);
        }

        private static async Task PrintLastStats()
        {
            await liveOutputTable.Stop();

            if (lastGC == null)
            {
                ConsoleOut.WriteWarning("No stats collected yet.");
            }
            else
            {
                var t = lastGC; // capture, since this could tear
                var s = t.HeapStats;

                ConsoleOut.WriteRule();

                Table table = new Table().HideHeaders();
                table.Title = new TableTitle(string.Format("Heap Stats as of {0:u} (Run {1} for gen {2}):\n", lastGCTime, t.Number, t.Generation));
                table.AddColumn(new TableColumn("Results"));
                table.AddRow(
                    new Table().HideHeaders()
                    .AddColumn("Name", config => config.Alignment(Justify.Left))
                    .AddColumn("Value", config => config.Alignment(Justify.Right))
                    .AddRow(Theme.ToHeader("Heaps:"), string.Format("{0:N0}", t.HeapCount))
                    .AddRow(Theme.ToHeader("Handles:"), string.Format("{0:N0}", s.GCHandleCount))
                    .AddRow(Theme.ToHeader("Pinned Obj Count:"), string.Format("{0:N0}", s.PinnedObjectCount)));

                table.AddRow(
                     new Panel(
                         new Table().HideHeaders().NoBorder() // lets us render two "rows" for stats table and then breakdown chart
                         .AddColumn("Col1") // unused and hidden
                         .AddRow(new Table().HideHeaders()
                            .AddColumn("Name", config => config.Alignment(Justify.Left))
                            .AddColumn("Value", config => config.Alignment(Justify.Right))
                            .AddRow(Theme.TotalHeap, string.Format("{0,17:N0} Bytes", s.TotalHeapSize))
                            .AddRow(Theme.Gen0Heap, string.Format("{0,17:N0} Bytes", s.GenerationSize0))
                            .AddRow(Theme.Gen1Heap, string.Format("{0,17:N0} Bytes", s.GenerationSize1))
                            .AddRow(Theme.Gen2Heap, string.Format("{0,17:N0} Bytes", s.GenerationSize2))
                            .AddRow(Theme.Gen3Heap, string.Format("{0,17:N0} Bytes", s.GenerationSize3))
                            .AddRow(Theme.Gen4Heap, string.Format("{0,17:N0} Bytes", s.GenerationSize4)))
                         .AddRow(new BreakdownChart()
                            .FullSize()
                            .Width(60)
                            .ShowPercentage()
                             // Todo Ryan - AddItem doesn't take a color string, so how do we use the theme class here?
                             .AddItem("Gen 0", Math.Round(100 * (s.GenerationSize0 / (double)s.TotalHeapSize), 2), Color.Green1)
                             .AddItem("Gen 1", Math.Round(100 * (s.GenerationSize1 / (double)s.TotalHeapSize), 2), Color.HotPink)
                             .AddItem("Gen 2", Math.Round(100 * (s.GenerationSize2 / (double)s.TotalHeapSize), 2), Color.DodgerBlue1)
                             .AddItem("Gen 3", Math.Round(100 * (s.GenerationSize3 / (double)s.TotalHeapSize), 2), Color.Yellow1)
                             .AddItem("Gen 4", Math.Round(100 * (s.GenerationSize4 / (double)s.TotalHeapSize), 2), Color.MediumPurple3))
                      )
                     .Header(Theme.ToMessage("Last Run Stats:")));

                AnsiConsole.Write(table);
                ConsoleOut.WriteRule();
            }

            liveOutputTable.Restart();
        }

        static void HandleConsoleInput(IDisposable session)
        {
            var k = Console.ReadKey(true);

            while (k.Key == ConsoleKey.S)
            {
                PrintLastStats();
                k = Console.ReadKey(true);
            }
            session.Dispose();
        }

        // Compute the path to the configuration file:
        //    given by -c on the command line (could be a full path name, relative path or file in the current working directory)
        //    or use the default .yaml file in the tool folder
        static async Task<Configuration.Configuration> GetConfiguration(Options options)
        {
            var configurationFile = options.PathToConfigurationFile;

            if (string.IsNullOrEmpty(configurationFile))
            {
                // the default .yaml file is at the same location as the CLI global tool / console application
                configurationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DefaultConfig.yaml");
            }
            else
            // ensure that the configuration file given on the command line exists
            if (!File.Exists(configurationFile))
            {
                throw new ArgumentException($"The given configuration file '{configurationFile}' does not exist...");
            }

            var configuration = await ConfigurationReader.ReadConfigurationAsync(configurationFile);
            return configuration;
        }

        static async Task Main(string[] args)
        {
            try
            {
                await Parser.Default.ParseArguments<Options>(args)
                  .MapResult(async options =>
                  {
                      if (options.ProcessId == -1 && string.IsNullOrEmpty(options.ProcessName))
                      {
                          throw new ArgumentException("Specify a process Id using: -p or a process name by using -n.");
                      }

                      var configuration = await GetConfiguration(options);

                      if (options.ProcessId == -1)
                      {
                          Process[] processes = Process.GetProcessesByName(options.ProcessName);
                          if (processes.Length == 0)
                          {
                              throw new ArgumentException($"No Processes found with name: {options.ProcessName}");
                          }

                          if (processes.Length != 1)
                          {
                              throw new ArgumentException($"Several processes with name: '{options.ProcessName}' have been found.");
                          }

                          options.ProcessId = processes[0].Id;
                      }

                      ConsoleOut.WriteRule($"[{Theme.Constants.MessageColor}]press [{Theme.Constants.HighlightColor}]s[/] for current stats or any other key to exit[/]");

                      SetupHeapStatsTimerIfEnabled(configuration);
                      RealTimeProcessing(options.ProcessId, options, configuration);
                  },
                  errors => Task.FromResult(errors)
                  );
            }
            catch (Exception x) when (
                (x is ArgumentException) ||
                (x is KeyNotFoundException)
                )
            {
                Console.WriteLine(x.Message);

                // exit on argument/configuration errors
            }
        }
    }
}
