namespace realmon.Utilities
{
    using System;
    using realmon.Configuration;
    using realmon.Configuration.Theme;

    internal static class ConsoleOutFactory
    {
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
