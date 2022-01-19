namespace realmon.Configuration.Theme
{
    /// <summary>
    /// A dark theme for console output.
    /// </summary>
    internal class DarkTheme : ITheme
    {
        public string MessageColor { get; } = "bold blue";

        public string WarningColor { get; } = "i darkorange";

        public string HighlightColor { get; } = "silver";

        public string GCTableHeaderColor { get; } = "yellow";
        public string Gen0RowColor { get; } = "skyblue1";
        public string Gen1RowColor { get; } = "lightskyblue1";
        public string Gen2RowColor { get; } = "deepskyblue1";

        public string TotalHeapColor { get; } = "silver";
        public string Gen0HeapColor { get; } = "green1";
        public string Gen1HeapColor { get; } = "hotpink";
        public string Gen2HeapColor { get; } = "dodgerblue1";
        public string Gen3HeapColor { get; } = "yellow1";
        public string Gen4HeapColor { get; } = "mediumpurple3";
    }
}
