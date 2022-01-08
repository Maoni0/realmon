namespace realmon.Configuration.Theme
{
    /// <summary>
    /// An interface to define which Spectre console colors are being used by the current theme.
    /// </summary>
    /// <remarks>Spectre Color reference: https://spectreconsole.net/appendix/colors </remarks>
    internal interface ITheme
    {
        string GCTableHeaderColor { get; }
        string Gen0HeapColor { get; }
        string Gen0RowColor { get; }
        string Gen1HeapColor { get; }
        string Gen1RowColor { get; }
        string Gen2HeapColor { get; }
        string Gen2RowColor { get; }
        string Gen3HeapColor { get; }
        string Gen4HeapColor { get; }
        string HighlightColor { get; }
        string MessageColor { get; }
        string MessageRuleColor { get; }
        string TotalHeapColor { get; }
        string WarningColor { get; }
    }
}