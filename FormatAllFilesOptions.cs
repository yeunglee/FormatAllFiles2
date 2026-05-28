using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FormatAllFiles2
{
    public class FormatAllFilesOptions : DialogPage
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

        private const string DefaultFormatCommands = "Edit.FormatDocument";

        private string fileExtensions = DefaultExtensions;
        private string formatCommands = DefaultFormatCommands;
        private HashSet<string> cachedExtensions;
        private string cachedExtensionsSource;
        private List<string> cachedCommands;
        private string cachedCommandsSource;

        [Category("Format All Files")]
        [DisplayName("File Extensions")]
        [Description("Semicolon-separated list of file extensions to format (include the dot). "
                   + "Example: .cs; .vb; .xml")]
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

        [Category("Format All Files")]
        [DisplayName("Format Commands")]
        [Description("Semicolon-separated list of Visual Studio commands to execute for formatting each file. "
                   + "Default: Edit.FormatDocument. Example: Edit.FormatDocument; Edit.RemoveAndSort")]
        public string FormatCommands
        {
            get => formatCommands;
            set
            {
                formatCommands = value;
                cachedCommands = null;
                cachedCommandsSource = null;
            }
        }

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
            var current = FormatCommands;
            if (cachedCommands != null && cachedCommandsSource == current)
                return cachedCommands;

            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(current))
            {
                foreach (var part in current.Split(';'))
                {
                    var cmd = part.Trim();
                    if (cmd.Length > 0)
                        list.Add(cmd);
                }
            }

            if (list.Count == 0)
                list.Add("Edit.FormatDocument");

            cachedCommands = list;
            cachedCommandsSource = current;
            return list;
        }

        public override void ResetSettings()
        {
            base.ResetSettings();
            FileExtensions = DefaultExtensions;
            FormatCommands = DefaultFormatCommands;
        }
    }
}
