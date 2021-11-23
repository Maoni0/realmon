namespace GCRealTimeMon.Utilities
{
    using GCRealTimeMon.Configuration.ConsoleOutput;

    using Spectre.Console;

    internal static class ConsoleOut
    {
        public static void WriteRule(string ruleMessage)
        {
            AnsiConsole.Write(new Rule(ruleMessage).RuleStyle(Style.Parse(Theme.Constants.MessageRuleColor)));
        }

        internal static void WriteRule()
        {
            AnsiConsole.Write(new Rule().RuleStyle(Style.Parse(Theme.Constants.MessageRuleColor)));
        }

        internal static void WriteWarning(string warning)
        {
            AnsiConsole.MarkupLine(Theme.ToWarning(warning));
        }
    }
}
