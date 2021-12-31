namespace GCRealTimeMon.Configuration
{
    /// <summary>
    /// Constants used for spectre console output theming.
    /// </summary>
    internal static class Theme
    {
        public static string ToHeader(string headerName) => $"[bold {Constants.GCTableHeaderColor}]{headerName}[/]";

        public static string ToMessage(string message) => $"[{Constants.MessageColor}]{message}[/]";

        public static string ToWarning(string message) => $"[{Constants.WarningColor}]{message}[/]";

        public static string TotalHeap = $"[bold {Constants.TotalHeapColor}]Total Heap:[/]";
        public static string Gen0Heap = $"[bold {Constants.Gen0HeapColor}]Gen 0:[/]";
        public static string Gen1Heap = $"[bold {Constants.Gen1HeapColor}]Gen 1:[/]";
        public static string Gen2Heap = $"[bold {Constants.Gen2HeapColor}]Gen 2:[/]";
        public static string Gen3Heap = $"[bold {Constants.Gen3HeapColor}]Gen 3:[/]";
        public static string Gen4Heap = $"[bold {Constants.Gen4HeapColor}]Gen 4:[/]";

        // Future - these could be configurable via yaml
        // Color reference: https://spectreconsole.net/appendix/colors
        internal class Constants
        {
            public const string MessageColor = "bold blue";
            public const string MessageRuleColor = "green1";

            public const string WarningColor = "i darkorange";

            public const string HighlightColor = "silver";

            public const string GCTableHeaderColor = "yellow";
            public const string Gen0RowColor = "[skyblue1]";
            public const string Gen1RowColor = "[lightskyblue1]";
            public const string Gen2RowColor = "[deepskyblue1]";

            public const string TotalHeapColor = "silver";
            public const string Gen0HeapColor = "green1";
            public const string Gen1HeapColor = "hotpink";
            public const string Gen2HeapColor = "dodgerblue1";
            public const string Gen3HeapColor = "yellow1";
            public const string Gen4HeapColor = "mediumpurple3";
        }
    }
}
