namespace GCRealTimeMon.Utilities
{
    using GCRealTimeMon.Configuration;

    using Spectre.Console;

    /// <summary>
    /// Utility for writing formatted console output
    /// </summary>
    internal static class ConsoleOut
    {
        /// <summary>
        /// Writes a horizontal rule line with a message in the center to the console.
        /// </summary>
        /// <param name="ruleMessage">The message to put in the center of the rule line.</param>
        public static void WriteRule(string ruleMessage)
        {
            AnsiConsole.Write(new Rule(ruleMessage).RuleStyle(Style.Parse(Theme.Constants.MessageRuleColor)));
        }

        /// <summary>
        /// Writes a horizontal rule line to the console.
        /// </summary>
        internal static void WriteRule()
        {
            AnsiConsole.Write(new Rule().RuleStyle(Style.Parse(Theme.Constants.MessageRuleColor)));
        }

        /// <summary>
        /// Writes a warning message to the console.
        /// </summary>
        /// <param name="warningMessage">The message to write.</param>
        internal static void WriteWarning(string warningMessage)
        {
            AnsiConsole.MarkupLine(Theme.ToWarning(warningMessage));
        }
    }
}
