using System;

namespace realmon.Configuration.Theme
{
    /// <summary>
    /// Constants used for spectre console output theming.
    /// </summary>
    internal static class ThemeConfig
    {
        public static void Initialize(Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            Current = configuration.Theme ?? Default;
        }

        public static ITheme Default
        {
            get
            {
                return System.Console.BackgroundColor == System.ConsoleColor.White ? LightTheme : DarkTheme;
            }
        }

        // Set by initialize
        // Can be overwritten by a custom theme defined in yaml
        public static ITheme Current { get; set; }

        public static string ToHeader(string headerName) => $"[bold {Current.GCTableHeaderColor}]{headerName}[/]";

        public static string ToMessage(string message) => $"[{Current.MessageColor}]{message}[/]";

        public static string ToWarning(string message) => $"[{Current.WarningColor}]{message}[/]";

        public static string TotalHeap => $"[bold {Current.TotalHeapColor}]Total Heap:[/]";
        public static string Gen0Heap => $"[bold {Current.Gen0HeapColor}]Gen 0:[/]";
        public static string Gen1Heap => $"[bold {Current.Gen1HeapColor}]Gen 1:[/]";
        public static string Gen2Heap => $"[bold {Current.Gen2HeapColor}]Gen 2:[/]";
        public static string Gen3Heap => $"[bold {Current.Gen3HeapColor}]Gen 3:[/]";
        public static string Gen4Heap => $"[bold {Current.Gen4HeapColor}]Gen 4:[/]";

        private static readonly ITheme DarkTheme = new DarkTheme();
        private static readonly ITheme LightTheme = new LightTheme();
    }
}
