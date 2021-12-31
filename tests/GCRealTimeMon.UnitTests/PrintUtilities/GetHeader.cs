using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;

namespace realmon.UnitTests
{
    [TestClass]
    public class GetHeader
    {
        [TestMethod]
        public void GetHeader_DefaultConfiguration_SuccessfulMatch()
        {
            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.Columns = new List<string> { "type", "gen", "pause (ms)", "reason" };
            string header = PrintUtilities.GetHeader(configuration);
            header.Should().NotBeNullOrEmpty();
            header.Should().BeEquivalentTo("GC#     index |            type |   gen | pause (ms) |                reason |");
        }
    }
}
