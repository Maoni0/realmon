using System;
using Microsoft.Diagnostics.Tracing.Analysis.GC;

namespace realmon.Utilities
{
    // Capture both time and data in one reference to avoid tearing when passing them around
    internal class CapturedGCEvent
    {
        public DateTime Time { get; set; }
        public TraceGC Data { get; set; }
    }
}
