using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<Configuration> CreateAndReturnNewConfiguration(string path)
        {
            var allColumns = Utilities.ColumnInfoMap.Map.Values.Select(s => $"{s.Name} : {s.Description}");

            // Column selection.
            var chosenColumns = Prompt.MultiSelect("Which columns would you like to select?", allColumns);
            var nameOfColumns = chosenColumns.Select(s => s.Split(":").First().Trim());

            // Heap stats timer.
            bool shouldSetupTimer = Prompt.Confirm("Would you like to setup the heap stats timer?");
            Dictionary<string, string> statsHeapDict = new();
            if (shouldSetupTimer)
            {
                string timer = Prompt.Input<string>("Enter the period magnitude as an integer and 'm' for minutes / 's' for seconds");
                statsHeapDict["timer"] = timer; 
            }

            Configuration configuration = new Configuration
            {
                Columns = nameOfColumns.ToList(),
                AvailableColumns = Utilities.ColumnInfoMap.Map.Keys.ToList() 
            };

            if (shouldSetupTimer)
            {
                configuration.StatsMode = statsHeapDict;
            }

            // Validate the configuration before persisting.
            ConfigurationReader.ValidateConfiguration(configuration);

            var serializer = new SerializerBuilder()
                                 .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                 .Build();
            var serializedResult = serializer.Serialize(configuration);
            await File.WriteAllTextAsync(path: path, contents: serializedResult);
            return configuration;
        }
    }
}
