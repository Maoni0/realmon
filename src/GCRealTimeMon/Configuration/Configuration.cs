using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace realmon.Configuration
{
    /// <summary>
    /// Class contains the parsed configuration from the YAML config. 
    /// </summary>
    public sealed class Configuration
    {
        /// <summary>
        /// Columns to display. 
        /// </summary>
        [YamlMember(Description = "Selected Columns to Display")]
        public List<string> Columns { get; set; }

        /// <summary>
        /// All columns available to display. 
        /// </summary>
        [YamlMember(Description = "All the Columns Available to Choose From. Please refer to the table via the following link for details about the columns: https://github.com/Maoni0/realmon#configuration")] 
        public List<string> AvailableColumns { get; set; }

        /// <summary>
        /// Stats mode properties. 
        /// </summary>
        [YamlMember(Description = "Statistic Mode including timer period for printing Heap Stats.")] 
        public Dictionary<string, string> StatsMode { get; set; }

        /// <summary>
        /// Display conditions for cases such as min gc pause duration.
        /// </summary>
        [YamlMember(Description = "Display conditions such as min gc pause time to display.")] 
        public Dictionary<string, string> DisplayConditions { get; set; }
    }
}
