using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Collections.Generic;

namespace realmon.Utilities
{
    internal static class ColumnInfoMap
    {
        static ColumnInfoMap()
        {
            AddGenerateBasedColumns(Gens.Gen0);
            AddGenerateBasedColumns(Gens.Gen1);
            AddGenerateBasedColumns(Gens.Gen2);
            AddGenerateBasedColumns(Gens.GenLargeObj);
        }

        /// <summary>
        /// Generates the generation size at the end of the GC, this generation's survival rate and this generation's frag ratio.
        /// </summary>
        /// <param name="generation"></param>
        internal static void AddGenerateBasedColumns(Gens generation)
        {
            string gen = generation != Gens.GenLargeObj ? generation.ToString().ToLower() : "LOH";
            Map[$"{gen} size (mb)"] =
                new ColumnInfo(name: $"{gen.ToString().ToLower()} size (mb)",
                               format: "N3",
                               getColumnValueFromEvent: (traceEvent) => traceEvent.GenSizeAfterMB(generation));
            Map[$"{gen} survival rate"] =
                new ColumnInfo(name: $"{gen.ToString().ToLower()} survival rate",
                               format: "N0",
                               getColumnValueFromEvent: (traceEvent) => traceEvent.SurvivalPercent(generation));
            Map[$"{gen} frag ratio"] =
                new ColumnInfo(name: $"{gen.ToString().ToLower()} frag ratio",
                               format: "N0",
                               getColumnValueFromEvent: (traceEvent) => traceEvent.GenFragmentationPercent(generation));
        }

        /// <summary>
        /// Map that is a registry of the available columns.
        /// </summary>
        public static Dictionary<string, ColumnInfo> Map =
            new Dictionary<string, ColumnInfo>
            {
                // Default Columns
                { "index", new ColumnInfo(name: "index",
                                          alignment: 10,
                                          getColumnValueFromEvent: (traceEvent) => traceEvent.Number )},

                // Additional Columns
                { "type",  new ColumnInfo(name: "type",
                                          alignment: 15,
                                          getColumnValueFromEvent: (traceEvent) => traceEvent.Type )},
                { "gen", new ColumnInfo(name: "gen",
                                        alignment: 5,
                                        getColumnValueFromEvent: (traceEvent) => traceEvent.Generation )},
                { "pause (ms)", new ColumnInfo(name: "pause (ms)",
                                               format: "N2",
                                               alignment: 10,
                                               getColumnValueFromEvent: (traceEvent) => traceEvent.PauseDurationMSec )},
                { "reason", new ColumnInfo(name: "reason",
                                           alignment: 21,
                                           getColumnValueFromEvent: (traceEvent) => traceEvent.Reason )},

                { "suspension time (ms)", new ColumnInfo(name: "suspension time (ms)",
                                                         format: "N3",
                                                         getColumnValueFromEvent: (traceEvent) => traceEvent.SuspendDurationMSec )},

                { "pause time (%)", new ColumnInfo(name: "pause time (%)", 
                                                   format: "N1",
                                                   getColumnValueFromEvent: (traceEvent) => traceEvent.PauseTimePercentageSinceLastGC )},

                { "gen0 alloc (mb)", new ColumnInfo(name: "gen0 alloc (mb)",
                                                    format: "N3",
                                                    getColumnValueFromEvent: (traceEvent) => traceEvent.UserAllocated[(int)Gens.Gen0] )},
                { "gen0 alloc rate", new ColumnInfo(name: "gen0 alloc rate",
                                                    format: "N2",
                                                    getColumnValueFromEvent: (traceEvent) =>
                                                    {
                                                        return (traceEvent.UserAllocated[(int)Gens.Gen0] * 1000.0) / traceEvent.DurationSinceLastRestartMSec;
                                                    })},
                { "peak size (mb)", new ColumnInfo(name: "peak size (mb)",
                                                   format: "N3",
                                                   getColumnValueFromEvent: (traceEvent) => traceEvent.HeapSizePeakMB)},
                { "after size (mb)", new ColumnInfo(name: "after size (mb)",
                                                    format: "N3",
                                                    getColumnValueFromEvent: (traceEvent) => traceEvent.HeapSizeAfterMB )},
                { "peak/after", new ColumnInfo(name: "peak/after",
                                               format: "N2",
                                               getColumnValueFromEvent: (traceEvent) => traceEvent.HeapSizePeakMB / traceEvent.HeapSizeAfterMB )},
                { "promoted (mb)", new ColumnInfo(name: "promoted (mb)",
                                                  format: "N3",
                                                  getColumnValueFromEvent: (traceEvent) => traceEvent.PromotedMB )},
                { "finalize promoted (mb)", new ColumnInfo(name: "finalize promoted (mb)",
                                                           format: "N2",
                                                           getColumnValueFromEvent: (traceEvent) => traceEvent.HeapStats.FinalizationPromotedSize / 1000000.0 )},
                { "pinned objects", new ColumnInfo(name: "pinned objects",
                                                   format: "N0",
                                                   getColumnValueFromEvent: (traceEvent) => traceEvent.HeapStats.PinnedObjectCount )},
            };
    }
}
