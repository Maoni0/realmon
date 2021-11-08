using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace realmon.UnitTests
{
    [TestClass]
    public class ConfigurationReaderTests
    {
        private const string TestConfigurationPath = "./TestConfigurations";

        [TestMethod]
        public async Task ReadConfigurationAsync_ReadDefault_SuccessfullyParsed()
        {
            string defaultPath = Path.Combine(TestConfigurationPath, "Default.yaml");
            Configuration.Configuration configuration = await ConfigurationReader.ReadConfigurationAsync(defaultPath);
            configuration.Should().NotBeNull();

            // Check Columns.
            configuration.Columns.Should().NotBeNull();
            configuration.Columns.Should().Contain("type");
            configuration.Columns.Should().Contain("gen");
            configuration.Columns.Should().Contain("pause (ms)");
            configuration.Columns.Should().Contain("reason");

            // Check Available Columns.
            configuration.AvailableColumns.Should().NotBeNull();
            configuration.AvailableColumns.Should().Contain("type");
            configuration.AvailableColumns.Should().Contain("gen");
            configuration.AvailableColumns.Should().Contain("pause (ms)");
            configuration.AvailableColumns.Should().Contain("reason");
        }
    }
}
