using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using realmon.Utilities;

namespace GCRealTimeMon.UnitTests
{
    [TestClass]
    public class AddSentinelValueForTheConfigPathIfNotSpecified
    {
        [TestMethod]
        public void AddSentinelValueForTheConfigPathIfNotSpecified_ArgNotPassed_ArgsNotChanged()
        {
            string[] args = new string[] { "-n", "devenv" };
            string[] returnArgs = CommandLineUtilities.AddSentinelValueForTheConfigPathIfNotSpecified(args);
            args.Should().BeEquivalentTo(returnArgs);
        }

        [TestMethod]
        public void AddSentinelValueForTheConfigPathIfNotSpecified_ArgPassedWithPath_ArgsNotChanged()
        {
            string[] args = new string[] { "-n", "devenv", "-c", "SomePath" };
            string[] returnArgs = CommandLineUtilities.AddSentinelValueForTheConfigPathIfNotSpecified(args);
            args.Should().BeEquivalentTo(returnArgs);
        }
        
        [TestMethod]
        public void AddSentinelValueForTheConfigPathIfNotSpecified_ArgPassedWithoutPathAtEnd_ArgsNotChanged()
        {
            string[] args = new string[] { "-n", "devenv", "-c" };
            string[] returnArgs = CommandLineUtilities.AddSentinelValueForTheConfigPathIfNotSpecified(args);
            args.Should().NotBeEquivalentTo(returnArgs);
            returnArgs[^1].Should().BeEquivalentTo(CommandLineUtilities.SentinelPath);
        }

        [TestMethod]
        public void AddSentinelValueForTheConfigPathIfNotSpecified_ArgPassedWithoutPathInTheMiddle1_ArgsNotChanged()
        {
            string[] args = new string[] { "-c", "-n", "devenv" };
            string[] returnArgs = CommandLineUtilities.AddSentinelValueForTheConfigPathIfNotSpecified(args);
            args.Should().NotBeEquivalentTo(returnArgs);
            returnArgs[1].Should().BeEquivalentTo(CommandLineUtilities.SentinelPath);
        }

        [TestMethod]
        public void AddSentinelValueForTheConfigPathIfNotSpecified_ArgPassedWithoutPathInTheMiddle2_ArgsNotChanged()
        {
            string[] args = new string[] { "-n", "devenv", "-c", "--help"};
            string[] returnArgs = CommandLineUtilities.AddSentinelValueForTheConfigPathIfNotSpecified(args);
            args.Should().NotBeEquivalentTo(returnArgs);
            returnArgs[3].Should().BeEquivalentTo(CommandLineUtilities.SentinelPath);
        }
    }
}
