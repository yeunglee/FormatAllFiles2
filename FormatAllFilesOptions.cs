using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FormatAllFiles2
{
    public class FormatAllFilesOptions : UIElementDialogPage
    {
        private const string DefaultExtensions =
            ".cs; .vb; .cpp; .h; .c; .hpp; .cc; .cxx; " +
            ".xml; .xaml; .axml; .html; .htm; .css; " +
            ".js; .ts; .jsx; .tsx; .json; .sql; " +
            ".cshtml; .vbhtml; .aspx; .ascx; .master; " +
            ".config; .targets; .props; .resx; " +
            ".fs; .razor; .scss; .less; .svg; " +
            ".ps1; .psm1; .psd1; .bat; .cmd; " +
            ".ini; .toml; .yaml; .yml; .proto; .md; .py";

        private string fileExtensions = DefaultExtensions;
        private FormatCommandPreset formatCommandPreset = FormatCommandPreset.FormatDocument;
        private string customFormatCommands = "Edit.FormatDocument";
        private HashSet<string> cachedExtensions;
        private string cachedExtensionsSource;
        private List<string> cachedCommands;
        private string cachedCommandsSource;
        private OptionsControl optionsControl;

        // ──────────────────────────────────────────
        //  Public properties (persisted to SettingsStore)
        // ──────────────────────────────────────────

        /// <summary>
        /// Semicolon-separated list of file extensions to format (include the dot).
        /// </summary>
        public string FileExtensions
        {
            get => fileExtensions;
            set
            {
                fileExtensions = value;
                cachedExtensions = null;
                cachedExtensionsSource = null;
            }
        }

        /// <summary>
        /// The preset format command selected by the user.
        /// </summary>
        public FormatCommandPreset FormatCommandPreset
        {
            get => formatCommandPreset;
            set
            {
                formatCommandPreset = value;
                cachedCommands = null;
                cachedCommandsSource = null;
            }
        }

        /// <summary>
        /// Custom format commands (only used when <see cref="FormatCommandPreset"/> is <see cref="FormatCommandPreset.Custom"/>).
        /// Semicolon-separated list of Visual Studio command names.
        /// </summary>
        public string CustomFormatCommands
        {
            get => customFormatCommands;
            set
            {
                customFormatCommands = value;
                cachedCommands = null;
                cachedCommandsSource = null;
            }
        }

        // ──────────────────────────────────────────
        //  WPF Child
        // ──────────────────────────────────────────

        /// <summary>
        /// The WPF user control shown in the Options dialog.
        /// </summary>
        protected override UIElement Child
        {
            get
            {
                if (optionsControl == null)
                {
                    optionsControl = new OptionsControl();
                    optionsControl.LoadSettings(FileExtensions, FormatCommandPreset, CustomFormatCommands);
                    optionsControl.SettingsChanged += OnSettingsChanged;
                }
                return optionsControl;
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            if (optionsControl == null) return;
            FileExtensions = optionsControl.FileExtensionsValue;
            FormatCommandPreset = optionsControl.PresetValue;
            CustomFormatCommands = optionsControl.CustomFormatCommandsValue;
        }

        // ──────────────────────────────────────────
        //  Public methods used by FormatAllFilesCommand
        // ──────────────────────────────────────────

        public ISet<string> GetExtensions()
        {
            var current = FileExtensions;
            if (cachedExtensions != null && cachedExtensionsSource == current)
                return cachedExtensions;

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(current))
            {
                foreach (var part in current.Split(';'))
                {
                    var ext = part.Trim();
                    if (ext.Length > 0)
                    {
                        if (!ext.StartsWith("."))
                            ext = "." + ext;
                        set.Add(ext);
                    }
                }
            }

            cachedExtensions = set;
            cachedExtensionsSource = current;
            return set;
        }

        public IList<string> GetCommands()
        {
            var cacheKey = FormatCommandPreset.ToString() + "|" + CustomFormatCommands;
            if (cachedCommands != null && cachedCommandsSource == cacheKey)
                return cachedCommands;

            var list = new List<string>();

            switch (FormatCommandPreset)
            {
                case FormatCommandPreset.FormatDocument:
                    list.Add("Edit.FormatDocument");
                    break;

                case FormatCommandPreset.RemoveAndSortUsings:
                    list.Add("Edit.RemoveAndSort");
                    break;

                case FormatCommandPreset.FormatDocumentThenRemoveAndSortUsings:
                    list.Add("Edit.FormatDocument");
                    list.Add("Edit.RemoveAndSort");
                    break;

                case FormatCommandPreset.Custom:
                    if (!string.IsNullOrWhiteSpace(CustomFormatCommands))
                    {
                        foreach (var part in CustomFormatCommands.Split(';'))
                        {
                            var cmd = part.Trim();
                            if (cmd.Length > 0)
                                list.Add(cmd);
                        }
                    }
                    if (list.Count == 0)
                        list.Add("Edit.FormatDocument");
                    break;

                default:
                    list.Add("Edit.FormatDocument");
                    break;
            }

            cachedCommands = list;
            cachedCommandsSource = cacheKey;
            return list;
        }

        // ──────────────────────────────────────────
        //  Reset
        // ──────────────────────────────────────────

        public override void ResetSettings()
        {
            base.ResetSettings();
            FileExtensions = DefaultExtensions;
            FormatCommandPreset = FormatCommandPreset.FormatDocument;
            CustomFormatCommands = "Edit.FormatDocument";
        }
    }
}
