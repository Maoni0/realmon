using System.Collections.Generic;

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
        public const string RequiredCommandNotProvided = @"
Required Command was not provided. The monitoring begins only when a process name via -n or a process id via -p is supplied.
Examples:
    - GCRealTimeMon.exe -n devenv / dotnet-gcmon -n devenv
    - GCRealTimeMon.exe -p 1020 / dotnet-gcmon -p 1020
";

        public static readonly string UsageDetails = @$"
Usage:
    GCRealTimeMon.exe [command line args] / dotnet-gcmon [command line args]

More Details:
    - Specify a process Id by using -p or a process name by using -n, the tool will show GCs as they occur in that process. If there are multiple processes with that name it would pick the first one
    - You can specify which info to display per GC by using a config file. You can either change the current config at ``{Configuration.ConfigurationReader.DefaultPath}`` -- specifically, if you are using this as a dotnet global tool it's the path where gcmon dll is, but if you are using this when you build it yourself, it's the DefaultConfig.yaml where GCRealTimeMon.dll is. Or specify your own by using ""-c config_file_path"", eg, ""-c c:\data\gcmon-config.yaml"" 
    - To create a config file, use -g which allows you to specify a path for the config file and choose which info to display for each GC or overwrite the default config by entering -c without any parameters. 

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
