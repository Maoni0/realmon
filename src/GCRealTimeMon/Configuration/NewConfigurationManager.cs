using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using realmon.Utilities;
using Sharprompt;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace realmon.Configuration
{
    public static class NewConfigurationManager
    {
        /// <summary>
        /// Method creates a new configuration based on the user inputs and then persists it to the said path.
        /// </summary>
        /// <returns></returns>
        public static async Task<Configuration> CreateAndReturnNewConfiguration(string path, Configuration defaultConfiguration = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                // caught in Main.
                throw new ArgumentException($"Invalid configuration file path '{path}'");
            }

            var allColumns = ColumnInfoMap.Map.Values.Select(s => $"{s.Name} : {s.Description}");
            // Remove the ``index`` column as it is included by default.
            allColumns = allColumns.Where(s => !s.Contains("index : The GC Index."));

            // Column selection.
            List<string> defaultValuesForColumns = new List<string>();
            // If the default config is specified, update the prompt with the current default columns selected.
            if (defaultConfiguration != null)
            {
                // Go through all the columns and grab the defaults.  
                foreach(var column in allColumns)
                {
                    string name = column.Split(":")[0].Trim();
                    foreach(var defaultConfigColumn in defaultConfiguration.Columns)
                    {
                        // If the column name is in the allColumns containing both the column name and description, add it to the defaults.
                        if (defaultConfiguration.Columns.Contains(name))
                        {
                            defaultValuesForColumns.Add(column);
                        }
                    }
                }
            }

            // Since SharpPrompt doesn't allow us to configuration the answer as the forms are internal,
            // the workaround here is to set the answer to the background color of the console so that it appears invisible 
            // and then manually output it.
            Prompt.ColorSchema.Answer = Console.BackgroundColor;
            var chosenColumns = Prompt.MultiSelect(message: "Which columns would you like to select?",
                                                   items: allColumns,
                                                   defaultValues: defaultValuesForColumns);
            var nameOfColumns = chosenColumns.Select(s => s.Split(":").First().Trim());
            Console.WriteLine($"Chosen Columns:\n{string.Join("\n", nameOfColumns)}\n");

            // Re-configure the color schema of the answer so that the rest of the answers are visible.
            Prompt.ColorSchema.Answer = ConsoleColor.DarkGreen;

            // Heap stats timer.
            bool shouldSetupTimer = Prompt.Confirm("Would you like to setup the heap stats timer?");
            Dictionary<string, string> statsHeapDict = defaultConfiguration?.StatsMode ?? new();
            if (shouldSetupTimer)
            {
                // Try to grab the defaults from the specified default config.
                // Since the stats_mode is optional, check for the existence of the timer.
                string defaultTimer = null;
                if (defaultConfiguration != null &&
                    defaultConfiguration.StatsMode != null && 
                    defaultConfiguration.StatsMode.TryGetValue("timer", out string timerValue))
                {
                    defaultTimer = timerValue; 
                }

                string timer = Prompt.Input<string>(message: "Enter the period magnitude as an integer and 'm' for minutes / 's' for seconds",
                                                    defaultValue: defaultTimer);
                statsHeapDict["timer"] = timer;
            }

            // Min GC Pause In Msec.
            Console.WriteLine();
            bool shouldSetupMinGCPauseInMsec = Prompt.Confirm("Would you like to set a value for the minimum GC Pause duration to filter GCs off of?");
            Dictionary<string, string> displayConditions = defaultConfiguration?.DisplayConditions ?? new();
            if (shouldSetupMinGCPauseInMsec)
            {
                string defaultGCPauseDuration = null;
                if (defaultConfiguration != null &&
                    defaultConfiguration.DisplayConditions != null &&
                    defaultConfiguration.DisplayConditions.TryGetValue("min gc duration (msec)", out string minDuration))
                {
                    defaultGCPauseDuration = minDuration;
                }

                string minGCPauseTime = Prompt.Input<string>(message: "Enter the minimum GC Pause duration value to consider in Msec",
                                                             defaultValue: defaultGCPauseDuration);
                displayConditions["min gc duration (msec)"] = minGCPauseTime;
            }

            Configuration configuration = new Configuration
            {
                Columns = nameOfColumns.ToList(),
                AvailableColumns = ColumnInfoMap.Map.Keys.ToList()
            };

            if (shouldSetupTimer)
            {
                configuration.StatsMode = statsHeapDict;
            }
            if (shouldSetupMinGCPauseInMsec)
            {
                configuration.DisplayConditions = displayConditions;
            }

            // Validate the configuration before persisting.
            ConfigurationReader.ValidateConfiguration(configuration);

            var serializer = new SerializerBuilder()
                                 .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                 .Build();
            var serializedResult = serializer.Serialize(configuration);
            await File.WriteAllTextAsync(path: path, contents: serializedResult);

            // Programmatically add the comments to all the columns.
            string[] lines = await File.ReadAllLinesAsync(path);
            for(int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (ColumnInfoMap.Map.TryGetValue(line.Replace("- ", ""), out var columnInfo))
                {
                    lines[i] = line + $" # {columnInfo.Description}";
                }
            }

            // Rewrite the file with all the lines.
            await File.WriteAllLinesAsync(path: path, contents: lines);

            return configuration;
        }
    }
}
