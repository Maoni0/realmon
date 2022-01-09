using YamlDotNet.Serialization;

namespace realmon.Configuration.Theme
{
    /// <summary>
    /// A custom user-defined theme. If a particular property is not set, it falls back to the default.
    /// </summary>
    /// <remarks>Some of the members are specially annoated with <see cref="YamlMemberAttribute"/> because their default naming
    /// convention would be more onerous.</remarks>
    public class CustomTheme : ITheme
    {
        private string gCTableHeaderColor;
        private string gen0HeapColor;
        private string gen0RowColor;
        private string gen1HeapColor;
        private string gen1RowColor;
        private string gen2HeapColor;
        private string gen2RowColor;
        private string gen3HeapColor;
        private string gen4HeapColor;
        private string highlightColor;
        private string messageColor;
        private string messageRuleColor;
        private string totalHeapColor;
        private string warningColor;

        /// <summary>
        /// Set to true to disable color output
        /// </summary>
        public bool UsePlainText { get; set; } = false;

        [YamlMember(Alias = "gc_table_header_color")]
        public string GCTableHeaderColor
        {
            get => gCTableHeaderColor ?? ThemeConfig.Default.GCTableHeaderColor;
            set => gCTableHeaderColor = value;
        }

        [YamlMember(Alias = "gen0_heap_color")]
        public string Gen0HeapColor
        {
            get => gen0HeapColor ?? ThemeConfig.Default.Gen0HeapColor;
            set => gen0HeapColor = value;
        }

        [YamlMember(Alias = "gen0_row_color")]
        public string Gen0RowColor
        {
            get => gen0RowColor ?? ThemeConfig.Default.Gen0RowColor;
            set => gen0RowColor = value;
        }

        [YamlMember(Alias = "gen1_heap_color")]
        public string Gen1HeapColor
        {
            get => gen1HeapColor ?? ThemeConfig.Default.Gen1HeapColor;
            set => gen1HeapColor = value;
        }

        [YamlMember(Alias = "gen1_row_color")]
        public string Gen1RowColor
        {
            get => gen1RowColor ?? ThemeConfig.Default.Gen1RowColor;
            set => gen1RowColor = value;
        }

        [YamlMember(Alias = "gen2_heap_color")]
        public string Gen2HeapColor
        {
            get => gen2HeapColor ?? ThemeConfig.Default.Gen2HeapColor;
            set => gen2HeapColor = value;
        }

        [YamlMember(Alias = "gen2_row_color")]
        public string Gen2RowColor
        {
            get => gen2RowColor ?? ThemeConfig.Default.Gen2RowColor;
            set => gen2RowColor = value;
        }

        [YamlMember(Alias = "gen3_heap_color")]
        public string Gen3HeapColor
        {
            get => gen3HeapColor ?? ThemeConfig.Default.Gen3HeapColor;
            set => gen3HeapColor = value;
        }

        [YamlMember(Alias = "gen4_heap_color")]
        public string Gen4HeapColor
        {
            get => gen4HeapColor ?? ThemeConfig.Default.Gen4HeapColor;
            set => gen4HeapColor = value;
        }

        public string HighlightColor
        {
            get => highlightColor ?? ThemeConfig.Default.HighlightColor;
            set => highlightColor = value;
        }

        public string MessageColor
        {
            get => messageColor ?? ThemeConfig.Default.MessageColor;
            set => messageColor = value;
        }

        public string MessageRuleColor
        {
            get => messageRuleColor ?? ThemeConfig.Default.MessageRuleColor;
            set => messageRuleColor = value;
        }

        public string TotalHeapColor
        {
            get => totalHeapColor ?? ThemeConfig.Default.TotalHeapColor;
            set => totalHeapColor = value;
        }

        public string WarningColor
        {
            get => warningColor ?? ThemeConfig.Default.WarningColor;
            set => warningColor = value;
        }
    }
}
