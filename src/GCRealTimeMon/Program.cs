using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                    HelpText = "The process name for which the GC Monitoring will take place for - the first process is chosen if there are multiple. Either this parameter or -p must be passed for the monitoring to begin.")]
            public string ProcessName { get; set; } = null;

            [Option(shortName: 'p',
                    longName: "processId",
                    Required = false,
                    HelpText = "The process id for which the GC Monitoring will take place for. Either this parameter or -n must be passed for the monitoring to begin.")]
            public int ProcessId { get; set; } = -1;

            [Option(shortName: 'c',
                    longName: "configPath",
                    Required = false,
HelpText = "The path to the YAML columns configuration file used during the session. If no path is specified, the default configuration is overwritten by the selected column in the prompt.")]
            public string PathToConfigurationFile { get; set; } = null;

            [Option(shortName: 'g',
                    longName: "createConfigPath",
                    Required = false,
                    HelpText = "The path of the YAML configuration file to be generated based on the colums selection available in the command prompt.")]
            public string PathToNewConfigurationFile { get; set; } = null;

            [Option(shortName: '?',
                    longName: "\\?",
                    Required = false,
                    HelpText = "Display Help.")]
            public bool HelpAsked { get; set; } = false;
        }

        static Timer heapStatsTimer;
        static DateTime lastGCTime;
        static TraceGC lastGC;
        static LiveOutputTable liveOutputTable;

        public static void RealTimeProcessing(int pid, Options options, Configuration.Configuration configuration)
        {
            Process process = Process.GetProcessById(pid);
            double? minDurationForGCPausesInMSec = null;
            if (configuration.DisplayConditions != null && 
                configuration.DisplayConditions.TryGetValue("min gc duration (msec)", out var minDuration))
            {
                minDurationForGCPausesInMSec = double.Parse(minDuration);
            }

            ConsoleOut.WriteRule($"[{Theme.Constants.MessageColor}]Monitoring process with name: [{Theme.Constants.HighlightColor}]{process.ProcessName}[/] and pid: [{Theme.Constants.HighlightColor}]{pid}[/][/]");

            liveOutputTable = new LiveOutputTable(configuration);
            liveOutputTable.Start();

            var source = PlatformUtilities.GetTraceEventDispatcherBasedOnPlatform(pid, out var session);
            Console.CancelKeyPress += (_, e) =>
            {
                // Dispose the session.
                session?.Dispose();
                // Exit the process.
                Environment.Exit(0);
            };

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
        //    if no path is specified, use the default .yaml file in the tool folder.
        //    else if the -c arg is specified (could be a full path name, relative path or file in the current working directory), serialize the file in the path.
        static async Task<Configuration.Configuration> GetConfiguration(Options options)
        {
            string defaultPath = ConfigurationReader.DefaultPath;

            // Case: -c was specified.
            if (!string.IsNullOrEmpty(options.PathToConfigurationFile))
            {
                string configurationFile = options.PathToConfigurationFile;

                // If the SENTINEL_VALUE passed by force, start the prompt to overwrite the default config.
                if (string.CompareOrdinal(options.PathToConfigurationFile, CommandLineUtilities.SentinelPath) == 0)
                {
                    Configuration.Configuration defaultConfig = await ConfigurationReader.ReadConfigurationAsync(defaultPath);
                    return await NewConfigurationManager.CreateAndReturnNewConfiguration(defaultPath, defaultConfig);
                }

                // Validate if the file in the specified path exists.
                if (!File.Exists(configurationFile))
                {
                    throw new ArgumentException($"The given configuration file '{configurationFile}' does not exist...");
                }

                return await ConfigurationReader.ReadConfigurationAsync(configurationFile);
            }

            // Case: -g was specified.
            else if (!string.IsNullOrEmpty(options.PathToNewConfigurationFile))
            {
                return await NewConfigurationManager.CreateAndReturnNewConfiguration(options.PathToNewConfigurationFile);
            }

            // Case: Neither -g nor -c was specified => fall back to the Default config.
            else
            {
                // the default .yaml file is at the same location as the CLI global tool / console application
                return await ConfigurationReader.ReadConfigurationAsync(defaultPath);
            }
        }

        public static void DisplayHelp(ParserResult<Options> parserResults, bool addWrongCommandAndExampleUsage = false)
        {
            if (addWrongCommandAndExampleUsage)
            {
                Console.WriteLine(CommandLineUtilities.RequiredCommandNotProvided);
            }

            Console.WriteLine(CommandLineUtilities.UsageDetails);
            Console.WriteLine(HelpText.AutoBuild<Options>(parserResults, h => h, e => e));
        }

        static async Task Main(string[] args)
        {
            try
            {
                args = CommandLineUtilities.AddSentinelValueForTheConfigPathIfNotSpecified(args);

                var result = Parser.Default.ParseArguments<Options>(args);
                await result.MapResult(async options =>
                  {
                      // If help is asked for / no command line args are specified or The process id / process name isn't specified, display the help text. 
                      if (args.Length == 0) 
                      {
                          DisplayHelp(result, true);
                          return;
                      }

                      if (options.HelpAsked)
                      {
                          DisplayHelp(result);
                          return;
                      }

                      var configuration = await GetConfiguration(options);

                      if (options.ProcessId == -1 && string.IsNullOrEmpty(options.ProcessName))
                      {
                          // If no process details are provided _but_ if the user provides -c without args or -g <path>, don't display help.
                          if (!string.IsNullOrWhiteSpace(options.PathToNewConfigurationFile) || // User passed: -g <path> 
                              string.CompareOrdinal(options.PathToConfigurationFile, CommandLineUtilities.SentinelPath) == 0 // User passed -c without a path.
                             )
                          {
                              return;
                          }

                          DisplayHelp(result, true);
                          return;
                      }

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
