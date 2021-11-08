using System.Collections.Generic;

namespace GCRealTimeMon.Utilities
{
    internal static class ColumnInfoMap
    {
        public static Dictionary<string, ColumnInfo> Map =
            new Dictionary<string, ColumnInfo>
            {
                // Default Columns
                { "index", new ColumnInfo(name: "index",
                                          alignment: 10,
                                          getColumnValueFromEvent: (traceEvent) => traceEvent.Number )},
                { "type",  new ColumnInfo(name: "type",
                                          alignment: 15,
                                          getColumnValueFromEvent: (traceEvent) => traceEvent.Type )},

                // Optional Columns
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
            };
    }
}
