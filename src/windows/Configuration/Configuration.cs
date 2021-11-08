using System.Collections.Generic;

namespace realmon.Configuration
{
    /// <summary>
    /// TODO:
    /// </summary>
    public sealed class Configuration
    {
        /// <summary>
        /// TODO:
        /// </summary>
        public List<string> Columns { get; set; }

        /// <summary>
        /// TODO:
        /// </summary>
        public Dictionary<string, string> StatsMode { get; set; }

        /// <summary>
        /// TODO:
        /// </summary>
        public List<string> AvailableColumns { get; set; }
    }
}
