using realmon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace realmon.Configuration 
{
    internal static class ConfigurationReader
    {
        /// <summary>
        /// This method parses the configuration based on the given config path. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<Configuration> ReadConfigurationAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("Path to the Configuration file is null.");
            }

            IDeserializer deserialier = new DeserializerBuilder()
                                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                            .Build();

            string configContents = await File.ReadAllTextAsync(path);
            Configuration configuration = deserialier.Deserialize<Configuration>(configContents);
            ValidateConfiguration(configuration);
            return await Task.FromResult(configuration);
        }

        /// <summary>
        /// Validates the configuration by checking for null values for the properties and unregistered columns.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public static void ValidateConfiguration(Configuration configuration)
        {
            // Null Checks.
            if (configuration == null)
            {
                throw new ArgumentNullException("The configuration is null. Ensure that the YAML syntax is correct.");
            }    

            if (configuration.Columns == null)
            {
                throw new ArgumentNullException("The `columns` are null. Please ensure there are columns under the `columns` list");
            }

            if (configuration.AvailableColumns == null)
            {
                throw new ArgumentNullException("The `available_columns` are null. Please ensure there are columns under the `available_columns` list");
            }

            // Check for valid column names for both available and columns to display.
            // Columns to Display.
            foreach(var column in configuration.Columns)
            {
                if (!ColumnInfoMap.Map.ContainsKey(column))
                {
                    throw new KeyNotFoundException($"Column: {column} in the `column` list isn't a registered column in the ColumnInfoMap.");
                }
            }

            // All Available Columns.
            foreach(var column in configuration.AvailableColumns)
            {
                if (!ColumnInfoMap.Map.ContainsKey(column))
                {
                    throw new KeyNotFoundException($"Column: {column} in the `available_column` list isn't a registered column in the ColumnInfoMap.");
                }
            }
        }
    }
}
