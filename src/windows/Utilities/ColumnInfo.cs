using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System;

namespace GCRealTimeMon.Utilities
{
    /// <summary>
    /// TODO: Fill this.
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
