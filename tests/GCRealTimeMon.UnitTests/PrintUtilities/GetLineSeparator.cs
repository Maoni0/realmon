using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;
using System.Collections.Generic;

namespace realmon.UnitTests
{
    [TestClass]
    public class GetLineSeparator
    {
        [TestMethod]
        public void GetLineSeparator_DefaultConfig_SuccessfullyParsed()
        {
            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.Columns = new List<string> { "type", "gen", "pause (ms)", "reason" };
            string lineSeparator = PrintUtilities.GetLineSeparator(configuration);
            lineSeparator.Should().NotBeNull();
            lineSeparator.Should().BeEquivalentTo("------------------------------------------------------------------------------");
        }
    }
}
