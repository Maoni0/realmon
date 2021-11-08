using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}
