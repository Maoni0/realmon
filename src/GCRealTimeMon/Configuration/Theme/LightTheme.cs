namespace realmon.Configuration.Theme
{
    /// <summary>
    /// A light theme for console output.
    /// </summary>
    internal class LightTheme : ITheme
    {
        public string MessageColor { get; } = "#718c00";

        public string WarningColor { get; } = "i #c82829";

        public string HighlightColor { get; } = "#8a6000";

        public string GCTableHeaderColor { get; } = "#8a6000";
        public string Gen0RowColor { get; } = "#3e999f";
        public string Gen1RowColor { get; } = "#4271ae";
        public string Gen2RowColor { get; } = "#8959a8";

        public string TotalHeapColor { get; } = "#000000";
        public string Gen0HeapColor { get; } = "#718c00";
        public string Gen1HeapColor { get; } = "#c82829";
        public string Gen2HeapColor { get; } = "#4271ae";
        public string Gen3HeapColor { get; } = "#eab700";
        public string Gen4HeapColor { get; } = "#8959a8";
    }
}
