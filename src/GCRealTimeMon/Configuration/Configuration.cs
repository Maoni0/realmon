using System.Collections.Generic;

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
        public List<string> Columns { get; set; }

        /// <summary>
        /// All columns available to display. 
        /// </summary>
        public List<string> AvailableColumns { get; set; }

        /// <summary>
        /// Stats mode properties. 
        /// </summary>
        public Dictionary<string, string> StatsMode { get; set; }
    }
}
