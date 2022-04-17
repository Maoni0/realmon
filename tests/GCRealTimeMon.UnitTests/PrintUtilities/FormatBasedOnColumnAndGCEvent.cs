using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;
using FluentAssertions;
using System;

namespace realmon.UnitTests
{
    [TestClass]
    public class PrintUtilitiesTest 
    {
        [TestMethod]
        public void FormatBasedOnColumnAndGCEvent_GCIndexColumn_SuccessfullyFormatted()
        {
            TraceGC traceGC = new TraceGC(2) { Number = 1 };
            CapturedGCEvent capturedGCEvent = new CapturedGCEvent { Data = traceGC };
            string resolvedColumn = PrintUtilities.FormatBasedOnColumnAndGCEvent(capturedGCEvent: capturedGCEvent, columnName: "index");
            resolvedColumn.Should().NotBeNullOrEmpty();
            resolvedColumn.Should().BeEquivalentTo("         1");
        }

        [TestMethod]
        public void FormatBasedOnColumnAndGCEvent_TypeInputColumn_SuccessfullyFormatted()
        {
            TraceGC traceGC = new TraceGC(2) { Type = Microsoft.Diagnostics.Tracing.Parsers.Clr.GCType.NonConcurrentGC };
            CapturedGCEvent capturedGCEvent = new CapturedGCEvent { Data = traceGC };
            string resolvedColumn = PrintUtilities.FormatBasedOnColumnAndGCEvent(capturedGCEvent: capturedGCEvent, columnName: "type");
            resolvedColumn.Should().NotBeNullOrEmpty();
            resolvedColumn.Should().BeEquivalentTo("NonConcurrentGC");
        }

        [TestMethod]
        public void FormatBasedOnColumnAndGCEvent_GenerationColumn_SuccessfullyFormatted()
        {
            TraceGC traceGC = new TraceGC(2) { Generation = 1 };
            CapturedGCEvent capturedGCEvent = new CapturedGCEvent { Data = traceGC };
            string resolvedColumn = PrintUtilities.FormatBasedOnColumnAndGCEvent(capturedGCEvent: capturedGCEvent, columnName: "gen");
            resolvedColumn.Should().NotBeNullOrEmpty();
            resolvedColumn.Should().BeEquivalentTo("    1");
        }

        [TestMethod]
        public void FormatBasedOnColumnAndGCEvent_GCPauseColumn_SuccessfullyFormatted()
        {
            TraceGC traceGC = new TraceGC(2) { PauseDurationMSec = 100 };
            CapturedGCEvent capturedGCEvent = new CapturedGCEvent { Data = traceGC };
            string resolvedColumn = PrintUtilities.FormatBasedOnColumnAndGCEvent(capturedGCEvent: capturedGCEvent, columnName: "pause (ms)");
            resolvedColumn.Should().NotBeNullOrEmpty();
            resolvedColumn.Should().BeEquivalentTo("    100.00");
        }

        [TestMethod]
        public void FormatBasedOnColumnAndGCEvent_UnidentifiedColumn_UnsuccessfullyParsedWithException()
        {
            TraceGC traceGC = new TraceGC(2) { PauseDurationMSec = 100 };
            CapturedGCEvent capturedGCEvent = new CapturedGCEvent { Data = traceGC };
            Action resolveColumn = () => PrintUtilities.FormatBasedOnColumnAndGCEvent(capturedGCEvent: capturedGCEvent, columnName: "unregisted");
            resolveColumn.Should().Throw<ArgumentException>();
        }
    }
}
