using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System;

namespace realmon.Utilities
{
    /// <summary>
    /// Abstraction representing the column with the presentation details. 
    /// </summary>
    internal sealed class ColumnInfo
    {
        public ColumnInfo(string name, int alignment, Func<TraceGC, object> getColumnValueFromEvent, string format = "")
        {
            Name = name;
            Alignment = alignment;
            GetColumnValueFromEvent = getColumnValueFromEvent;
            Format = format;
        }

        public string Name   { get; }
        public int Alignment { get; }
        public Func<TraceGC, object> GetColumnValueFromEvent { get; }
        public string Format { get; }
    }
}
