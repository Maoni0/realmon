using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;
using System;

namespace realmon.UnitTests
{
    [TestClass]
    public class FormatBasedOnColumnName
    {
        [TestMethod]
        public void FormatBasedOnColumnName_IndexColumn_SuccessfullyFormated()
        {
            string resolvedColumn = PrintUtilities.FormatBasedOnColumnName(columnName: "index");
            resolvedColumn.Should().NotBeNullOrEmpty();
            resolvedColumn.Should().BeEquivalentTo("     index");
        }

        [TestMethod]
        public void FormatBasedOnColumnName_TypeColumn_SuccessfullyFormated()
        {
            string resolvedColumn = PrintUtilities.FormatBasedOnColumnName(columnName: "type");
            resolvedColumn.Should().NotBeNullOrEmpty();
            resolvedColumn.Should().BeEquivalentTo("           type");
        }

        [TestMethod]
        public void FormatBasedOnColumnName_UnregistedColumn_Excepted()
        {
            Action resolveColumn = () => PrintUtilities.FormatBasedOnColumnName(columnName: "unregistered");
            resolveColumn.Should().Throw<ArgumentException>();
        }
    }
}
