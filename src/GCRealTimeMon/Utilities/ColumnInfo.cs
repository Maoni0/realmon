﻿using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System;

namespace realmon.Utilities
{
    /// <summary>
    /// Abstraction representing the column with the presentation details. 
    /// </summary>
    internal sealed class ColumnInfo
    {
        public ColumnInfo(string name,
                          Func<TraceGC, object> getColumnValueFromEvent,
                          int? alignment = null,
                          string format = "")
        {
            Name = name;
            // If no alignment is specified, default to the length of the name.
            Alignment = alignment ?? Name.Length;
            GetColumnValueFromEvent = getColumnValueFromEvent;
            Format = format;
        }

        public string Name   { get; }
        public int Alignment { get; }
        public Func<TraceGC, object> GetColumnValueFromEvent { get; }
        public string Format { get; }
    }
}
