﻿using System;
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
using CommandLine.Text;

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

            [Option(shortName: 's',
                    longName: "callStacks",
                    Required = false,
                    HelpText = "Setting this option will enable call stacks for induced GCs and large object allocations. Getting call stacks will currently only work for Windows and the session must have admin privileges.")]
            public bool EnableCallStacks { get; set; } = false;

            [Option(shortName: '?',
                    longName: "\\?",
                    Required = false,
                    HelpText = "Display Help.")]
            public bool HelpAsked { get; set; } = false;
        }

        static Timer heapStatsTimer;
        static DateTime lastGCTime;
        static TraceGC lastGC;
        static object writerLock = new object();

        public static void RealTimeProcessing(int pid, Options options, Configuration.Configuration configuration)
        {
            Console.WriteLine();
            Process process = Process.GetProcessById(pid);
            double? minDurationForGCPausesInMSec = null;
            if (configuration.DisplayConditions != null && 
                configuration.DisplayConditions.TryGetValue("min gc duration (msec)", out var minDuration))
            {
                minDurationForGCPausesInMSec = double.Parse(minDuration);
            }

            Console.WriteLine($"Monitoring process with name: {process.ProcessName} and pid: {pid}");
            Console.WriteLine(PrintUtilities.GetHeader(configuration));
            Console.WriteLine(PrintUtilities.GetLineSeparator(configuration));

            var source = PlatformUtilities.GetTraceEventDispatcherBasedOnPlatform(configuration: configuration, 
                                                                                  processId: pid, 
                                                                                  enableCallStacks: options.EnableCallStacks,
                                                                                  out var session);

            // this thread is responsible for listening to user input on the console and dispose the session accordingly
            Thread monitorThread = new Thread(() => HandleConsoleInput(session)) ;
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

                                lock (writerLock) {
                                    Console.WriteLine(PrintUtilities.GetRowDetails(gc, configuration));
                                }
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

                      Console.WriteLine("------- press s for current stats or any other key to exit -------");

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
