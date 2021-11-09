using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Configuration;
using System;
using System.Collections.Generic;

namespace realmon.UnitTests
{
    [TestClass]
    public class ValidateConfiguration
    {
        [TestMethod]
        public void ValidateConfiguration_ConfigurationIsNull_ShouldThrowArgumentNullException()
        {
            Configuration.Configuration configuration = null;
            Action validate = () => ConfigurationReader.ValidateConfiguration(configuration);
            validate.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ValidateConfiguration_ColumnsAreNull_ShouldThrowArgumentNullException()
        {
            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.Columns = null;
            Action validate = () => ConfigurationReader.ValidateConfiguration(configuration);
            validate.Should().Throw<ArgumentNullException>();
        }
        
        [TestMethod]
        public void ValidateConfiguration_AvailableColumnsAreNull_ShouldThrowArgumentNullException()
        {
            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.AvailableColumns = null;
            Action validate = () => ConfigurationReader.ValidateConfiguration(configuration);
            validate.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ValidateConfiguration_ColumnIsNotRegistered_ShouldThrowKeyNotFoundException()
        {
            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.Columns = new List<string> { "unregisted"};
            configuration.AvailableColumns = new List<string>();
            Action validate = () => ConfigurationReader.ValidateConfiguration(configuration);
            validate.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void ValidateConfiguration_AvailableColumnIsNotRegistered_ShouldThrowKeyNotFoundException()
        {
            Configuration.Configuration configuration = new Configuration.Configuration();
            configuration.Columns = new List<string> { "index", "gen" };
            configuration.AvailableColumns = new List<string> { "unregisted" };
            Action validate = () => ConfigurationReader.ValidateConfiguration(configuration);
            validate.Should().Throw<KeyNotFoundException>();
        }
    }
}
