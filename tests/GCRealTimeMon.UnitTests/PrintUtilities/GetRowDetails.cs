using FluentAssertions;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;
using System.Collections.Generic;

namespace realmon.UnitTests
{
    [TestClass]
    public class GetRowDetails
    {
        [TestMethod]
        public void GetRowDetails_DefaultConfigurationsSimpleEvent_SuccessfullyFormatted()
        {
            TraceGC traceEvent = new TraceGC(2)
            {
                Number = 430,
                Type = Microsoft.Diagnostics.Tracing.Parsers.Clr.GCType.NonConcurrentGC,
                Generation = 0,
                PauseDurationMSec = 7.38,
                Reason = Microsoft.Diagnostics.Tracing.Parsers.Clr.GCReason.AllocSmall
            };

            CapturedGCEvent capturedGCEvent = new CapturedGCEvent { Data = traceEvent };

            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.Columns = new List<string> { "type", "gen", "pause (ms)", "reason" };

            string rowDetails = PrintUtilities.GetRowDetails(capturedGCEvent, configuration);
            rowDetails.Should().NotBeNullOrEmpty();
            rowDetails.Should().BeEquivalentTo("GC#       430 | NonConcurrentGC |     0 |       7.38 |            AllocSmall |");
        }
    }
}
