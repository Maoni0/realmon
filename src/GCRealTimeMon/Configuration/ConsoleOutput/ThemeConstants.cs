namespace GCRealTimeMon.Configuration.ConsoleOutput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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

        // todo - allow yaml config?
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
            public const string Gen0HeapColor = "Green1";
            public const string Gen1HeapColor = "HotPink";
            public const string Gen2HeapColor = "Dodgerblue1";
            public const string Gen3HeapColor = "Yellow1";
            public const string Gen4HeapColor = "mediumpurple3";
        }
    }
}
