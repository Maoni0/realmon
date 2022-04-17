using System;

namespace realmon.Utilities
{
    /// <summary>
    /// Abstraction representing the column with the presentation details. 
    /// </summary>
    internal sealed class ColumnInfo
    {
        public ColumnInfo(string name,
                          Func<CapturedGCEvent, object> getColumnValueFromEvent,
                          int? alignment = null,
                          string format = "",
                          string description = "")
        {
            Name = name;
            // If no alignment is specified, default to the length of the name.
            Alignment = alignment ?? Name.Length;
            GetColumnValueFromEvent = getColumnValueFromEvent;
            Format = format;
            Description = description;
        }

        public string Name   { get; }
        public int Alignment { get; }
        public Func<CapturedGCEvent, object> GetColumnValueFromEvent { get; }
        public string Format { get; }
        public string Description { get; }
    }
}
