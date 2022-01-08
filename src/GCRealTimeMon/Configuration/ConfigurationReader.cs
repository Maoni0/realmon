using realmon.Configuration.Theme;
using realmon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Spectre.Console;

namespace realmon.Configuration
{
    internal static class ConfigurationReader
    {
        public static readonly string DefaultPath;

        static ConfigurationReader()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // single file mode doesn't support getting the location of an assembly
            if (string.IsNullOrEmpty(location))
                DefaultPath = "DefaultConfig.yaml";
            else
                DefaultPath = Path.Combine(location, "DefaultConfig.yaml");
        }

        /// <summary>
        /// This method parses the configuration based on the given config path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<Configuration> ReadConfigurationAsync(string path)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                            .Build();

            string configContents = await File.ReadAllTextAsync(path);
            Configuration configuration = deserializer.Deserialize<Configuration>(configContents);
            ValidateConfiguration(configuration);
            return configuration;
        }

        /// <summary>
        /// Validates the configuration by checking for null values for the properties and unregistered columns.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ArgumentException"></exception>
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
            foreach (var column in configuration.Columns)
            {
                if (!ColumnInfoMap.Map.ContainsKey(column))
                {
                    throw new KeyNotFoundException($"Column: {column} in the `column` list isn't a registered column in the ColumnInfoMap.");
                }
            }

            // All Available Columns.
            foreach (var column in configuration.AvailableColumns)
            {
                if (!ColumnInfoMap.Map.ContainsKey(column))
                {
                    throw new KeyNotFoundException($"Column: {column} in the `available_column` list isn't a registered column in the ColumnInfoMap.");
                }
            }

            // Checks for the optional stats mode.
            if (configuration.StatsMode != null)
            {
                // timer is a mandatory property if `stats_mode` is opted in.
                if (!configuration.StatsMode.TryGetValue("timer", out string timer))
                {
                    throw new ArgumentException("`timer` is null.");
                }

                // timer format is #m or #s where # is the magnitude of the period and m is for minutes / s for seconds.
                if (timer.Length <= 1)
                {
                    throw new ArgumentException("The `timer` value should be specified as a period as an int and a char representing the magnitude and the period type with minutes 'm' or seconds 's', respectively.");
                }

                if (timer[timer.Length - 1] != 's' && timer[timer.Length - 1] != 'm')
                {
                    throw new ArgumentException("The `timer` value should either end with a period type representing character - minutes 'm' or seconds 's'");
                }

                if (!int.TryParse(timer[0..^1], out int _))
                {
                    throw new ArgumentException("The `timer` value should be a number as the period magnitude followed by a period type representing character");
                }
            }

            // Check for optional display conditions mode.
            if (configuration.DisplayConditions != null)
            {
                if (configuration.DisplayConditions.TryGetValue("min gc duration (msec)", out var minDuration))
                {
                    if (!double.TryParse(minDuration, out _))
                    {
                        throw new ArgumentException("The `min gc duration (msec)` value of the display_conditions has to be a double.");
                    }
                }
            }

            // Check for custom theme
            if (configuration.Theme != null)
            {
                foreach (PropertyInfo prop in typeof(ITheme).GetProperties())
                {
                    string value = (string)prop.GetValue(configuration.Theme);

                    // null is allowed; we'll fall back to default theme for those fields
                    // if there is a non-null value specified, make sure it parses as a style
                    if (value != null && !Style.TryParse(value, out Style result))
                    {
                        throw new ArgumentException($"The value '{value}' for theme field '{prop.Name}' is invalid.");
                    }
                }
            }
        }
    }
}
