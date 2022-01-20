using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Configuration;
using realmon.Configuration.Theme;
using System;
using System.IO;
using System.Threading.Tasks;

namespace realmon.UnitTests
{
    [TestClass]
    public class ThemeTests
    {
        [TestMethod]
        public async Task Theme_Config_IsUsed()
        {
            string path = Path.Combine("./Theme", "Theme.yaml");
            Configuration.Configuration configuration = await ConfigurationReader.ReadConfigurationAsync(path);
            configuration.Should().NotBeNull();

            Action validate = () => ConfigurationReader.ValidateConfiguration(configuration);
            validate.Should().NotThrow();

            ThemeConfig.Initialize(configuration);

            // these two settings were defined in yaml so they should have unique values that do not match the default
            ThemeConfig.Current.GCTableHeaderColor.Should().Be("#112233");
            ThemeConfig.Current.GCTableHeaderColor.Should().NotBe(ThemeConfig.Default.GCTableHeaderColor);

            ThemeConfig.Current.Gen0HeapColor.Should().Be("#332211");
            ThemeConfig.Current.Gen0HeapColor.Should().NotBe(ThemeConfig.Default.Gen0HeapColor);

            // this setting was not defined in yaml, so it should equal the  fall back
            ThemeConfig.Current.Gen1HeapColor.Should().Be(ThemeConfig.Default.Gen1HeapColor);
        }
    }
}
