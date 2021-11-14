using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace realmon.UnitTests
{
    [TestClass]
    public class ReadConfigurationAsync
    {
        private static readonly string TestConfigurationPath = Path.Combine("./ConfigurationReader", "TestConfigurations");

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

        [TestMethod]
        public async Task ReadConfigurationAsync_ReadDefaultWithStatsMode_SuccessfullyParsed()
        {
            string defaultPath = Path.Combine(TestConfigurationPath, "DefaultWithStatsMode.yaml");
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

            // Check Stats Mode.
            configuration.StatsMode.Should().NotBeNull();
            configuration.StatsMode.Should().ContainKeys("timer");
            configuration.StatsMode["timer"].Should().NotBeNull();
            int.TryParse(configuration.StatsMode["timer"][0..^1], out var _).Should().BeTrue();
            bool conditionForPeriodType = configuration.StatsMode["timer"].Last() == 'm' || configuration.StatsMode["timer"].Last() == 's';
            conditionForPeriodType.Should().BeTrue();
        }

        [TestMethod]
        public async Task ReadConfigurationAsync_NoColumns_ShouldThrowNullReferenceException()
        {
            string path = Path.Combine(TestConfigurationPath, "NoColumns.yaml");
            Func<Task> validate = async () => await ConfigurationReader.ReadConfigurationAsync(path);
            await validate.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ReadConfigurationAsync_NoAvailableColumns_ShouldThrowNullReferenceException()
        {
            string path = Path.Combine(TestConfigurationPath, "NoAvailableColumns.yaml");
            Func<Task> validate = async () => await ConfigurationReader.ReadConfigurationAsync(path);
            await validate.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ReadConfigurationAsync_UnregisteredColumns_ShouldThrowNullReferenceException()
        {
            string path = Path.Combine(TestConfigurationPath, "UnregisteredColumns.yaml");
            Func<Task> validate = async () => await ConfigurationReader.ReadConfigurationAsync(path);
            await validate.Should().ThrowAsync<KeyNotFoundException>();
        }

        [TestMethod]
        public async Task ReadConfigurationAsync_UnregisteredAvailableColumns_ShouldThrowNullReferenceException()
        {
            string path = Path.Combine(TestConfigurationPath, "UnregisteredAvailableColumns.yaml");
            Func<Task> validate = async () => await ConfigurationReader.ReadConfigurationAsync(path);
            await validate.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
