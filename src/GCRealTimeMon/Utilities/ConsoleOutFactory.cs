using System;
using realmon.Configuration.Theme;

namespace realmon.Utilities
{
    using Configuration = realmon.Configuration.Configuration;

    internal static class ConsoleOutFactory
    {
        internal static bool IsPlainTextUsed(Configuration configuration) 
            => Console.IsOutputRedirected || configuration.Theme?.UsePlainText == true;

        internal static IConsoleOut Create(Configuration configuration)
        {
            if (Console.IsOutputRedirected || configuration.Theme?.UsePlainText == true)
            {
                return new PlainTextConsoleOut(configuration);
            }

            ThemeConfig.Initialize(configuration);
            return new SpectreConsoleOut(configuration);
        }
    }
}
