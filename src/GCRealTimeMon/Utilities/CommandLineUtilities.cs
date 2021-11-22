﻿using System.Collections.Generic;
using System.IO;
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
    }
}
