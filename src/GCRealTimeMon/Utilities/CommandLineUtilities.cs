using Sharprompt;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;

namespace realmon.Utilities
{
    public static class CommandLineUtilities
    {
        /// <summary>
        /// Sentinel path that'll be inserted if -c has no specified value.
        /// This value will be matched in the case of deciding which configuration to use.
        /// This is to facilitate the behavior to overwrite the default config based on the inputs of the prompts.
        /// </summary>
        public const string SentinelPath = "__SENTINEL_PATH__";
        public static readonly string ExeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public static readonly string RequiredCommandNotProvided = @$"
Required Command was not provided. The monitoring begins only when a process name via -n or a process id via -p is supplied.
Examples:
    - {ExeName} -p 1020 
    - {ExeName} -n devenv
";

        public static readonly string UsageDetails = @$"
Usage:
    {ExeName} [command line args]

More Details:
    - Specify a process Id with -p or a process name with -n to display details of the garbage collection as they occur in that process. If there are multiple processes with that name it would pick the first one.
    - Displayed columns per GC are defined in a YAML configuration file: {Configuration.ConfigurationReader.DefaultPath}. It can be changed to a custom one generated with -g or manually select columns with -c.

";

        public static string[] AddSentinelValueForTheConfigPathIfNotSpecified(string[] argsFromCommandLine)
        {
            List<string> listOfArgs = new List<string>(argsFromCommandLine);

            // Check to see if the -c arg is passed. If not, pass in the SENTINEL_VALUE.

            // If -c is passed in, we need to check if the subsequent item in the array is a path or another command line arg.
            int idxOfConfigArg = listOfArgs.IndexOf("-c");

            // The idx of config arg exists.
            if (idxOfConfigArg != -1)
            {
                // If the idx of the -c is the last arg, append the SENTINEL_VALUE to the end.
                // This is done since the path is null as `-c` is at the end of the args array.
                if (idxOfConfigArg == listOfArgs.Count - 1)
                {
                    listOfArgs.Add(SentinelPath);
                }

                // Path cannot begin with '-' => it's another command line arg and therefore, insert the SENTINEL_VALUE at the next idx.
                else if (listOfArgs[idxOfConfigArg + 1].StartsWith("-"))
                {
                    listOfArgs.Insert(idxOfConfigArg + 1, SentinelPath);
                }
            }

            return listOfArgs.ToArray();
        }

        /// <summary>
        /// Gets the command line arguments for a particular process. The methodology to get this differs between Windows and Linux/MacOS.
        /// Either way, requires Root i.e. Admin or sudo privileges.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        internal static string GetCommandLineArguments(this Process process)
        {
            if (PlatformUtilities.IsWindows)
            {
                string cmdLine = null;
                using (var searcher = new ManagementObjectSearcher(
                  $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    using (var matchEnum = searcher.Get().GetEnumerator())
                    {
                        if (matchEnum.MoveNext()) // Move to the 1st item.
                        {
                            cmdLine = matchEnum.Current["CommandLine"]?.ToString();
                        }
                    }
                }
                if (cmdLine == null)
                {
                    var dummy = process.MainModule; // Provoke exception.
                }

                return cmdLine;
            }

            else
            {
                return File.ReadAllText($"/proc/{ process.Id }/cmdline");
            }
        }

        /// <summary>
        /// Prompts the user to pick a process if multiple processes with the same name are found.
        /// A more detailed list of processes (including command line args) is available if the user is in admin/sudo mode.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public static int GetProcessIdIfThereAreMultipleProcesses(Process[] processes)
        {
            // We can only obtain the process args if we have sudo / admin privileges.
            bool isRoot = PlatformUtilities.IsRoot();

            List<string> processDetails = new List<string>(processes.Length); 
            foreach(var process in processes)
            {
                if (isRoot)
                {
                    processDetails.Add($"Pid: {process.Id} | Arguments: {process.GetCommandLineArguments()}"); 
                }
                else
                {
                    processDetails.Add($"Pid: {process.Id} ");
                }
            }

            string multiprocessPrompt = isRoot ?
                $"Several processes with name: '{processes[0].ProcessName}' have been found. Please choose one from the following from below"
                : $"Several processes with name: '{processes[0].ProcessName}' have been found. Please choose one from the following from below. \nNote: To view the command line arguments for the process, you have to be in admin/sudo mode.";

            string selectedProcess = Prompt.Select(multiprocessPrompt, processDetails);
            string pidAsString = selectedProcess.Split('|')[0].Split(':')[1].Trim();
            return int.Parse(pidAsString);
        }
    }
}
