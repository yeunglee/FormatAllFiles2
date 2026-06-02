namespace FormatAllFiles2
{
    /// <summary>
    /// Preset format commands available in the options dialog.
    /// </summary>
    public enum FormatCommandPreset
    {
        /// <summary>Edit.FormatDocument</summary>
        FormatDocument,

        /// <summary>Edit.RemoveAndSort</summary>
        RemoveAndSortUsings,

        /// <summary>Edit.FormatDocument then Edit.RemoveAndSort</summary>
        FormatDocumentThenRemoveAndSortUsings,

        /// <summary>Use custom format commands specified by the user</summary>
        Custom
    }
}
